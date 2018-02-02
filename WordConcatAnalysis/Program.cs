using System;
using System.Collections.Generic;
using System.Linq;

namespace Analyser
{
    class Program
    {
        static void Main()
        {
            // step 1, read the file. To make simple, path is hardcoded
            var file = System.IO.Path.Combine(new System.IO.DirectoryInfo(Environment.CurrentDirectory).Parent.Parent.FullName, "wordlist.txt");
            var words = new List<WordBreaker>();
            using (var sr = new System.IO.StreamReader(file))
            {
                for (int i = 0; !sr.EndOfStream; i++)
                {
                    var line = sr.ReadLine().Trim();
                    if (!string.IsNullOrEmpty(line))
                    {
                        words.Add(new WordBreaker(line, i));
                    }
                }
            }

            // Step 2, fill the universe, LINQ sort is quick enough for this data volume
            WordUniverse = words
                // group by word length, w/in a group order by word alphabetically
                .GroupBy(wordBuilder => wordBuilder.Word.Length, (k, g) => new { Length = k, Group = g.OrderBy(wordBuilder => wordBuilder.Word), })
                .ToDictionary(g => g.Length, g => g.Group.ToArray());

            var queue = new Queue<WordBreaker>(WordUniverse
                // longest go first
                .OrderByDescending(kv => kv.Key)
                .SelectMany(kv => kv.Value));
            int count = 0;

            // Step 3, find 2 breakables only
            for (WordBreaker breakable; ((breakable = TryBreakOne(queue)) != null) // if not a full scan
                && (count < 2);) // if we need only first 2 breakables
            {
                if (breakable.Parts.Any())
                {
                    count++;
                    Console.WriteLine($"Count = {count}: {breakable.Word} = {string.Join(",", breakable.Parts.Select(part => part.Word).Reverse())}");
                }
            }

            // step 4, how many breakables totally?
            for (WordBreaker breakable; ((breakable = TryBreakOne(queue)) != null); // if not a full scan
                count += (breakable.Parts.Any() ? 1 : 0)) ;
            Console.WriteLine($"Total Count = {count}");
        }

        private static Dictionary<int, WordBreaker[]> WordUniverse = new Dictionary<int, WordBreaker[]>();

        // this function tries to break the top 1 from all left
        private static WordBreaker TryBreakOne(Queue<WordBreaker> queue)
        {
            if (queue.Count == 0)
            {
                return null; // all tested
            }
            var breakable = queue.Dequeue();
            breakable.Break(breakable.Word);
            return breakable;
        }

        private class WordBreaker
        {
            public string Word { get; }
            public int Index { get; }
            public List<WordBreaker> Parts { get; } = new List<WordBreaker>();

            public WordBreaker(string word, int index)
            {
                Word = word;
                Index = index;
            }


            // this function searches the universe for an exact match, assuming all lowercase
            private bool TryMatch(string word, out WordBreaker breaker)
            {
                WordBreaker[] list;
                if (WordUniverse.TryGetValue(word.Length, out list)) // we have same length words
                {
                    breaker = list
                        // cannot match myself
                        .Where(wordBreaker => wordBreaker.Index != Index)
                        .FirstOrDefault(wordBreaker => wordBreaker.Word == word);
                }
                else
                {
                    breaker = null;
                }
                return null != breaker;
            }

            // this function uses the universe to break myself
            public bool Break(string word)
            {
                if (word.Length == 0)
                {
                    return true; // recursion will step back from here
                }

                for (int i = word.Length; i > 0; i--) // start from searching longest match
                {
                    WordBreaker breaker;
                    if (TryMatch(word.Substring(0, i), out breaker) && Break(word.Substring(i)))
                    {
                        Parts.Add(breaker);
                        return true;
                    }
                }

                Parts.Clear();
                return false;
            }
        }
    }
}
