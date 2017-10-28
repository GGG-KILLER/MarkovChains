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
			public String Type;
		}

		public FileHeader ReadHeader ( BinaryReader br )
		{
			var header = new FileHeader
			{
				IsValid = br.ReadString ( ) == "MARKOV",
				Type = br.ReadString ( ),
				Version = br.ReadInt32 ( )
			};
			header.IsValid = header.IsValid && ( header.Type == "SPEED" || header.Type == "SPACE" ) && header.Version == 3;

			return header;
		}

		public MarkovChain Deserialize ( Stream stream )
		{
			using ( var br = new BinaryReader ( stream, Encoding.UTF8, true ) )
			{
				FileHeader header = ReadHeader ( br );
				if ( !header.IsValid )
					throw new Exception ( "Invalid Markov save file." );

				switch ( header.Type )
				{
					case "SPEED":
						return this.DeserializeForSpeed ( br );

					case "SPACE":
						return this.DeserializeForSpace ( br );

					default:
						throw new Exception ( "Invalid Markov save file." );
				}
			}
		}

		private MarkovChain DeserializeForSpeed ( BinaryReader br )
		{
			var chain = new MarkovChain ( );

			// Read node count
			var nodeCount = br.ReadInt32 ( );
			while ( nodeCount-- > 0 )
			{
				// Deserialize node info
				WordNode word = chain.GetWord ( br.ReadString ( ) );
				word.Count = br.ReadInt32 ( );

				// And add possible next words
				var subNodeCount = br.ReadInt32 ( );
				while ( subNodeCount-- > 0 )
					word.NextWords.Add ( chain.GetWord ( br.ReadString ( ) ) );
			}

			return chain;
		}

		private struct SpaceGraphItem
		{
			public String Word;
			public Int32 Count;
			public Int32[] NextWords;
		}

		private MarkovChain DeserializeForSpace ( BinaryReader br )
		{
			var chain = new MarkovChain ( );

			// Read node count
			var nodeCount = br.ReadInt32 ( );
			var nodes = new SpaceGraphItem[nodeCount];

			// Read the graph into an array temporarily
			for ( var i = 0 ; i < nodeCount ; i++ )
			{
				// Read the struct
				var node = new SpaceGraphItem
				{
					Word = br.ReadString ( ),
					Count = br.ReadInt32 ( ),
					NextWords = new Int32[br.ReadInt32 ( )]
				};

				// And the sub-items' indexes
				for ( var j = 0 ; j < node.NextWords.Length ; j++ )
					node.NextWords[j] = br.ReadInt32 ( );

				nodes[i] = node;
			}

			// After all nodes of the graph were loaded into the
			// array we can then re-transform it into a graph
			// using the indexes
			foreach ( SpaceGraphItem node in nodes )
			{
				// Get the word (whether it exists or not the
				// method will handle it)
				var word = chain.GetWord ( node.Word );
				// Set the count since it only exists at the root
				word.Count = node.Count;

				// Then transform the indexes into references to
				// other nodes
				foreach ( Int32 idx in node.NextWords )
					word.NextWords.Add ( chain.GetWord ( nodes[idx].Word ) );
			}

			return chain;
		}
	}
}
