using ResNetMatcher;
using System;
using System.IO;
using System.Threading;

namespace Task1 {
    class Program {

        static void Callback(string image, int id, int storeCount) {
            // I/O operations using these streams are synchronized, 
            //  which means multiple threads can read from, or write to, 
            //  the streams.
            Console.WriteLine($"{image} is: {Labels.classLabels[id]}");
        }

        static void Main(string[] args) {
            Matcher matcher = new Matcher();

            try {
                if (args.Length == 0)
                    matcher.Match(Callback, "Data");
                else
                    matcher.Match(Callback, args[0]);

                Console.WriteLine($"Data path is: {(args.Length == 0 ? "Data" : args[0])}");
                Console.WriteLine("Press ESC to exit");
                while (Console.ReadKey().Key != ConsoleKey.Escape)
                    ;

                matcher.CancelMatch();
            } catch (FileNotFoundException e) {
                Console.WriteLine(e.Message);
            }
        }
    }
}
