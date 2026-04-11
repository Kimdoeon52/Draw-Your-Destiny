namespace NYH.CoreCardSystem
{
    using UnityEngine;

    /// <summary>
    /// 싱글톤(Singleton) 패턴
    ///  ActionSystem.Instance, CardSystem.Instance 처럼 어디서든 접근 가능하게 해줍니다.
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        // 외부에서 접근할 수 있는 유일한 인스턴스
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            // 이미 인스턴스가 있다면 새로 만들어진 오브젝트를 파괴하여 '유일성'을 유지합니다.
            if (Instance != null)
            {
                Debug.LogWarning($"[Singleton] {typeof(T).Name}의 인스턴스가 이미 존재하여 파괴합니다.");
                Destroy(gameObject);
                return;
            }
            // 현재 오브젝트를 유일한 인스턴스로 설정합니다.
            Instance = this as T;
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this as T)
            {
                Instance = null;
            }
        }

        protected virtual void OnApplicationQuit()
        {
            // 게임 종료 시 참조를 정리합니다.
            Instance = null;
        }
    }
}
