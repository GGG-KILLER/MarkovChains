using MarkovChains;
using System;
using System.IO;

namespace MarkovConsole
{
	class Program
	{
		static void Main()
		{
			var chain = new MarkovChain ( );
			while (true)
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
						foreach ( String fline in File.ReadAllLines ( data.Trim ( ) ) )
							chain.Learn ( fline );
						break;

					case "generate":
						try
						{
							if ( data == "" )
							{
								foreach ( var word in chain.Generate ( ) )
									Console.Write ( word + " " );
								Console.WriteLine ( );
							}
							else
							{
								String[] parts = data.Split(new[]{' '});
								var firstWord = parts[0];
								var length = Int32.Parse ( parts[1] );
								foreach ( var word in chain.Generate ( firstWord, length ) )
									Console.Write ( word + " " );
								Console.WriteLine ( );
							}
						}
						catch ( Exception e )
						{
							Console.WriteLine ( e );
						}
						break;

					case "save":
						try
						{
							using ( FileStream file = File.OpenWrite ( data ) )
								chain.Save ( file );
						}
						catch ( Exception e )
						{
							Console.WriteLine ( e );
						}
						break;

					case "load":
						try
						{
							using ( FileStream file = File.OpenRead ( data ) )
								chain.Load ( file );
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
