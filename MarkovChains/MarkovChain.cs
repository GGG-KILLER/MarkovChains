using System;
using System.Collections.Generic;
using System.Linq;

namespace MarkovChains
{
	public class MarkovChain
	{
		internal readonly Dictionary<String, List<String[]>> Memory = new Dictionary<String, List<String[]>> ( );
		private readonly Random rand = new Random ( );

		public IEnumerable<String> Words => this.Memory.Keys.AsEnumerable ( );

		public void Learn ( String sentence )
		{
			IEnumerable<String> words = sentence.Split ( new[] { ' ' } );
			while ( words.Count ( ) > 2 )
			{
				String first = words.First ( );
				String[] sent = words.Skip ( 1 ).ToArray ( );
				words = sent;

				if ( !this.Memory.ContainsKey ( first ) )
					this.Memory[first] = new List<String[]> ( );
				this.Memory[first].Add ( sent );
			}
		}

		private String[] PickNextSentencePiece ( String Current )
		{
			List<String[]> cand;

			// If there isn't a word to start from, then pick a
			// random one and get the sentence list of it
			if ( Current == null )
			{
				List<String[]>[] candidates = this.Memory.Values.ToArray ( );
				lock ( this.rand )
					cand = candidates[this.rand.Next ( candidates.Length )];
			}
			// Otherwise pick the sentence piece list from the word
			else if ( this.Memory.ContainsKey ( Current ) )
			{
				cand = this.Memory[Current];
			}
			else
			{
				return null;
			}

			// Then pick a random sentence piece to use
			lock ( this.rand )
			{
				return cand[this.rand.Next ( cand.Count )];
			}
		}

		private String[] GetSentencePieceFromWords ( String[] words, Int32 depth )
		{
			if ( words.Length > depth )
				words = words.Skip ( words.Length - depth ).ToArray ( );

			if ( words.Length > 1 )
			{
				List<String[]> pieces = this.Memory[words[0]];
				words = words.Skip ( 1 ).ToArray ( );

				foreach ( String[] piece in pieces )
				{
					if ( piece.Length < words.Length )
						continue;

					for ( var i = 0 ; i < words.Length ; i++ )
						if ( words[i] != piece[i] )
							goto end;

					return piece;

					end:;
				}

				return null;
			}
			else
			{
				return PickNextSentencePiece ( words[0] );
			}
		}

		#region Generating

		public String Generate ( )
		{
			return Generate ( PickNextSentencePiece ( null )[0] );
		}

		public String Generate ( String start )
		{
			return Generate ( start, 2 );
		}

		public String Generate ( String start, Int32 depth )
		{
			return Generate ( start, depth, 40 );
		}

		public String Generate ( String start, Int32 depth, Int32 maxLength )
		{
			var len = 0;
			var lastword = start;

			// initialize with the first word in
			var sentence = new List<String> ( );

			// Handle starting sentences
			if ( start.Contains ( ' ' ) )
			{
				// Add all words of the starting sentence from this
				String[] words = start.Split ( new[] { ' ' } );
				sentence.AddRange ( words );

				String[] piece = GetSentencePieceFromWords ( words, depth );
				// Generate the first part only if there is any
				// known sentences with this sub-sentence
				if ( piece != null )
				{
					for ( var i = 0 ; i < depth && len < maxLength ; i++ )
					{
						sentence.Add ( piece[i] );
						start = piece[i];
						len++;
					}
				}
				// Otherwise just return what the user typed
				else
				{
					return String.Join ( " ", sentence );
				}
			}
			else
			{
				sentence.Add ( start );
			}

			// Keep generating while we haven't hit the maximum length
			while ( len < maxLength )
			{
				// Pick next sentence piece
				String[] piece = this.PickNextSentencePiece ( lastword );

				// If it was a terminator word, just quit
				if ( piece == null )
					break;

				// Otherwise continue generating the sentence
				for ( var curdepth = 0 ; curdepth < depth && len < maxLength && curdepth < piece.Length ; curdepth++ )
				{
					sentence.Add ( piece[curdepth] );
					lastword = piece[curdepth];
					len++;
				}
			}
			return String.Join ( " ", sentence );
		}

		#endregion Generating

		public void DebugPrint ( )
		{
			Console.WriteLine ( '{' );
			foreach ( KeyValuePair<String, List<String[]>> kv in this.Memory )
			{
				Console.WriteLine ( kv.Key );
				Console.WriteLine ( "{" );
				foreach ( String[] sentence in kv.Value )
					Console.WriteLine ( $"\t{String.Join ( " ", sentence )}" );
				Console.WriteLine ( "}" );
			}
			Console.WriteLine ( '}' );
		}
	}
}
