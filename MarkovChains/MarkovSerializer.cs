using System;
using System.IO;
using System.Text;

namespace MarkovChains
{
	public class MarkovSerializer
	{
		protected void WriteHeader ( BinaryWriter bw, String type )
		{
			// Magic number
			bw.Write ( "MARKOV" );

			// Save file type
			bw.Write ( type );

			// Write file version (yes, 3rd iteration)
			bw.Write ( 3 );
		}

		public void SerializeForSpeed ( MarkovChain chain, Stream stream )
		{
			WordNode[] words = chain.Words;
			using ( var bw = new BinaryWriter ( stream, Encoding.UTF8, true ) )
			{
				// Write the header
				WriteHeader ( bw, "SPEED" );

				// Write amount of words
				bw.Write ( words.Length );
				foreach ( WordNode word in words )
				{
					// Serialize node base info
					bw.Write ( word.Word );
					bw.Write ( word.Count );

					// Serialize possible next words (count not
					// needed as they'll appear in the root scope too)
					bw.Write ( word.NextWords.Count );
					foreach ( WordNode next in word.NextWords )
						bw.Write ( next.Word );
				}
			}
		}

		public void SerializeForSpace ( MarkovChain chain, Stream stream )
		{
			WordNode[] words = chain.Words;
			using ( var bw = new BinaryWriter ( stream, Encoding.UTF8, true ) )
			{
				// Write the header
				WriteHeader ( bw, "SPACE" );

				// Write amount of words
				bw.Write ( words.Length );
				foreach ( WordNode word in words )
				{
					// Serialize node base info
					bw.Write ( word.Word );
					bw.Write ( word.Count );

					// Serialize possible next words using only
					// their indexes to save space
					bw.Write ( word.NextWords.Count );
					foreach ( WordNode next in word.NextWords )
						bw.Write ( Array.IndexOf ( words, next ) );
				}
			}
		}
	}
}
