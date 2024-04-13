using System.Runtime.Serialization;

namespace MarkovChains
{
    [Serializable]
    public class MarkovChain : ISerializable
    {
        internal readonly Dictionary<string, List<string[]>> SubSentences = new Dictionary<string, List<string[]>>();
        internal readonly List<string> SentenceInitiators = new List<string>();
        [NonSerialized]
        private readonly Random _rand;

        public IReadOnlyList<string> Initiators => SentenceInitiators;

        public MarkovChain()
        {
            _rand = new Random();
        }

        public MarkovChain(int seed)
        {
            _rand = new Random(seed);
        }

        public void Learn(string sentence)
        {
            var words = new Queue<string>(sentence.Split(new[] { ' ', '\n', '\r', '\t' }).Where(str => !string.IsNullOrWhiteSpace(str)));

            SentenceInitiators.Add(words.Peek());
            while (words.Count > 2)
            {
                // Work with pairs of first word of subsentence and remaining words of the subsentence
                var current = words.Dequeue();

                // Create the subsentence list should it not exist
                if (!SubSentences.TryGetValue(current, out List<string[]> list))
                {
                    list = new List<string[]>();
                    SubSentences[current] = list;
                }

                list.Add(words.ToArray());
            }
        }

        #region Generating

        #region Helpers

        private string[] PickNextSubSentence(string current)
        {
            List<string[]> cand;

            // If there isn't a word to start from, then pick a random one and get the sentence list of it
            if (current == null)
            {
                lock (_rand)
                {
                    cand = SubSentences
                        .Values
                        .Skip(_rand.Next(SubSentences.Count))
                        .First();
                }
            }

            // Otherwise attempt to pick the sentence piece list from the word
            else if (!SubSentences.TryGetValue(current, out cand))
            {
                // And if it fails, then return null
                return null;
            }

            // Then pick a random sentence piece to use
            lock (_rand)
            {
                return cand[_rand.Next(cand.Count)];
            }
        }

        /// <summary>
        /// Finds a sentence that starts with the last <paramref name="depth" /> words in
        /// <paramref name="words" />
        /// </summary>
        /// <param name="words">The words to use when searching</param>
        /// <param name="depth">The depth we're working with</param>
        /// <returns></returns>
        private string[] GetSubSentenceByWords(string[] words, int depth)
        {
            // If we have more words than needed, skip the ones we won't use
            if (words.Length > depth)
                words = words.Skip(words.Length - depth).ToArray();

            // If we're dealing with a sentence then do our work
            if (words.Length == 1)
                return PickNextSubSentence(words[0]);

            // Get the list of subsentences that start with the first word in our sentence
            List<string[]> subsentences = SubSentences[words[0]];

            // Then remove the first word so that we can work with the rest of the sentence
            words = words.Skip(1).ToArray();

            foreach (var subsentence in subsentences)
            {
                // If the sub-sentence
                if (subsentence.Length < words.Length)
                    continue;

                // Otherwise check if the first <depth> words equals the subsentence we're searching for
                for (var i = 0; i < words.Length; i++)
                {
                    if (words[i] != subsentence[i])
                        goto end;
                }

                // Then return the subsentence should we find it
                return subsentence;

            end:
                ;
            }

            return null;
        }

        /// <summary>
        /// Picks a random sentence initiator (for starting words)
        /// </summary>
        /// <returns></returns>
        public string GetRandomSentenceInitiator()
        {
            if (SentenceInitiators.Count > 1)
            {
                lock (_rand)
                {
                    return SentenceInitiators[_rand.Next(SentenceInitiators.Count - 1)];
                }
            }
            else
            {
                return SentenceInitiators[0];
            }
        }

        #endregion Helpers

        /// <summary>
        /// Generates a random sentence picking a random starting word (defaults to depth 2 and 40 words
        /// at max)
        /// </summary>
        /// <returns></returns>
        public string Generate() => Generate(GetRandomSentenceInitiator());

        /// <summary>
        /// Generates a sentence using <paramref name="start" /> as the start (defaults to depth 2 and 40
        /// words at max)
        /// </summary>
        /// <param name="start">Start of the sentence</param>
        /// <returns></returns>
        public string Generate(string start) => Generate(start, 2);

