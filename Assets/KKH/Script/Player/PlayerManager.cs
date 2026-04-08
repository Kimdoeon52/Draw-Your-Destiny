using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager :   PersistentSingleton<PlayerManager>
{
    [Header("Player State")]
         [SerializeField] private Era currentEra = Era.Stone;
        public Era CurrentEra => currentEra;
   
        [Header("Player Entities")]
        [SerializeField] private PlayerLordCastle playerCastle;
        public PlayerLordCastle PlayerCastle => playerCastle;
   
        // 플레이어가 소유한 모든 유닛 리스트
        private List<HumanUnit> playerUnits = new List<HumanUnit>();
        public IReadOnlyList<HumanUnit> PlayerUnits => playerUnits;
   
        // 이벤트 (UI나 다른 시스템에서 구독)
        public event Action<Era> OnEraChanged;
        public event Action<HumanUnit> OnUnitSpawned;
        public event Action<HumanUnit> OnUnitDied;
   
        protected override void Awake()
        {
            base.Awake();
        }
   
        // 초기 영주성 설정
        public void Initialize(PlayerLordCastle castle)
        {
            playerCastle = castle;
        }
   
        // 시대 업그레이드 (GameManager의 checkResearch 로직을 이쪽으로 이동 고려)
        public void SetEra(Era newEra)
        {
            if (currentEra == newEra) return;
            currentEra = newEra;
            Debug.Log($"[PlayerManager] Era upgraded to:{newEra}");
            OnEraChanged?.Invoke(currentEra);
        }
   
        // 유닛 관리 로직
        public void RegisterUnit(HumanUnit unit)
        {
            if (!playerUnits.Contains(unit))
            {
                playerUnits.Add(unit);
                OnUnitSpawned?.Invoke(unit);
            }
        }
   
        public void UnregisterUnit(HumanUnit unit)
        {
            if (playerUnits.Remove(unit))
            {
                OnUnitDied?.Invoke(unit);
            }
        }
   
        // 유닛 생성 (기존 GameManager.GenerateHumans 로직을 이쪽으로 이동)
        public void SpawnUnit(PlayerUnitInfoByJob unitInfo,       
    Vector3 spawnPosition)
        {
            // TODO: HumanPool을 사용하여 유닛 생성 및 RegisterUnit 호출
        }
}
