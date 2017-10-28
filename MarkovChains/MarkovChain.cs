using System;
using System.Collections.Generic;
using System.Linq;

namespace MarkovChains
{
	public class WordNode
	{
		public String Word;
		public IList<WordNode> NextWords;
		public Int32 Count;
	}

	public class MarkovChain
	{
		private readonly Dictionary<String, WordNode> WordList = new Dictionary<String, WordNode> ( );
		public WordNode[] Words => this.WordList.Values.ToArray ( );

		public WordNode GetWord ( String word )
		{
			// Returns word if it exist otherwise create and
			// return it
			return this.WordList.ContainsKey ( word ) ?
				this.WordList[word] :
				this.WordList[word] = new WordNode
				{
					Word = word,
					NextWords = new List<WordNode> ( ),
					Count = 0
				};
		}

		public void Learn ( String sentence )
		{
			String[] words = sentence.Split ( new[] { ' ' } );
			for ( var i = 0 ; i < words.Length - 1 ; i++ )
			{
				WordNode word = GetWord ( words[i] ),
					next = GetWord ( words[i + 1] );
				word.Count++;
				next.Count++;

				if ( !word.NextWords.Contains ( next ) )
					word.NextWords.Add ( next );
			}
		}

		public void Forget ( String wordstr )
		{
			WordNode word = this.GetWord ( wordstr );
			foreach ( WordNode Word in this.Words )
				Word.NextWords.Remove ( word );
			word.NextWords.Clear ( );
			this.WordList.Remove ( wordstr );
		}

		#region Generating

		/// <summary>
		/// Generate a random word chain using a random intial
		/// word (based on occurrence) and a maximum length of 40
		/// </summary>
		/// <returns></returns>
		public IEnumerable<String> Generate ( )
		{
			WordNode[] words = this.WordList.Values.ToArray ( );
			Int32 counts = words.Sum ( w => w.Count ),
				acc = 0,
				rnd = new Random ( ).Next ( 0, counts );

			WordNode word = words[0];
			foreach ( WordNode cword in words )
			{
				word = cword;
				acc += cword.Count;
				if ( acc >= rnd )
					break;
			}

			return Generate ( word.Word, Int32.MaxValue );
		}

		public IEnumerable<String> Generate ( String firstWord )
		{
			return Generate ( firstWord, 40 );
		}

		public IEnumerable<String> Generate ( String firstWord, Int32 wordCount )
		{
			var count = 0;
			// Each generator uses it's own random instance for
			// thread safety and speed since lock would make it slower
			var rand = new Random ( );
			WordNode current = this.GetWord ( firstWord );

			// Return the first word
			yield return current.Word;

			// TODO: fix length being ignored when the same word
			//       is used twice or more times in a row
			while ( ++count < wordCount )
			{
				if ( this.WordList.ContainsKey ( current.Word ) && current.NextWords.Count > 0 )
				{
					Int32 rn = rand.Next ( 0, current.NextWords.Sum ( word => word.Count ) ),
						acc = 0;

					foreach ( WordNode word in current.NextWords )
					{
						acc += word.Count;
						current = word;
						if ( acc >= rn )
							break;
					}

					yield return current.Word;
				}
				else
					break;
			}
		}

		#endregion Generating

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
	}
}
