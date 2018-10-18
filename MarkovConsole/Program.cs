using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Timers;
using GUtils.CLI.Commands;
using GUtils.Timing;
using MarkovChains;
using Newtonsoft.Json;

namespace MarkovConsole
{
    internal class Program
    {
        private static readonly Timer timer = new Timer
        {
            Interval = 500,
            AutoReset = true
        };
        private static MarkovChain chain;
        private static CommandManager commandManager;

        private class StackingArea : TimingArea
        {
            private static readonly Stack<StackingArea> areas = new Stack<StackingArea> ( );

            public StackingArea ( String name, TimingArea parent = null ) : base ( name, parent ?? ( areas.Count > 0 ? areas.Peek ( ) : null ) )
            {
            }

            public override void Dispose ( )
            {
                base.Dispose ( );
                if ( areas.Pop ( ) != this )
                    throw new Exception ( "Popped area that wasn't at the top of the stack." );
            }
        }

        private static void Main ( )
        {
            // Initialize the command manager
            commandManager = new CommandManager ( );
            commandManager.LoadCommands<Program> ( null );
            commandManager.AddHelpCommand ( );

            // Timer to save me from myself :v
            timer.Elapsed += Timer_Elapsed;
            timer.Start ( );

            chain = new MarkovChain ( );
            while ( true )
            {
                Console.Write ( "markov> " );
                var line = Console.ReadLine ( );
                try
                {
                    using ( new StackingArea ( $"executing: {line}" ) )
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

        [Command ( "load" ), Command ( "deserialize" )]
        [RawInput]
        public static void Load ( String path )
        {
            if ( !File.Exists ( path ) )
                throw new FileNotFoundException ( "File not found." );

            using ( new StackingArea ( "Deserialization" ) )
            using ( FileStream stream = File.OpenRead ( path ) )
            {
                var formatter = new BinaryFormatter ( );
                var deserialized = formatter.Deserialize ( stream );
                if ( !( deserialized is MarkovChain ) )
                    throw new InvalidDataException ( "Data deserialized is not a markov chain." );
                chain = ( MarkovChain ) deserialized;
            }
        }

        [Command ( "save" ), Command ( "serialize" )]
        [RawInput]
        private static void Save ( String path )
        {
            if ( File.Exists ( path ) )
                File.Delete ( path );

            using ( new StackingArea ( "Serialization" ) )
            using ( FileStream stream = File.OpenWrite ( path ) )
            {
                var formatter = new BinaryFormatter ( );
                formatter.Serialize ( stream, chain );
            }
        }

        [Command ( "gen" ), Command ( "generate" )]
        private static void Generate ( String start = null, Int32 depth = 2, Int32 maxLen = 40 )
        {
            if ( String.IsNullOrWhiteSpace ( start ) )
                start = null;

            using ( new StackingArea ( "Generating sentence" ) )
                Console.WriteLine ( start != null ? chain.Generate ( start, depth, maxLen ) : chain.Generate ( ) );
        }

        [Command ( "learn" ), Command ( "learnfrom" )]
        [RawInput]
        private static void LearnFile ( String pattern )
        {
            pattern = pattern.Trim ( );
            
            var sentences = new List<String> ( );
            using ( new StackingArea ( "File processing" ) )
            {
                foreach ( FileInfo file in new DirectoryInfo ( "." ).EnumerateFiles ( pattern ) )
                {
                    using ( var reader = new StreamReader ( file.OpenRead ( ) ) )
                    {
                        switch ( file.Extension )
                        {
                            case ".json":
                                sentences.AddRange ( JsonConvert.DeserializeObject<String[]> ( reader.ReadToEnd ( ) ) );
                                break;

                            default:
                                while ( !reader.EndOfStream )
                                    sentences.Add ( reader.ReadLine ( ) );
                                break;
                        }
                    }
                }
            }

            using ( new StackingArea ( "Learning all files/lines" ) )
            {
                chain = new MarkovChain ( );
                foreach ( var sentence in sentences )
                    chain.Learn ( sentence );
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
