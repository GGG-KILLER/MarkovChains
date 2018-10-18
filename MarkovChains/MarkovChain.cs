using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MarkovChains
{
    public class MarkovChain : ISerializable
    {
        internal readonly Dictionary<String, List<String[]>> SubSentences = new Dictionary<String, List<String[]>> ( );
        internal readonly List<String> SentenceInitiators = new List<String> ( );
        private readonly Random rand;

        public IReadOnlyList<String> Initiators => this.SentenceInitiators;

        public MarkovChain ( )
        {
            this.rand = new Random ( );
        }

        public MarkovChain ( Int32 seed )
        {
            this.rand = new Random ( seed );
        }

        public void Learn ( String sentence )
        {
            var words = sentence.Split ( new[] { ' ', '\n', '\r', '\t' } )
                .Where ( str => !String.IsNullOrWhiteSpace ( str ) )
                .ToArray ( );

            this.SentenceInitiators.Add ( words[0] );
            while ( words.Length > 2 )
            {
                // Work with pairs of first word of subsentence
                // and remaining words of the subsentence
                var current = words[0];
                var subsentence = words.Skip ( 1 ).ToArray ( );
                words = subsentence;

                // Create the subsentence list should it not exist
                if ( !this.SubSentences.ContainsKey ( current ) )
                    this.SubSentences[current] = new List<String[]> ( );

                this.SubSentences[current].Add ( subsentence );
            }
        }

        #region Generating

        #region Helpers

        private String[] PickNextSubSentence ( String Current )
        {
            List<String[]> cand;

            // If there isn't a word to start from, then pick a
            // random one and get the sentence list of it
            if ( Current == null )
            {
                List<String[]>[] candidates = this.SubSentences.Values.ToArray ( );
                lock ( this.rand )
                    cand = candidates[this.rand.Next ( candidates.Length )];
            }
            // Otherwise pick the sentence piece list from the word
            else if ( this.SubSentences.ContainsKey ( Current ) )
            {
                cand = this.SubSentences[Current];
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

        /// <summary>
        /// Finds a sentence that starts with the last
        /// <paramref name="depth" /> words in <paramref name="words" />
        /// </summary>
        /// <param name="words">The words to use when searching</param>
        /// <param name="depth">The depth we're working with</param>
        /// <returns></returns>
        private String[] GetSubSentenceByWords ( String[] words, Int32 depth )
        {
            // If we have more words than needed, skip the ones we
            // won't use
            if ( words.Length > depth )
                words = words.Skip ( words.Length - depth ).ToArray ( );

            // If we're dealing with a sentence then do our work
            if ( words.Length == 1 )
                return this.PickNextSubSentence ( words[0] );

            // Get the list of subsentences that start with the
            // first word in our sentence
            List<String[]> subsentences = this.SubSentences[words[0]];

            // Then remove the first word so that we can work with
            // the rest of the sentence
            words = words.Skip ( 1 ).ToArray ( );

            foreach ( var subsentence in subsentences )
            {
                // If the sub-sentence
                if ( subsentence.Length < words.Length )
                    continue;

                // Otherwise check if the first <depth> words
                // equals the subsentence we're searching for
                for ( var i = 0; i < words.Length; i++ )
                {
                    if ( words[i] != subsentence[i] )
                        goto end;
                }

                // Then return the subsentence should we find it
                return subsentence;

                end:;
            }

            return null;
        }

        /// <summary>
        /// Picks a random sentence initiator (for starting words)
        /// </summary>
        /// <returns></returns>
        public String GetRandomSentenceInitiator ( )
        {
            lock ( this.rand )
                return this.SentenceInitiators[this.rand.Next ( this.SentenceInitiators.Count )];
        }

        #endregion Helpers

        /// <summary>
        /// Generates a random sentence picking a random starting
        /// word (defaults to depth 2 and 40 words at max)
        /// </summary>
        /// <returns></returns>
        public String Generate ( ) => this.Generate ( this.GetRandomSentenceInitiator ( ) );

        /// <summary>
        /// Generates a sentence using <paramref name="start" />
        /// as the start (defaults to depth 2 and 40 words at max)
        /// </summary>
        /// <param name="start">Start of the sentence</param>
        /// <returns></returns>
        public String Generate ( String start ) => this.Generate ( start, 2 );

        /// <summary>
        /// Generates a sentence using <paramref name="start" />
        /// as the start and <paramref name="depth" /> (defaults
        /// to 40 words max)
        /// </summary>
        /// <param name="start">Start of the sentence</param>
        /// <param name="depth">Markov depth</param>
        /// <returns></returns>
        public String Generate ( String start, Int32 depth ) => this.Generate ( start, depth, 40 );

        /// <summary>
        /// Generates a sentence with <paramref name="depth" />
        /// depth, <paramref name="start" /> as the start and at
        /// max <paramref name="maxLength" /> words
        /// </summary>
        /// <param name="start">Start of the sentence</param>
        /// <param name="depth">Markov depth</param>
        /// <param name="maxLength">Maximum word count</param>
        /// <returns></returns>
        public String Generate ( String start, Int32 depth, Int32 maxLength )
        {
            if ( start == null )
                throw new ArgumentNullException ( nameof ( start ) );
            if ( depth < 1 )
                throw new ArgumentException ( "Depth must be positive.", nameof ( depth ) );
            if ( maxLength < 1 )
                throw new ArgumentException ( "Maximum length must be at least 1 word.", nameof ( maxLength ) );
            var len = 0;
            var lastword = start;

            // initialize with the first word in
            var sentence = new List<String> ( );

            // Handle starting sentences
            if ( start.Contains ( ' ' ) )
            {
                // Add all words of the starting sentence from this
                var words = start.Split ( new[] { ' ' } );
                sentence.AddRange ( words );

                var piece = this.GetSubSentenceByWords ( words, depth );
                // Generate the first part only if there is any
                // known sentences with this sub-sentence
                if ( piece != null )
                {
                    for ( var i = 0; i < depth && len < maxLength; i++ )
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
                var piece = this.PickNextSubSentence ( lastword );

                // If it was a terminator word, just quit
                if ( piece == null )
                    break;

                // Otherwise continue generating the sentence
                for ( var curdepth = 0; curdepth < depth && len < maxLength && curdepth < piece.Length; curdepth++ )
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
            var sw = new System.Diagnostics.Stopwatch ( );
            sw.Start ( );
            var b = new System.Text.StringBuilder ( );
            b.AppendLine ( "{" );
            foreach ( KeyValuePair<String, List<String[]>> kv in this.SubSentences )
            {
                b.Append ( "\t" )
                    .AppendLine ( kv.Key )
                    .AppendLine ( "\t{" );
                foreach ( var sentence in kv.Value )
                {
                    b.Append ( "\t\t" )
                        .AppendLine ( String.Join ( " ", sentence ) );
                }

                b.AppendLine ( "\t}" );
            }
            b.AppendLine ( "}" );
            Console.Write ( b.ToString ( ) );
            sw.Stop ( );

            Console.WriteLine ( $"Wrote {b.Length} bytes of data to the console in {sw.ElapsedMilliseconds}ms" );
        }

        #region ISeralizable

        // Deserialization
        public MarkovChain ( SerializationInfo info, StreamingContext context )
        {
            this.SentenceInitiators = ( List<String> ) info.GetValue ( "initiators", typeof ( List<String> ) );
            this.SubSentences = ( Dictionary<String, List<String[]>> ) info.GetValue ( "sub_sentences", typeof ( Dictionary<String, List<String[]>> ) );
        }

        // Seralization
        public void GetObjectData ( SerializationInfo info, StreamingContext context )
        {
            info.AddValue ( "initiators", this.SentenceInitiators, typeof ( List<String> ) );
            info.AddValue ( "sub_sentences", this.SubSentences, typeof ( Dictionary<String, List<String[]>> ) );
        }

        #endregion
    }
}
