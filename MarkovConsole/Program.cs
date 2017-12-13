using MarkovChains;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Timers;

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

		private static void Main ( )
		{
			chain = new MarkovChain ( );
			timer.Elapsed += Timer_Elapsed;
			timer.Start ( );

			while ( true )
			{
				Console.Write ( "markov> " );
				var line = Console.ReadLine ( );

				var firstSpace = line.IndexOf ( ' ' );
				var cmd = line.Substring ( 0, firstSpace != -1 ? firstSpace : line.Length );
				var data = firstSpace != -1 ? line.Substring ( firstSpace + 1 ) : "";

				switch ( cmd )
				{
					case "learn":
						chain.Learn ( data );
						break;

					case "learnfile":
						LearnFile ( data );
						break;

					case "generate":
						Generate ( data );
						break;

					case "print":
						chain.DebugPrint ( );
						break;

					case "exit":
						timer.Dispose ( );
						return;

					case "init":
						Console.WriteLine ( String.Join ( "\n", chain.Initiators ) );
						break;
				}
			}
		}

		private static void Generate ( String data )
		{
			try
			{
				var idx = data.IndexOf ( ' ' );
				if ( idx != -1 )
				{
					var sdepth = data.Substring ( 0, idx ).Trim ( );
					var start = data.Substring ( idx + 1 ).Trim ( );
					if ( Int32.TryParse ( sdepth, out Int32 depth ) && depth > 0 )
					{
						Console.WriteLine ( chain.Generate ( start, depth, Int32.MaxValue ) );
					}
					else if ( data.Trim ( ) != "" )
					{
						Console.WriteLine ( chain.Generate ( data, 2, Int32.MaxValue ) );
					}
				}
				else
				{
					Console.WriteLine ( chain.Generate ( ) );
				}
			}
			catch ( Exception e )
			{
				Console.WriteLine ( e );
			}
		}

		private static void LearnFile ( String data )
		{
			try
			{
				data = data.Trim ( );

				String[] sentences;
				switch ( Path.GetExtension ( data ) )
				{
					case ".json":
						sentences = JsonConvert.DeserializeObject<String[]> ( File.ReadAllText ( data ) );
						break;

					default:
						var lines = new List<String> ( );
						foreach ( var file in Directory.EnumerateFiles ( ".", data.Trim ( ) ) )
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

				chain = new MarkovChain ( );
				foreach ( String sentence in sentences )
					chain.Learn ( sentence );
			}
			catch ( Exception e )
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine ( e );
				Console.ResetColor ( );
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
