namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    /*
     * BattleUnitRoster
     *
     * 역할:
     * - 전투 1회에 투입할 한 팀의 전체 유닛 목록을 담는 순수 데이터(DTO).
     * - Player용과 Enemy용 양쪽 다 이 클래스를 사용합니다.
     *
     * 사용 흐름:
     * - BattleUnitRosterBuilder.Build() → BattleUnitRoster 생성
     * - BattleStartContext에 담겨 BattleManager.SetupBattle()로 전달
     * - BattleStartSpawner.SpawnFromRoster()가 실제 소환
     */
    [System.Serializable]
    public class BattleUnitRoster
    {
        [Tooltip("이 로스터가 속한 팀")]
        public BattleTeam Team;

        [Tooltip("병종별 엔트리 목록")]
        public List<BattleUnitRosterEntry> Entries = new();

        // 전체 유닛 수 합산
        public int TotalUnitCount
        {
            get
            {
                int total = 0;
                for (int i = 0; i < Entries.Count; i++)
                {
                    total += Entries[i].Count;
                }

                return total;
            }
        }

        // 병종 종류 수 (드로우 규칙에서 사용)
        public int UniqueUnitTypeCount => Entries.Count;
    }
}
