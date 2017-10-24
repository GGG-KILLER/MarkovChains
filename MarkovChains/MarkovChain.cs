using System;
using System.Collections.Generic;
using System.IO;

namespace MarkovChains
{
	public struct WordNode
	{
		public String Word;
		public IList<WordNode> NextWords;
	}

	public class MarkovChain
	{
		private readonly Dictionary<String, WordNode> WordList = new Dictionary<String, WordNode> ( );

		public WordNode GetWord ( String word )
		{
			return this.WordList.ContainsKey ( word ) ?
				this.WordList[word] :
				this.WordList[word] = new WordNode
				{
					Word = word,
					NextWords = new List<WordNode> ( )
				};
		}

		public void Learn ( String sentence )
		{
			String[] words = sentence.Split ( new[] { ' ' } );
			for ( var i = 0 ; i < words.Length - 1 ; i++ )
			{
				WordNode word = GetWord ( words[i] );
				WordNode next = GetWord ( words[i + 1] );

				if ( !word.NextWords.Contains ( next ) )
					word.NextWords.Add ( next );
			}
		}

		public void DebugPrint ( )
		{
			Console.WriteLine ( '{' );
			foreach ( KeyValuePair<String, WordNode> wordPair in this.WordList )
			{
				Console.WriteLine ( "\t" + wordPair.Key );
				Console.WriteLine ( "\t{" );
				foreach ( WordNode next in wordPair.Value.NextWords )
				{
					Console.WriteLine ( "\t\t" + next.Word );
				}
				Console.WriteLine ( "\t}" );
			}
			Console.WriteLine ( '}' );
		}

		public void Forget ( String word )
		{
			this.WordList[word].NextWords.Clear ( );
			this.WordList.Remove ( word );
		}

		#region Generating

		public IEnumerable<String> Generate ( )
		{
			var word = new List<String> ( this.WordList.Keys )[new Random ( ).Next ( 0, this.WordList.Count )];
			return Generate ( word, Int32.MaxValue );
		}

		public IEnumerable<String> Generate ( String firstWord )
		{
			return Generate ( firstWord, Int32.MaxValue );
		}

		public IEnumerable<String> Generate ( String firstWord, Int32 wordCount )
		{
			var count = 0;
			var rand = new Random ( );
			WordNode current = GetWord ( firstWord );

			yield return current.Word;
			while ( ++count < wordCount )
			{
				if ( this.WordList.ContainsKey ( current.Word ) && current.NextWords.Count > 0 )
				{
					var nextIdx = rand.Next ( 0, current.NextWords.Count );
					current = current.NextWords[nextIdx];
					yield return current.Word;
				}
				else
					break;
			}
		}

		#endregion Generating

		#region Save/Loading

		public void Save ( Stream stream )
		{
			using ( var writer = new StreamWriter ( stream ) )
			{
				foreach ( KeyValuePair<String, WordNode> node in this.WordList )
				{
					writer.WriteLine ( node.Key );
					foreach ( WordNode next in node.Value.NextWords )
					{
						writer.Write ( '\t' );
						writer.WriteLine ( next.Word );
					}
				}
			}
		}

		public void Load ( Stream stream )
		{
			this.WordList.Clear ( );
			using ( var reader = new StreamReader ( stream ) )
			{
				String line, currentWord = null;
				while ( ( line = reader.ReadLine ( ) ) != null )
				{
					var issub = line[0] == '\t';
					line = line.Trim ( );
					if ( issub )
					{
						if ( currentWord == null )
							throw new Exception ( "Invalid save file. (no word before next word candidates)" );

						WordNode word = this.GetWord ( line );
						this.WordList[currentWord].NextWords.Add ( word );
					}
					else
					{
						currentWord = line;
						this.WordList[currentWord] = this.GetWord ( currentWord );
					}
				}
			}
		}

		#endregion Save/Loading
	}
}
