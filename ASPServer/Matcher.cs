using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ASPServer {
    // dotnet ef migrations add FirstVersion
    // dotnet ef database update

    public class StorageContext : DbContext {
        public DbSet<Result> Results { get; set; }
        public DbSet<ResultData> ResultsData { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder o) => o.UseSqlite("DataSource=storage.db");
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

    public class Matcher {

        public static Matcher Instance = null;

        public static void Init() {
            Instance = new Matcher();
        }

        public void Clear() {
            lock (db) {
                db.Results.RemoveRange(db.Results);
                db.ResultsData.RemoveRange(db.ResultsData);
                db.SaveChanges();
            }
        }

        public const string MODEL_PATH = "model.onnx";

        private InferenceSession session;
        public StorageContext db;

        public Matcher() {
            Console.WriteLine("Loading database");
            db = new StorageContext();
            Console.WriteLine("Loaded database");
            Console.WriteLine("Loading model");
            session = new InferenceSession(MODEL_PATH);
            Console.WriteLine("Loaded model");

            db.Results.RemoveRange(db.Results);
            db.ResultsData.RemoveRange(db.ResultsData);
            db.SaveChanges();
        }

        // Write file to temp location & check the db, read file as image & process
        public Tuple<int, int> Match(byte[] image) {

            // Check in persistent storage
            Tuple<int, int> persistent = PersistentPredict(image);

            if (persistent != null)
                return new Tuple<int, int>(persistent.Item1, persistent.Item2);
            else {
                var tensor = ToTensor(image);
                int predict = PredictImage(tensor);

                // Insert new record into persistent storage
                PersistentAdd(image, predict);

                return new Tuple<int, int>(predict, 0);
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
                    foreach (var p in db.Results.Include(p => p.resultData)) {
                        if (ComputeHash(p.resultData.file) == ComputeHash(image) && p.resultData.file.SequenceEqual(image)) {
                            val = p;
                            break;
                        }
                    }

                    if (val == null)
                        return null;

                    val.CallCount++;

                    db.Update(val);
                    db.SaveChanges();

                    return new Tuple<int, int>(val.ClassId, val.CallCount);
                }
            } catch (Exception e) {
                Trace.WriteLine(e);
                return null;
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void PersistentAdd(byte[] image, int id) {
            try {
                lock (db) {
                    ResultData rd = new ResultData { file = image };
                    Result r = new Result { CallCount = 0, ClassId = id };
                    r.resultData = rd;

                    db.Results.Add(r);
                    db.ResultsData.Add(rd);
                    db.SaveChanges();
                }
            } catch (Exception e) {
                Trace.WriteLine(e);
            }
        }

        private Tensor<float> ToTensor(byte[] file) {
            var image = Image.Load<Rgb24>(file);

            const int TargetWidth = 224;
            const int TargetHeight = 224;

            image.Mutate(x => {
                x.Resize(new ResizeOptions {
                    Size = new Size(TargetWidth, TargetHeight),
                    Mode = ResizeMode.Crop
                });
            });

            var input = new DenseTensor<float>(new[] { 1, 3, TargetHeight, TargetWidth });
            var mean = new[] { 0.485f, 0.456f, 0.406f };
            var stddev = new[] { 0.229f, 0.224f, 0.225f };
            for (int y = 0; y < TargetHeight; y++) {
                Span<Rgb24> pixelSpan = image.GetPixelRowSpan(y);
                for (int x = 0; x < TargetWidth; x++) {
                    input[0, 0, y, x] = ((pixelSpan[x].R / 255f) - mean[0]) / stddev[0];
                    input[0, 1, y, x] = ((pixelSpan[x].G / 255f) - mean[1]) / stddev[1];
                    input[0, 2, y, x] = ((pixelSpan[x].B / 255f) - mean[2]) / stddev[2];
                }
            }

            return input;
        }

        private int PredictImage(Tensor<float> tensor) {
            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("data", tensor) };
            IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = session.Run(inputs);

            var output = results.First().AsEnumerable<float>().ToArray();
            var sum = output.Sum(x => (float)Math.Exp(x));
            var softmax = output.Select(x => (float)Math.Exp(x) / sum);

            return softmax
                .Select((x, i) => new Tuple<int, float>(i, x))
                .OrderByDescending(x => x.Item2)
                .Take(1).First().Item1;
        }
    }
}
