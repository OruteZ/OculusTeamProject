using System.Collections.Generic;

namespace Utility
{
    public static class Combinatorics
    {
        public static List<List<T>> GetCombinations<T>(List<T> list, int length)
        {
            List<List<T>> result = new ();
            if (length == 0)
            {
                result.Add(new List<T>());
                return result;
            }

            for (int i = 0; i <= list.Count - length; i++)
            {
                List<T> sublist = list.GetRange(i + 1, list.Count - (i + 1));
                foreach (List<T> combination in GetCombinations(sublist, length - 1))
                {
                    combination.Insert(0, list[i]);
                    result.Add(combination);
                }
            }

            return result;
        }
    }
}