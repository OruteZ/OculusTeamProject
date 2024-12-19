using System;
using System.Collections.Generic;
using UnityEngine;

namespace Util
{
    public static class Combinatorics
    {
        public static List<List<T>> GetCombinations<T>(List<T> list, int length)
        {
            List<List<int>> indexes = Combination(list.Count, length);
            
            List<List<T>> results = new List<List<T>>();
            if (results == null) throw new ArgumentNullException(nameof(results));
            
            foreach (List<int> index in indexes)
            {
                List<T> combination = new List<T>();
                foreach (int i in index)
                {
                    combination.Add(list[i]);
                }
                results.Add(combination);
            }
            
            return results;
        }

        public static List<List<int>> Combination(int length, int cnt)
        {
            Debug.Log($"Combination({length}, {cnt})");
            
            if(cnt > length)
                throw new ArgumentException("cnt should be less than or equal to length");
            
            List<List<int>> results = new List<List<int>>();
            List<int> combination = new List<int>();
            GenerateCombinations(0, length, cnt, combination, results);
            return results;
        }

        private static void GenerateCombinations(int start, int length, int cnt, List<int> current, List<List<int>> results)
        {
            // 필요한 개수를 모두 선택한 경우 결과에 추가
            if (cnt == 0)
            {
                results.Add(new List<int>(current));
                return;
            }

            // 남은 길이를 생각했을 때 더 이상 선택할 수 없는 경우 종료
            // start부터 length-1까지 남은 개수보다 cnt가 많다면 불가능.
            for (int i = start; i <= length - cnt; i++)
            {
                current.Add(i);
                GenerateCombinations(i + 1, length, cnt - 1, current, results);
                current.RemoveAt(current.Count - 1);
            }
        }
    }
}