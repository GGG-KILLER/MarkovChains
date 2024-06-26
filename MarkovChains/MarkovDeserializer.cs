﻿using System.IO.Compression;
using System.Text;

namespace MarkovChains
{
    public static class MarkovDeserializer
    {
        public struct FileHeader
        {
            public bool IsValid;
            public int Version;
        }

        public static FileHeader ReadHeader(BinaryReader br)
        {
            var header = new FileHeader
            {
                IsValid = br.ReadString() == "MARKOV",
                Version = br.ReadInt32()
            };
            header.IsValid = header.IsValid && header.Version == 5;

            return header;
        }

        public static MarkovChain DeserializeGzip(Stream stream)
        {
            using var gz = new GZipStream(stream, CompressionMode.Decompress, true);
            return Deserialize(gz);
        }

        public static MarkovChain DeserializeDeflate(Stream stream)
        {
            using var def = new DeflateStream(stream, CompressionMode.Decompress, true);
            return Deserialize(def);
        }

        public static MarkovChain Deserialize(Stream stream)
        {
            using var br = new BinaryReader(stream, Encoding.UTF8, true);
            FileHeader header = ReadHeader(br);
            if (!header.IsValid)
                throw new Exception("Invalid Markov save file.");

            var chain = new MarkovChain();

            string[] wordlist;

            // Read word dictionary
            wordlist = new string[br.ReadInt32()];
            for (var i = 0; i < wordlist.Length; i++)
                wordlist[i] = br.ReadString();

            // Read sentence intiators
            var initiatorCount = br.ReadInt32();
            while (initiatorCount-- > 0)
            {
                // Read each word by it's index
                chain.SentenceInitiators.Add(wordlist[br.ReadInt32()]);
            }

            // Read the (word, sub-sentences) pairs
            var pairCount = br.ReadInt32();
            while (pairCount-- > 0)
            {
                // Read the word by it's index
                var word = wordlist[br.ReadInt32()];

                // Read the sub-sentences
                var subsentenceCount = br.ReadInt32();
                chain.SubSentences[word] = new List<string[]>(subsentenceCount);
                while (subsentenceCount-- > 0)
                {
                    string[] subsentence = new string[br.ReadInt32()];
                    for (var i = 0; i < subsentence.Length; i++)
                    {
                        // Read each word of the sub-sentence
                        // by their indexes
                        subsentence[i] = wordlist[br.ReadInt32()];
                    }

                    chain.SubSentences[word].Add(subsentence);
                }
            }

            return chain;
        }
    }
}
