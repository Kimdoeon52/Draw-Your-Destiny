using System.Collections.Generic;
using NYH.BattleCardSystem;
using UnityEngine;

/*
 * BattleUnitRosterBuilder
 *
 * 역할:
 * - NodeData의 units 리스트를 읽어 BattleUnitRoster(전투 유닛 목록 DTO)로 변환
 * - 경제 유닛(Farmer, Shoper)은 전투에 참여하지 않으므로 필터링
 * - UnitDatabase에서 보정 적용된 최종 스탯을 함께 채워줌
 *
 * 사용 시점:
 * - BattleSessionController.EnterBattle()에서 전투 진입 직전에 호출
 *
 * 의존:
 * - NodeData, NodeUnit (KDU)
 * - UnitDatabase (KDU)
 * - BattleUnitPrefabRegistry (NYH)
 */
public static class BattleUnitRosterBuilder
{
    // 전투에 참여하지 않는 경제 유닛 목록
    private static readonly HashSet<UnitType> NonCombatUnitTypes = new()
    {
        UnitType.Farmer,
        UnitType.Shoper,
    };

    // nodeData의 전투 유닛을 BattleUnitRoster로 변환
    // 경제 유닛은 제외하고, 수량이 0 이하인 엔트리도 무시
    public static BattleUnitRoster Build(
        NodeData nodeData,
        BattleTeam team,
        int civID,
        BattleUnitPrefabRegistry prefabRegistry)
    {
        BattleUnitRoster roster = new BattleUnitRoster { Team = team };
        if (nodeData == null || nodeData.units == null || prefabRegistry == null)
        {
            Debug.LogWarning("[BattleUnitRosterBuilder] Build 실패: nodeData 또는 prefabRegistry가 null입니다.");
            return roster;
        }

        for (int i = 0; i < nodeData.units.Count; i++)
        {
            NodeUnit nodeUnit = nodeData.units[i];
            if (nodeUnit == null || nodeUnit.count <= 0)
            {
                continue;
            }

            if (NonCombatUnitTypes.Contains(nodeUnit.unitType))
            {
                continue;
            }

            BattleUnit prefab = prefabRegistry.GetPrefab(nodeUnit.unitType, team);
            if (prefab == null)
            {
                Debug.LogWarning($"[BattleUnitRosterBuilder] 프리팹 없음: unitType={nodeUnit.unitType}, team={team}");
                continue;
            }

            int health = 10;
            int attackPower = 1;
            if (UnitDatabase.Instance != null)
            {
                UnitStat stat = UnitDatabase.Instance.GetFinalStat(civID, nodeUnit.unitType);
                if (stat != null)
                {
                    health = stat.maxHP;
                    attackPower = stat.baseAttack;
                }
            }

            roster.Entries.Add(new BattleUnitRosterEntry
            {
                UnitType = nodeUnit.unitType,
                Count = nodeUnit.count,
                Prefab = prefab,
                Health = health,
                AttackPower = attackPower,
            });
        }

        Debug.Log($"[BattleUnitRosterBuilder] Build 완료: team={team}, entries={roster.Entries.Count}, totalUnits={roster.TotalUnitCount}");
        return roster;
    }
}
