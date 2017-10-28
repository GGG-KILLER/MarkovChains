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
				WordNode word;

				switch ( cmd )
				{
					case "learn":
						chain.Learn ( data );
						break;

					case "learnfile":
						chain = null;
						chain = new MarkovChain ( );
						foreach ( String fline in File.ReadAllLines ( data.Trim ( ) ) )
							chain.Learn ( fline );
						break;

					case "generate":
						try
						{
							if ( data == "" )
							{
								foreach ( var sword in chain.Generate ( ) )
									Console.Write ( sword + " " );
								Console.WriteLine ( );
							}
							else
							{
								String[] parts = data.Split(new[]{' '});
								var firstWord = parts[0];
								var length = Int32.Parse ( parts[1] );
								foreach ( var sword in chain.Generate ( firstWord, length ) )
									Console.Write ( sword + " " );
								Console.WriteLine ( );
							}
						}
						catch ( Exception e )
						{
							Console.WriteLine ( e );
						}
						break;

					case "wordinfo":
						word = chain.GetWord ( data.Trim ( ) );
						Console.WriteLine ( $@"Word info for {word.Word}:
	Frequence: {word.Count}" );
						break;

					case "counts":
						Console.WriteLine ( "Counts:" );
						foreach ( WordNode sword in chain.Words.OrderBy ( w => w.Count ) )
							Console.WriteLine ( $"\t{sword.Word.PadRight ( 50, ' ' )}: {sword.Count}" );
						break;

					case "prob":
						word = chain.GetWord ( data.Trim ( ) );
						Console.WriteLine ( $"Probability of seeing {word.Word}:" );

						var tot = chain.Words.Sum ( w => w.Count );
						Console.WriteLine ( $"\tAs first word: {( ( Double ) word.Count / tot ) * 100}" );

						foreach ( WordNode sword in chain.Words )
							if ( sword.NextWords.Contains ( word ) )
								Console.WriteLine ( $"\tAfter {sword.Word}: {( ( Double ) word.Count / sword.NextWords.Sum ( w => w.Count ) ) * 100}" );
						break;

					case "save":
						try
						{
							using ( FileStream speed = File.OpenWrite ( $"{data}.speed.mk" ) )
							using ( FileStream space = File.OpenWrite ( $"{data}.space.mk" ) )
							{
								var serializer = new MarkovSerializer ( );
								serializer.SerializeForSpeed ( chain, speed );
								serializer.SerializeForSpace ( chain, space );
							}
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
							{
								var deserializer = new MarkovDeserializer ( );
								chain = deserializer.Deserialize ( file );
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
