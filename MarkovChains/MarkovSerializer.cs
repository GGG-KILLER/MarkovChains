using System.Text;

namespace MarkovChains
{
    public class MarkovSerializer
    {
        protected static Dictionary<string, int> CreateWordLookupTable(Dictionary<string, List<string[]>> dictionary)
        {
            var wordlist = new Dictionary<string, int>();
            var i = 0;
            foreach (KeyValuePair<string, List<string[]>> kv in dictionary)
            {
                if (!wordlist.ContainsKey(kv.Key))
                    wordlist[kv.Key] = i++;

                foreach (var subsentence in kv.Value)
                {
                    foreach (var word in subsentence)
                    {
                        if (!wordlist.ContainsKey(word))
                            wordlist[word] = i++;
                    }
                }
            }
            return wordlist;
        }

        public static void Serialize(MarkovChain chain, Stream stream)
        {
            // Will be faster than calling Array.IndexOf for all words
            Dictionary<string, int> wordLUT = CreateWordLookupTable(chain.SubSentences);
            Dictionary<string, int>.KeyCollection wordList = wordLUT.Keys;

            using (var bw = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                // Write the header
                bw.Write("MARKOV"); // Magic number
                bw.Write(5);        // Version

                // Write all words
                bw.Write(wordList.Count);
                foreach (var word in wordList)
                    bw.Write(word);

                // Write sentence initiators indexes
                bw.Write(chain.SentenceInitiators.Count);
                foreach (var initiator in chain.SentenceInitiators)
                    bw.Write(wordLUT[initiator]);

                // Write amount of word and sub-sentences pairs
                bw.Write(chain.SubSentences.Count);
                foreach (KeyValuePair<string, List<string[]>> kv in chain.SubSentences)
                {
                    // Write index of the word
                    bw.Write(wordLUT[kv.Key]);

                    // Write amount of sub-sentences
                    List<string[]> subsentences = kv.Value;
                    bw.Write(subsentences.Count);
                    foreach (var subsentence in subsentences)
                    {
                        // Then write the index of each word in
                        // the sub-sentence
                        bw.Write(subsentence.Length);
                        foreach (var word in subsentence)
                            bw.Write(wordLUT[word]);
                    }
                }
            }
        }
    }
}
