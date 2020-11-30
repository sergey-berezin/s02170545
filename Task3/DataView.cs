using ResNetMatcher;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

// DataView + Data
namespace WPFClient {
    public class DataView : INotifyPropertyChanged {

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
            public string Image { get; set; }
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
            matchedResult[classId].Add(new MatchResult { ClassName = Labels.classLabels[classId], Image = fileName, StoreCount = storeCount });

            Application.Current.Dispatcher.Invoke(new Action(() => {
                OnPropertyChanged("ClassList");
                OnPropertyChanged("ClassMatchResult");
            }));
        }

        internal void StartMatching() {
            for (int i = 0; i < Labels.classLabels.Length; ++i) {
                matchedCount[i].Count = 0;
                matchedResult[i].Clear();
            }

            
            // Start procedure of matching with connect to the server
            // Back each value to the localdb

            IsMatching = true;
        }

        internal void StopMatching() {

            // Stop matching

            IsMatching = false;
        }

        public DataView() {
            for (int i = 0; i < Labels.classLabels.Length; ++i) {
                matchedCount[i] = new CountResult { Count = 0, ClassName = Labels.classLabels[i] };
                matchedResult[i] = new List<MatchResult>();
            }

            // Load local database

        }
    }
}
