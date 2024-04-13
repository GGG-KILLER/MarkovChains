using System.Diagnostics;
using System.Runtime.Serialization.Formatters.Binary;
using System.Timers;
using MarkovChains;
using Newtonsoft.Json;
using Tsu.CLI.Commands;
using Tsu.Timing;

namespace MarkovConsole
{
    internal class Program
    {
        private static MarkovChain s_chain = null!;
        private static ConsoleCommandManager s_commandManager = new();
        private static readonly ConsoleTimingLogger s_timingLogger = new();

        private static void Main()
        {
            // Initialize the command manager
            s_commandManager = new ConsoleCommandManager();
            s_commandManager.LoadCommands<Program>(null!);
            s_commandManager.AddHelpCommand();

            s_chain = new MarkovChain();
            while (true)
            {
                Console.Write("markov> ");
                var line = Console.ReadLine();
                try
                {
                    using (s_timingLogger.BeginScope($"executing: {line}"))
                        s_commandManager.Execute(line);
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(e);
                    Console.ResetColor();
                }
            }
        }

        [Command("load"), Command("deserialize")]
        [RawInput]
        public static void Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("File not found.");

            using (s_timingLogger.BeginOperation("Deserialization"))
            using (FileStream stream = File.OpenRead(path))
            {
                var deserialized = new BinaryFormatter().Deserialize(stream);
                if (deserialized is not MarkovChain)
                    throw new InvalidDataException("Data deserialized is not a markov chain.");
                s_chain = (MarkovChain) deserialized;
            }
        }

        [Command("save"), Command("serialize")]
        [RawInput]
        public static void Save(string path)
        {
            if (File.Exists(path))
                File.Delete(path);

            using (s_timingLogger.BeginOperation("Serialization"))
            using (FileStream stream = File.OpenWrite(path))
                new BinaryFormatter().Serialize(stream, s_chain);
        }

        [Command("gen"), Command("generate")]
        public static void Generate(string? start = null, int depth = 2, int maxLen = 40)
        {
            if (string.IsNullOrWhiteSpace(start))
                start = null;

            using (s_timingLogger.BeginScope("Generating sentence"))
                s_timingLogger.WriteLine($"Generated: {(start != null ? s_chain.Generate(start, depth, maxLen) : s_chain.Generate())}");
        }

        [Command("learn"), Command("learnfrom")]
        [RawInput]
        public static void LearnFileWithPattern(string pattern)
        {
            pattern = pattern.Trim();

            var sentences = new List<string>();
            using (s_timingLogger.BeginScope("File processing"))
            {
                foreach (FileInfo file in new DirectoryInfo(".").EnumerateFiles(pattern))
                {
                    s_timingLogger.LogInformation($"Learning {file}...");
                    using var reader = new StreamReader(file.OpenRead());
                    switch (file.Extension)
                    {
                        case ".json":
                            sentences.AddRange(JsonConvert.DeserializeObject<string[]>(reader.ReadToEnd()));
                            break;

                        default:
                            while (!reader.EndOfStream)
                                sentences.Add(reader.ReadLine()!);
                            break;
                    }
                }
            }

            using (s_timingLogger.BeginOperation("Learning all files/lines"))
            {
                s_chain = new MarkovChain();
                foreach (var sentence in sentences)
                    s_chain.Learn(sentence);
            }
        }

        [Command("print"), Command("p")]
        public static void Print() => s_chain.DebugPrint();

        private const long GiB = 1024 * 1024 * 1024;

        private static void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            using var self = Process.GetCurrentProcess();
            if (self.PrivateMemorySize64 > GiB)
            {
                self.Kill();
            }
        }
    }
}
