using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MarkovChains
{
	public class MarkovSerializer
	{
		protected void WriteHeader ( BinaryWriter bw)
		{
			// Magic number
			bw.Write ( "MARKOV" );

			// Write file version (yes, 3rd iteration)
			bw.Write ( 4 );
		}

		public void Serialize ( MarkovChain chain, Stream stream )
		{
			Dictionary<String, List<String[]>> mem = chain.Memory;
			using ( var bw = new BinaryWriter ( stream, Encoding.UTF8, true ) )
			{
				// Write the header
				WriteHeader ( bw );

				// Write amount of words
				bw.Write ( mem.Count );
				foreach ( KeyValuePair<String, List<String[]>> kv in mem )
				{
					// Serialize base row
					bw.Write ( kv.Key );
					List<String[]> sentences = kv.Value;
					bw.Write ( sentences.Count );

					// Serialize possible next words (count not
					// needed as they'll appear in the root scope too)
					bw.Write ( sentences.Count );
					foreach ( String[] sentence in sentences )
					{
						// Write the sentence word array
						bw.Write ( sentence.Length );
						foreach ( var word in sentence )
							bw.Write ( word );
					}
				}
			}
		}
	}
}
