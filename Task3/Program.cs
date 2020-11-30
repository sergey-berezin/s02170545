using Contracts;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server {

    public class StorageContext : DbContext {
        public DbSet<Result> Results { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder o) => o.UseSqlite("../../../../../ResNetMatcher/DataSource=storage.db");
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
        private InferenceSession session;
        private ConcurrentQueue<string> images = new ConcurrentQueue<string>();
        private Action<string, int, int> async_handler;
        private CancellationTokenSource tokenSource;
        private Task[] tasks;
        private StorageContext db;

        public Matcher() {
            db = new StorageContext();
        }

        // Action<string, int, float, int>
        // string - path
        // int - class id
        // float - probability
        // int - persistent storage call count for the image
        public void Match(Action<string, int, int> async_handler, string model_data_path) {
            session = new InferenceSession(model_data_path + Path.DirectorySeparatorChar + "model.onnx");
            Array.ForEach(Directory.GetFiles(model_data_path + Path.DirectorySeparatorChar + "images"), p => images.Enqueue(p));
            this.async_handler = async_handler;

            tokenSource = new CancellationTokenSource();

            tasks = new Task[Environment.ProcessorCount];
            for (int i = 0; i < Environment.ProcessorCount; ++i)
                tasks[i] = Task.Run(() => {
                    if (tokenSource.Token.IsCancellationRequested)
                        return;

                    string image;

                    while (images.TryDequeue(out image)) {
                        if (tokenSource.Token.IsCancellationRequested)
                            return;

                        // Check in persistent storage
                        Tuple<int, int> persistent = PersistentPredict(image);

                        if (persistent != null)
                            async_handler(image, persistent.Item1, persistent.Item2);
                        else {
                            var tensor = ReadImage(image);
                            int predict = PredictImage(tensor);

                            // Insert new record into persistent storage
                            PersistentAdd(image, predict);

                            async_handler(image, predict, 0);
                        }
                    }
                }, tokenSource.Token);
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

        private Tuple<int, int> PersistentPredict(string path) {
            try {
                byte[] image = File.ReadAllBytes(path);
                lock (db) {
                    Result val = null;
                    var pat = System.IO.Directory.GetCurrentDirectory();
                    foreach (var p in db.Results) {
                        if (ComputeHash(p.resultData.file) == ComputeHash(image) && p.resultData.file.SequenceEqual(image)) {
                            val = p;
                            break;
                        }
                    }
                    //var pres = db.Results.AsEnumerable().Where(p => p.file.GetHashCode() == image.GetHashCode()).Where(p => p.file.SequenceEqual(image));
                    // from res in db.Results where res.file.GetHashCode() == image.GetHashCode() select res;
                    // var dres = from res in pres where res.file.SequenceEqual<byte>(image) select res;
                    //var val = pres.SingleOrDefault(); // dres.ToList();

                    if (val == null)
                        return null;

                    val.CallCount++;

                    db.Update(val);
                    db.SaveChanges();

                    return new Tuple<int, int>(val.ClassId, val.CallCount);
                }
            } catch {
                return null;
            }
        }

        private void PersistentAdd(string path, int id) {
            try {
                lock (db) {
                    db.Add(new Result { CallCount = 0, ClassId = id, resultData = new ResultData { file = File.ReadAllBytes(path) } });
                    db.SaveChanges();
                }
            } catch { }
        }

        private Tensor<float> ReadImage(string file) {
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

        public void CancelMatch() {
            tokenSource.Cancel();
        }
    }

    class Match : MarshalByRefObject, IMatch {
        Tuple<int, int> IMatch.Match(byte[] file) {
            return new Tuple<int, int>(0, 0);
        }
    }

    class Program {
        static void Main(string[] args) {
            var m = new Match();
            ChannelServices.RegisterChannel(new TcpChannel(8080), false);
            RemotingServices.Marshal(m, "match");
            Console.ReadLine();
        }
    }
}
