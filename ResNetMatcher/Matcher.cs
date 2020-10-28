using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ResNetMatcher {
    public class Matcher {
        private InferenceSession session;
        private ConcurrentQueue<string> images = new ConcurrentQueue<string>();
        Action<string, int, float> async_handler;
        CancellationTokenSource tokenSource;
        Task[] tasks;

        public void Match(Action<string, int, float> async_handler, string model_data_path) {
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

                        var tensor = ReadImage(image);
                        var predict = PredictImage(tensor);
                        async_handler(image, predict.Item1, predict.Item2);
                    }
                }, tokenSource.Token);
        }

        private Tensor<float> ReadImage(string file) {
            using var image = Image.Load<Rgb24>(file);

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

        private Tuple<int, float> PredictImage(Tensor<float> tensor) {
            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("data", tensor) };
            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = session.Run(inputs);

            var output = results.First().AsEnumerable<float>().ToArray();
            var sum = output.Sum(x => (float)Math.Exp(x));
            var softmax = output.Select(x => (float)Math.Exp(x) / sum);

            return softmax
                .Select((x, i) => new Tuple<int, float>(i, x))
                .OrderByDescending(x => x.Item2)
                .Take(1).First();
        }

        public void CancelMatch() {
            tokenSource.Cancel();
        }
    }
}
