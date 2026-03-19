namespace NYH.CoreCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    public static class ListExtensions
    {
        public static T Draw<T>(this List<T> list)
        {
            if(list.Count == 0)
            {
                return default;
            }
            int r = Random.Range(0, list.Count);
            T t = list[r];
            list.RemoveAt(r); // 객체 직접 삭제보다 인덱스 삭제가 효율적입니다.
            return t;
        }

        // ✅ 덱을 섞기 위한 Shuffle 확장 메서드 추가
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
