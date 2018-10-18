using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace MarkovChains
{
    [Obsolete ( "MarkovChain now implements ISerializable, please use that instead." )]
    public static class MarkovDeserializer
    {
        public struct FileHeader
        {
            public Boolean IsValid;
            public Int32 Version;
        }

        public static FileHeader ReadHeader ( BinaryReader br )
        {
            var header = new FileHeader
            {
                IsValid = br.ReadString ( ) == "MARKOV",
                Version = br.ReadInt32 ( )
            };
            header.IsValid = header.IsValid && header.Version == 5;

            return header;
        }

        public static MarkovChain DeserializeGzip ( Stream stream )
        {
            using ( var gz = new GZipStream ( stream, CompressionMode.Decompress, true ) )
                return Deserialize ( gz );
        }

        public static MarkovChain DeserializeDeflate ( Stream stream )
        {
            using ( var def = new DeflateStream ( stream, CompressionMode.Decompress, true ) )
                return Deserialize ( def );
        }

        public static MarkovChain Deserialize ( Stream stream )
        {
            using ( var br = new BinaryReader ( stream, Encoding.UTF8, true ) )
            {
                FileHeader header = ReadHeader ( br );
                if ( !header.IsValid )
                    throw new Exception ( "Invalid Markov save file." );

                var chain = new MarkovChain ( );

                String[] wordlist;

                // Read word dictionary
                wordlist = new String[br.ReadInt32 ( )];
                for ( var i = 0; i < wordlist.Length; i++ )
                    wordlist[i] = br.ReadString ( );

                // Read sentence intiators
                var initiatorCount = br.ReadInt32 ( );
                while ( initiatorCount-- > 0 )
                {
                    // Read each word by it's index
                    chain.SentenceInitiators.Add ( wordlist[br.ReadInt32 ( )] );
                }

                // Read the (word, sub-sentences) pairs
                var pairCount = br.ReadInt32 ( );
                while ( pairCount-- > 0 )
                {
                    // Read the word by it's index
                    var word = wordlist[br.ReadInt32 ( )];

                    // Read the sub-sentences
                    var subsentenceCount = br.ReadInt32 ( );
                    chain.SubSentences[word] = new List<String[]> ( subsentenceCount );
                    while ( subsentenceCount-- > 0 )
                    {
                        String[] subsentence = new String[br.ReadInt32 ( )];
                        for ( var i = 0; i < subsentence.Length; i++ )
                        {
                            // Read each word of the sub-sentence
                            // by their indexes
                            subsentence[i] = wordlist[br.ReadInt32 ( )];
                        }

                        chain.SubSentences[word].Add ( subsentence );
                    }
                }

                for ( var i = 0; i < wordlist.Length; i++ )
                    wordlist[i] = null;
                wordlist = null;

                return chain;
            }
        }
    }
}
