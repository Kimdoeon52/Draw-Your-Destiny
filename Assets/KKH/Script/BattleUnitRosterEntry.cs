namespace NYH.BattleCardSystem
{
    using UnityEngine;

    /*
     * BattleUnitRosterEntry
     *
     * 역할:
     * - 전투에 투입할 병종 1종의 요약 정보를 담는 순수 데이터(DTO).
     * - 문명 시스템(NodeData)과 전투 시스템(BattleStartSpawner) 사이 데이터 전달 전용.
     *
     * 사용하는 곳:
     * - BattleUnitRosterBuilder가 NodeData를 읽어 이 구조체를 생성합니다.
     * - BattleStartSpawner가 이 구조체를 읽어 실제 프리팹을 소환합니다.
     */
    [System.Serializable]
    public class BattleUnitRosterEntry
    {
        [Tooltip("전투 유닛 병종")]
        public UnitType UnitType;

        [Tooltip("해당 병종의 수량")]
        public int Count;

        [Tooltip("소환할 BattleUnit 프리팹")]
        public BattleUnit Prefab;

        [Tooltip("전투 시작 체력 (UnitDatabase 보정 적용 후)")]
        public int Health;

        [Tooltip("전투 시작 공격력 (UnitDatabase 보정 적용 후)")]
        public int AttackPower;
    }
}
