using System.Collections.Generic;
using System.Linq;

namespace AtaraxiaAI.Business.Base.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Compares sentences and finds the sentence that has the most words in common.
        /// </summary>
        public static string FindBestMatch(this string stringToCompare, HashSet<string> stringsToCompareAgainst)
        {
            // https://stackoverflow.com/questions/13793560/find-closest-match-to-input-string-in-a-list-of-strings

            HashSet<string> strCompareHash = stringToCompare.Split(' ').ToHashSet();

            int maxIntersectCount = 0;
            string bestMatch = string.Empty;

            foreach (string str in stringsToCompareAgainst)
            {
                HashSet<string> strHash = str.Split(' ').ToHashSet();

                int intersectCount = strCompareHash.Intersect(strCompareHash).Count();

                if (intersectCount > maxIntersectCount)
                {
                    maxIntersectCount = intersectCount;
                    bestMatch = str;
                }
            }

            return bestMatch;
        }
    }
}
