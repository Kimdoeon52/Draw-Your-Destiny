namespace NYH.CoreCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    public static class ListExtensions
    {
        public static T Draw<T>(this List<T> list)
        {
            if (list.Count == 0)
            {
                return default;
            }
            T t = list[0];
            list.RemoveAt(0);
            return t;
        }

        //덱을 섞기 위한 Shuffle 확장 메서드 추가
        public static void Shuffle<T>(this List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                T temp = list[i];
                int randomIndex = Random.Range(i, list.Count);
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }
        }

        public static void AddRange<T>(this List<T> list, List<T> other)
        {
            foreach (var item in other)
            {
                list.Add(item);
            }
        }
    }
}
