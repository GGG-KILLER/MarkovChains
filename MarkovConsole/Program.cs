using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Timers;
using MarkovChains;
using MarkovConsole.Commands;
using Newtonsoft.Json;

namespace MarkovConsole
{
    public enum SaveAlgos
    {
        Gzip,
        Deflate,
        None
    }

    internal class Program
    {
        private static readonly Timer timer = new Timer
        {
            Interval = 500,
            AutoReset = true
        };

        private static MarkovChain chain;
        private static CommandManager commandManager;

        private static void Main ( )
        {
            commandManager = new CommandManager ( );
            commandManager.LoadCommands ( typeof ( Program ) );

            chain = new MarkovChain ( );
            timer.Elapsed += Timer_Elapsed;
            timer.Start ( );

            while ( true )
            {
                Console.Write ( "markov> " );
                var line = Console.ReadLine ( );
                try
                {
                    commandManager.Execute ( line );
                }
                catch ( Exception e )
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine ( e.Message );
                    Console.ResetColor ( );
                }
            }
        }

        private const Double TicksPerMicrosecond = TimeSpan.TicksPerMillisecond / 1000D;

        public static String HumanTime ( Stopwatch sw )
        {
            return sw.ElapsedTicks > TimeSpan.TicksPerMinute
                ? $"{sw.Elapsed.TotalMinutes:#00.00}m"
                : sw.ElapsedTicks > TimeSpan.TicksPerSecond
                    ? $"{sw.Elapsed.TotalSeconds:#00.00}s"
                    : sw.ElapsedTicks > TimeSpan.TicksPerMillisecond
                        ? $"{sw.Elapsed.TotalMilliseconds:#00.00}ms"
                        : $"{sw.ElapsedTicks / TicksPerMicrosecond:#00.00}μs";
        }

        [Command ( "load" )]
        private static void Load ( String path, SaveAlgos algo = SaveAlgos.None )
        {
            if ( !File.Exists ( path ) )
                throw new FileNotFoundException ( "File not found." );

            using ( FileStream stream = File.OpenRead ( path ) )
            {
                var sw = Stopwatch.StartNew ( );
                try
                {
                    switch ( algo )
                    {
                        case SaveAlgos.Deflate:
                            chain = MarkovDeserializer.DeserializeDeflate ( stream );
                            break;

                        case SaveAlgos.Gzip:
                            chain = MarkovDeserializer.DeserializeGzip ( stream );
                            break;

                        case SaveAlgos.None:
                            chain = MarkovDeserializer.Deserialize ( stream );
                            break;
                    }
                }
                finally
                {
                    sw.Stop ( );
                    Console.WriteLine ( $"Time elapsed on loading: {HumanTime ( sw )}" );
                }
            }
        }

        [Command ( "save" )]
        private static void Save ( String path, SaveAlgos algo = SaveAlgos.None, CompressionLevel level = CompressionLevel.Fastest )
        {
            if ( File.Exists ( path ) )
                File.Delete ( path );

            using ( FileStream stream = File.OpenWrite ( path ) )
            {
                var sw = Stopwatch.StartNew ( );
                try
                {
                    switch ( algo )
                    {
                        case SaveAlgos.Deflate:
                            MarkovSerializer.SerializeDeflate ( chain, stream, level );
                            break;

                        case SaveAlgos.Gzip:
                            MarkovSerializer.SerializeGzip ( chain, stream, level );
                            break;

                        case SaveAlgos.None:
                            MarkovSerializer.Serialize ( chain, stream );
                            break;
                    }
                }
                finally
                {
                    sw.Stop ( );
                    Console.WriteLine ( $"Time elapsed on saving: {HumanTime ( sw )}" );
                }
            }
        }

        [Command ( "generate" )]
        private static void Generate ( String start = null, Int32 depth = 2, Int32 maxLen = Int32.MaxValue )
        {
            var sw = Stopwatch.StartNew();
            try
            {
                Console.WriteLine ( start != null ? chain.Generate ( start, depth, maxLen ) : chain.Generate ( ) );
            }
            finally
            {
                sw.Stop ( );
                Console.WriteLine ( $"Time elapsed on generating: {HumanTime ( sw )}" );
            }
        }

        [Command ( "learnfile" )]
        private static void LearnFile ( String path )
        {
            path = path.Trim ( );

            String[] sentences;
            var sw = Stopwatch.StartNew();
            try
            {
                switch ( Path.GetExtension ( path ) )
                {
                    case ".json":
                        sentences = JsonConvert.DeserializeObject<String[]> ( File.ReadAllText ( path ) );
                        break;

                    default:
                        var lines = new List<String> ( );
                        foreach ( var file in Directory.EnumerateFiles ( ".", path.Trim ( ) ) )
                        {
                            Console.WriteLine ( $"Reading from {file}" );
                            lines.AddRange ( File.ReadAllLines ( file ) );
                        }

                        sentences = lines.ToArray ( );
                        if ( sentences.Length < 1 )
                        {
                            Console.WriteLine ( "No files found or all files were empty." );
                        }
                        break;
                }
            }
            finally
            {
                sw.Stop ( );
                Console.WriteLine ( $"Time elapsed on reading the data: {HumanTime ( sw )}" );
            }

            sw = Stopwatch.StartNew ( );
            try
            {
                chain = new MarkovChain ( );
                foreach ( String sentence in sentences )
                    chain.Learn ( sentence );
            }
            finally
            {
                sw.Stop ( );
                Console.WriteLine ( $"Time elapsed learning file(s): {HumanTime ( sw )}" );
            }
        }

        private const Int64 GiB = 1024 * 1024 * 1024;

        private static void Timer_Elapsed ( Object sender, ElapsedEventArgs e )
        {
            using ( var self = Process.GetCurrentProcess ( ) )
            {
                if ( self.PrivateMemorySize64 > GiB )
                {
                    self.Kill ( );
                }
            }
        }
    }
}