        /// <summary>
        /// Generates a sentence using <paramref name="start" /> as the start and
        /// <paramref name="depth" /> (defaults to 40 words max)
        /// </summary>
        /// <param name="start">Start of the sentence</param>
        /// <param name="depth">Markov depth</param>
        /// <returns></returns>
        public string Generate(string start, int depth) => Generate(start, depth, 40);

        /// <summary>
        /// Generates a sentence with <paramref name="depth" /> depth, <paramref name="start" /> as the
        /// start and at max <paramref name="maxLength" /> words
        /// </summary>
        /// <param name="start">Start of the sentence</param>
        /// <param name="depth">Markov depth</param>
        /// <param name="maxLength">Maximum word count</param>
        /// <returns></returns>
        public string Generate(string start, int depth, int maxLength)
        {
            if (start == null)
                throw new ArgumentNullException(nameof(start));
            if (depth < 1)
                throw new ArgumentException("Depth must be positive.", nameof(depth));
            if (maxLength < 1)
                throw new ArgumentException("Maximum length must be at least 1 word.", nameof(maxLength));
            var len = 0;
            var lastword = start;

            // initialize with the first word in
            var sentence = new List<string>();

            // Handle starting sentences
            if (start.Contains(' '))
            {
                // Add all words of the starting sentence from this
                var words = start.Split(new[] { ' ' });
                sentence.AddRange(words);

                var piece = GetSubSentenceByWords(words, depth);

                // Generate the first part only if there is any known sentences with this sub-sentence
                if (piece != null)
                {
                    for (var i = 0; i < depth && len < maxLength; i++)
                    {
                        sentence.Add(piece[i]);
                        len++;
                    }
                }

                // Otherwise just return what the user typed
                else
                {
                    return string.Join(" ", sentence);
                }
            }
            else
            {
                sentence.Add(start);
            }

            // Keep generating while we haven't hit the maximum length
            while (len < maxLength)
            {
                // Pick next sentence piece
                var piece = PickNextSubSentence(lastword);

                // If it was a terminator word, just quit
                if (piece == null)
                    break;

                // Otherwise continue generating the sentence
                for (var curdepth = 0; curdepth < depth && len < maxLength && curdepth < piece.Length; curdepth++)
                {
                    sentence.Add(piece[curdepth]);
                    lastword = piece[curdepth];
                    len++;
                }
            }
            return string.Join(" ", sentence);
        }

        #endregion Generating

        public void DebugPrint()
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            var b = new System.Text.StringBuilder();
            b.AppendLine("Sentence Initiators");
            b.AppendLine("{");
            foreach (var initiator in SentenceInitiators)
                b.Append("\t").AppendLine(initiator);
            b.AppendLine("}");
            b.AppendLine("Sub-sentences");
            b.AppendLine("{");
            foreach (KeyValuePair<string, List<string[]>> kv in SubSentences)
            {
                b.Append("\t")
                    .AppendLine(kv.Key)
                    .AppendLine("\t{");
                foreach (var sentence in kv.Value)
                {
                    b.Append("\t\t")
                        .AppendLine(string.Join(" ", sentence));
                }

                b.AppendLine("\t}");
            }
            b.AppendLine("}");
            Console.Write(b.ToString());
            sw.Stop();

            Console.WriteLine($"Wrote {b.Length} bytes of data to the console in {sw.ElapsedMilliseconds}ms");
        }

        #region ISeralizable

        // Deserialization
        public MarkovChain(SerializationInfo info, StreamingContext context)
        {
            _rand = new Random();
            SentenceInitiators = (List<string>) info.GetValue("initiators", typeof(List<string>));
            SubSentences = (Dictionary<string, List<string[]>>) info.GetValue("sub_sentences", typeof(Dictionary<string, List<string[]>>));
        }

        // Seralization
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("initiators", SentenceInitiators, typeof(List<string>));
            info.AddValue("sub_sentences", SubSentences, typeof(Dictionary<string, List<string[]>>));
        }

        #endregion ISeralizable
    }
}
