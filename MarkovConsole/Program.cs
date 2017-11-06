using MarkovChains;
using System;
using System.IO;
using System.Linq;

namespace MarkovConsole
{
	internal class Program
	{
		private static void Main ( )
		{
			var chain = new MarkovChain ( );
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
						if ( !File.Exists ( data.Trim ( ) ) )
						{
							Console.WriteLine ( "File does not exist." );
							return;
						}

						chain = new MarkovChain ( );
						foreach ( String fline in File.ReadAllLines ( data.Trim ( ) ) )
						{
							Console.WriteLine ( $"Learning {fline}" );
							chain.Learn ( fline );
						}
						break;

					case "generate":
						try
						{
							var idx = data.IndexOf ( ' ' );
							if ( idx != -1 )
							{
								var sdepth = data.Substring ( 0, idx ).Trim ( );
								var start = data.Substring ( idx + 1 ).Trim ( );
								if ( Int32.TryParse ( sdepth, out Int32 depth ) )
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
						break;

					case "print":
						chain.DebugPrint ( );
						break;

					case "exit":
						return;
				}
			}
		}
	}
}
