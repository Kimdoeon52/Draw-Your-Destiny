namespace NYH.CoreCardSystem
{
    using UnityEngine;

    /*
     * CivilizationDeckStateStore
     *
     * 역할:
     * - 문명 씬의 현재 덱/손패/버림 상태를 전투 씬 왕복 동안 임시로 보관합니다.
     * - 저장 파일용 영구 세이브가 아니라, 씬 전환을 넘기는 런타임 보관소입니다.
     */
    public class CivilizationDeckStateStore : Singleton<CivilizationDeckStateStore>
    {
        public bool HasStoredState { get; private set; }
        public CardPileRuntimeState StoredState { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            if (Instance == this)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        public static CivilizationDeckStateStore GetOrCreate()
        {
            if (Instance != null)
            {
                return Instance;
            }

            GameObject storeObject = new(nameof(CivilizationDeckStateStore));
            return storeObject.AddComponent<CivilizationDeckStateStore>();
        }

        public void Store(CardPileRuntimeState state)
        {
            StoredState = state;
            HasStoredState = state != null;
        }

        public bool TryConsume(out CardPileRuntimeState state)
        {
            state = StoredState;
            bool hasState = HasStoredState && state != null;
            StoredState = null;
            HasStoredState = false;
            return hasState;
        }

        public void Clear()
        {
            StoredState = null;
            HasStoredState = false;
        }
    }
}
