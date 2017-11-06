using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MarkovChains
{
	public class MarkovDeserializer
	{
		public struct FileHeader
		{
			public Boolean IsValid;
			public Int32 Version;
		}

		public FileHeader ReadHeader ( BinaryReader br )
		{
			var header = new FileHeader
			{
				IsValid = br.ReadString ( ) == "MARKOV",
				Version = br.ReadInt32 ( )
			};
			header.IsValid = header.IsValid && header.Version == 4;

			return header;
		}

		public MarkovChain Deserialize ( Stream stream )
		{
			using ( var br = new BinaryReader ( stream, Encoding.UTF8, true ) )
			{
				FileHeader header = ReadHeader ( br );
				if ( !header.IsValid )
					throw new Exception ( "Invalid Markov save file." );

				var chain = new MarkovChain ( );

				var wordcount = br.ReadInt32 ( );
				while ( wordcount-- > 0 )
				{
					var key = br.ReadString ( );
					var sentencecount = br.ReadInt32 ( );
					chain.Memory[key] = new List<String[]> ( sentencecount );

					while ( sentencecount-- > 0 )
					{
						var sentencelength = br.ReadInt32 ( );
						var sentence = new String[sentencelength];
						for ( var i = 0 ; i < sentencelength ; i++ )
							sentence[i] = br.ReadString ( );

						chain.Memory[key].Add ( sentence );
					}
				}

				return chain;
			}
		}
	}
}
