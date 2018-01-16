using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MarkovChains
{
    public class MarkovSerializer
    {
        protected static void WriteHeader ( BinaryWriter bw )
        {
            // Magic number
            bw.Write ( "MARKOV" );

            // Write file version
            bw.Write ( 5 );
        }

        public static Dictionary<String, Int32> CreateWordlist ( Dictionary<String, List<String[]>> dictionary )
        {
            var wordlist = new Dictionary<String, Int32> ( );
            var i = 0;
            foreach ( KeyValuePair<String, List<String[]>> kv in dictionary )
            {
                if ( !wordlist.ContainsKey ( kv.Key ) )
                    wordlist[kv.Key] = i++;

                foreach ( String[] subsentence in kv.Value )
                    foreach ( String word in subsentence )
                        if ( !wordlist.ContainsKey ( word ) )
                            wordlist[word] = i++;
            }
            return wordlist;
        }

        public static void Serialize ( MarkovChain chain, Stream stream )
        {
            // Will be faster than calling Array.IndexOf for all words
            Dictionary<String, Int32> worddict = CreateWordlist(chain.SubSentences);
            String[] wordlist = worddict.OrderBy ( kv => kv.Value )
                .Select ( kv => kv.Key )
                .ToArray ( );

            using ( var bw = new BinaryWriter ( stream, Encoding.UTF8, true ) )
            {
                // Write the header
                WriteHeader ( bw );

                // Write all words
                bw.Write ( wordlist.Length );
                foreach ( var word in wordlist )
                    bw.Write ( word );

                // Write sentence initiators indexes
                bw.Write ( chain.SentenceInitiators.Count );
                foreach ( var initiator in chain.SentenceInitiators )
                    bw.Write ( worddict[initiator] );

                // Write amount of word and sub-sentences pairs
                bw.Write ( chain.SubSentences.Count );
                foreach ( KeyValuePair<String, List<String[]>> kv in chain.SubSentences )
                {
                    // Write index of the word
                    bw.Write ( worddict[kv.Key] );

                    // Write amount of sub-sentences
                    List<String[]> subsentences = kv.Value;
                    bw.Write ( subsentences.Count );
                    foreach ( String[] subsentence in subsentences )
                    {
                        // Then write the index of each word in
                        // the sub-sentence
                        bw.Write ( subsentence.Length );
                        foreach ( var word in subsentence )
                            bw.Write ( worddict[word] );
                    }
                }
            }
        }
    }
}
