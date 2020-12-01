using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;

// DataView + Data
namespace WPFClient {

    // dotnet ef migrations add FirstVersion
    // dotnet ef database update

    public class StorageContext : DbContext {
        public DbSet<Result> Results { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder o) => o.UseSqlite("DataSource=../../../storage.db");
    }

    public class ResultData {
        public int ResultDataId { get; set; }
        public byte[] file { get; set; }
    }

    public class Result {
        public int ResultId { get; set; }
        public int CallCount { get; set; }
        public int ClassId { get; set; }
        public ResultData resultData { get; set; }
    }

    public class DataView : INotifyPropertyChanged {

        private StorageContext db;
        private Task matcher = null;
        private CancellationTokenSource tokenSource = null;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool isMatching;
        public bool IsMatching {
            get {
                return isMatching;
            }
            set {
                isMatching = value;
                IsChooseEnabled = !isMatching;
                IsMatchEnabled = !isMatching;
                OnPropertyChanged("IsMatching");
            }
        }

        private bool isMatchEnabled;
        public bool IsMatchEnabled {
            get {
                return isMatchEnabled;
            }
            set {
                isMatchEnabled = value;
                OnPropertyChanged("IsMatchEnabled");
            }
        }

        public bool IsDirSelected {
            get {
                return dirName != null;
            }
            set { }
        }

        private string dirName = null;
        public string DirName {
            get {
                return dirName;
            }
            set {
                dirName = value;
                IsMatchEnabled = true;
                OnPropertyChanged("DirName");
                OnPropertyChanged("IsDirSelected");
                for (int i = 0; i < Labels.classLabels.Length; ++i) {
                    matchedCount[i].Count = 0;
                    matchedResult[i].Clear();
                }
                OnPropertyChanged("ClassList");
                OnPropertyChanged("ClassMatchResult");
            }
        }

        private int selectedClassId;
        public int SelectedClassId {
            get {
                return selectedClassId;
            }
            set {
                selectedClassId = value;
                OnPropertyChanged("SelectedClassId");
                OnPropertyChanged("ClassMatchResult");
            }
        }

        private bool isChooseEnabled = true;
        public bool IsChooseEnabled {
            get {
                return isChooseEnabled;
            }
            set {
                isChooseEnabled = value;
                OnPropertyChanged("IsChooseEnabled");
            }
        }

        public class MatchResult {
            public Image Image { get; set; }
            public string ClassName { get; set; }
            public int StoreCount { get; set; }
        }

        public class CountResult {
            public string ClassName { get; set; }
            public int Count { get; set; }
        }

        private CountResult[] matchedCount = new CountResult[Labels.classLabels.Length];
        private List<MatchResult>[] matchedResult = new List<MatchResult>[Labels.classLabels.Length];

        public IEnumerable ClassList {
            // get => from match in matched group match by match.classId into g select new { Count = g.Count(), ClassName = g.First().className };
            get {
                return matchedCount;
                // var count = from match in matched group match by match.classId into g select new { Count = g.Count(), ClassName = g.First().className };
                // return from label in Labels.classLabels select new { ClassName = label, Count = (from c in count where c.ClassName == label select c.Count).FirstOrDefault() };
            }
            // get => from match, label in matched, Labels.classLabels group match by match.classId into g select new { Count = g.Count(), ClassName = g.First().className };
        }

        public IEnumerable ClassMatchResult {
            get {
                return matchedResult[SelectedClassId];
                    // from match in matched where match.classId == selectedClassId select new { ClassName = match.className, Image = match.imagePath };
            }
            set { }
        }
        
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void matchHandler(string fileName, int classId, int storeCount) {
            matchedCount[classId].Count++;
            matchedResult[classId].Add(new MatchResult { ClassName = Labels.classLabels[classId], Image = Image.FromFile(fileName), StoreCount = storeCount });

            Application.Current.Dispatcher.Invoke(new Action(() => {
                OnPropertyChanged("ClassList");
                OnPropertyChanged("ClassMatchResult");
            }));
        }

