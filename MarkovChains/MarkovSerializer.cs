using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MarkovChains
{
    [Obsolete ( "MarkovChain now implements ISerializable, please use that instead." )]
    public class MarkovSerializer
    {
        protected static Dictionary<String, Int32> CreateWordLookupTable ( Dictionary<String, List<String[]>> dictionary )
        {
            var wordlist = new Dictionary<String, Int32> ( );
            var i = 0;
            foreach ( KeyValuePair<String, List<String[]>> kv in dictionary )
            {
                if ( !wordlist.ContainsKey ( kv.Key ) )
                    wordlist[kv.Key] = i++;

                foreach ( var subsentence in kv.Value )
                    foreach ( var word in subsentence )
                        if ( !wordlist.ContainsKey ( word ) )
                            wordlist[word] = i++;
            }
            return wordlist;
        }

        public static void Serialize ( MarkovChain chain, Stream stream )
        {
            // Will be faster than calling Array.IndexOf for all words
            Dictionary<String, Int32> wordLUT = CreateWordLookupTable(chain.SubSentences);
            Dictionary<String, Int32>.KeyCollection wordList = wordLUT.Keys;

            using ( var bw = new BinaryWriter ( stream, Encoding.UTF8, true ) )
            {
                // Write the header
                bw.Write ( "MARKOV" ); // Magic number
                bw.Write ( 5 );        // Version

                // Write all words
                bw.Write ( wordList.Count );
                foreach ( var word in wordList )
                    bw.Write ( word );

                // Write sentence initiators indexes
                bw.Write ( chain.SentenceInitiators.Count );
                foreach ( var initiator in chain.SentenceInitiators )
                    bw.Write ( wordLUT[initiator] );

                // Write amount of word and sub-sentences pairs
                bw.Write ( chain.SubSentences.Count );
                foreach ( KeyValuePair<String, List<String[]>> kv in chain.SubSentences )
                {
                    // Write index of the word
                    bw.Write ( wordLUT[kv.Key] );

                    // Write amount of sub-sentences
                    List<String[]> subsentences = kv.Value;
                    bw.Write ( subsentences.Count );
                    foreach ( var subsentence in subsentences )
                    {
                        // Then write the index of each word in
                        // the sub-sentence
                        bw.Write ( subsentence.Length );
                        foreach ( var word in subsentence )
                            bw.Write ( wordLUT[word] );
                    }
                }
            }
        }
    }
}