        public void StartMatching() {
            for (int i = 0; i < Labels.classLabels.Length; ++i) {
                matchedCount[i].Count = 0;
                matchedResult[i].Clear();
            }

            // Start procedure of matching with connect to the server
            // Back each value to the localdb

            tokenSource = new CancellationTokenSource();
            ConcurrentQueue<string> images = new ConcurrentQueue<string>();
            Array.ForEach(Directory.GetFiles(DirName), p => images.Enqueue(p));
            matcher = Task.Run(() => {
                if (tokenSource.Token.IsCancellationRequested)
                    return;

                string image;

                while (images.TryDequeue(out image)) {
                    if (tokenSource.Token.IsCancellationRequested)
                        return;

                    try {
                        // Can throw error on not 200 status code
                        Tuple<int, int> res = MatchAsync(File.ReadAllBytes(image));

                        matchHandler(image, res.Item1, res.Item2);
                    } catch (Exception e) {
                        Trace.WriteLine("Exc: " + e);
                        // Nothing else, ignore
                    }
                }
            }, tokenSource.Token);

            IsMatching = true;
        }

        public void StopMatching() {

            // Stop matching
            tokenSource.Cancel();

            IsMatching = false;
        }


        // Write file to temp location & check the db, read file as image & process
        public Tuple<int, int> MatchAsync(byte[] image) {

            // Check in persistent storage
            Tuple<int, int> persistent = PersistentPredict(image);

            if (persistent != null)
                return new Tuple<int, int>(persistent.Item1, persistent.Item2);
            else {
                // Make request to the server & fetch data
                HttpClient client = new HttpClient();

                string path = "http://localhost:5000/api/matchresult";
                Trace.WriteLine(path);
                HttpResponseMessage response = null;

                StringContent sc = new StringContent("\"" + Convert.ToBase64String(image) + "\"");
                sc.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                response = client.PutAsync(path, sc).GetAwaiter().GetResult();

                if (response.IsSuccessStatusCode) {
                    string resp = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    dynamic obj = JsonConvert.DeserializeObject(resp);

                    PersistentAdd(image, (int)obj.classId, (int)obj.statistics);

                    return new Tuple<int, int>((int)obj.classId, (int)obj.statistics);
                }

                throw new Exception("Error 500 or something else");
            }
        }

        public static int ComputeHash(params byte[] data) {
            unchecked {
                const int p = 16777619;
                int hash = (int)2166136261;

                for (int i = 0; i < data.Length; i++)
                    hash = (hash ^ data[i]) * p;

                hash += hash << 13;
                hash ^= hash >> 7;
                hash += hash << 3;
                hash ^= hash >> 17;
                hash += hash << 5;
                return hash;
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private Tuple<int, int> PersistentPredict(byte[] image) {
            try {
                lock (db) {
                    Result val = null;
                    var pat = System.IO.Directory.GetCurrentDirectory();
                    foreach (var p in db.Results) {
                        if (ComputeHash(p.resultData.file) == ComputeHash(image) && p.resultData.file.SequenceEqual(image)) {
                            val = p;
                            break;
                        }
                    }

                    if (val == null)
                        return null;

                    //val.CallCount++;

                    //db.Update(val);
                    //db.SaveChanges();

                    return new Tuple<int, int>(val.ClassId, val.CallCount);
                }
            } catch {
                return null;
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void PersistentAdd(byte[] image, int id, int callCount) {
            try {
                lock (db) {
                    ResultData rd = new ResultData { file = image };
                    Result r = new Result { CallCount = callCount, ClassId = id };
                    r.resultData = rd;

                    db.Results.Add(r);
                    db.SaveChanges();
                }
            } catch { }
        }

        public DataView() {
            for (int i = 0; i < Labels.classLabels.Length; ++i) {
                matchedCount[i] = new CountResult { Count = 0, ClassName = Labels.classLabels[i] };
                matchedResult[i] = new List<MatchResult>();
            }

            Trace.WriteLine("Loading DB");
            // Load local database
            db = new StorageContext();
            //db.Results.RemoveRange(db.Results);
            //db.SaveChanges();
            Trace.WriteLine("Loaded DB");

            Trace.WriteLine("Adding DB");
            // Disaply images from DB



            // XXX: Crashing because no resultData is being saved
            
            
            
            lock (db) {
                foreach (var p in db.Results) {
                    matchedCount[p.ClassId].Count++;
                    matchedResult[p.ClassId].Add(new MatchResult { 
                        ClassName = Labels.classLabels[p.ClassId], 
                        Image = System.Drawing.Image.FromStream(new MemoryStream(p.resultData.file)), 
                        StoreCount = p.CallCount 
                    });
                }
            }
        }
    }
}
