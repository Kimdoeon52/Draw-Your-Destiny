using System.Collections.Generic;
using UnityEngine;

// ============================================================
// UnitDatabase — 유닛 스탯 DB + 보정 시스템 (싱글톤)
//
// ■ 베이스 스탯: Inspector에서 편집. 전 문명 공유, 런타임 수정 금지.
// ■ 보정(Modifier): 카드/건물/시대 효과로 문명별 런타임 추가/제거.
// ■ 최종 스탯: GetFinalStat()이 베이스 + 해당 civID 보정 합산 반환.
//
// ── 사용법 ──────────────────────────────────────────────────
//
// 베이스만 필요할 때 (전투 시스템):
//   UnitStat base = UnitDatabase.Instance.GetStat(UnitType.RockWarrior);
//
// 버프 적용 (카드 시스템):
//   var mod = new UnitModifier { bonusAttack = 3, source = "전사의 격려" };
//   UnitDatabase.Instance.AddModifier(civID, UnitType.RockWarrior, mod);
//
// 버프 해제:
//   UnitDatabase.Instance.RemoveModifier(civID, UnitType.RockWarrior, mod);
//
// 최종 스탯 조회 (베이스 + 보정 합산):
//   UnitStat final = UnitDatabase.Instance.GetFinalStat(civID, UnitType.RockWarrior);
//
// 문명 보정 초기화 (게임 리셋 등):
//   UnitDatabase.Instance.ClearModifiers(civID);
// ============================================================
public class UnitDatabase : Singleton<UnitDatabase>
{
    [SerializeField] private List<UnitStat> unitStats = new List<UnitStat>();

    public IReadOnlyList<UnitStat> Stats => unitStats;

    // civID → (UnitType → 보정 리스트)
    private Dictionary<int, Dictionary<UnitType, List<UnitModifier>>> modifiers
        = new Dictionary<int, Dictionary<UnitType, List<UnitModifier>>>();

    // ── 베이스 스탯 조회 ────────────────────────────────────
    public UnitStat GetStat(UnitType type)
    {
        for (int i = 0; i < unitStats.Count; i++)
        {
            if (unitStats[i].unitType == type) return unitStats[i];
        }
        return null;
    }

    // ── 최종 스탯 조회 (베이스 + 보정 합산) ─────────────────
    public UnitStat GetFinalStat(int civID, UnitType type)
    {
        UnitStat baseStat = GetStat(type);
        if (baseStat == null) return null;

        int totalAtk = 0;
        int totalDef = 0;
        int totalHP  = 0;

        List<UnitModifier> mods = GetModifiers(civID, type);
        if (mods != null)
        {
            for (int i = 0; i < mods.Count; i++)
            {
                totalAtk += mods[i].bonusAttack;
                totalDef += mods[i].bonusDefense;
                totalHP  += mods[i].bonusHP;
            }
        }

        return new UnitStat
        {
            unitName    = baseStat.unitName,
            unitType    = baseStat.unitType,
            baseAttack  = baseStat.baseAttack  + totalAtk,
            baseDefense = baseStat.baseDefense + totalDef,
            maxHP       = baseStat.maxHP       + totalHP,
            requiredEra = baseStat.requiredEra
        };
    }

    // ── 보정 추가 ───────────────────────────────────────────
    public void AddModifier(int civID, UnitType type, UnitModifier mod)
    {
        if (mod == null) return;

        if (!modifiers.ContainsKey(civID))
            modifiers[civID] = new Dictionary<UnitType, List<UnitModifier>>();

        if (!modifiers[civID].ContainsKey(type))
            modifiers[civID][type] = new List<UnitModifier>();

        modifiers[civID][type].Add(mod);
    }

    // ── 보정 제거 ───────────────────────────────────────────
    public void RemoveModifier(int civID, UnitType type, UnitModifier mod)
    {
        List<UnitModifier> mods = GetModifiers(civID, type);
        if (mods != null)
            mods.Remove(mod);
    }

    // ── 문명 전체 보정 초기화 ───────────────────────────────
    public void ClearModifiers(int civID)
    {
        if (modifiers.ContainsKey(civID))
            modifiers[civID].Clear();
    }

    // ── 보정 전체 Export (SaveManager용) ──────────────────
    public List<SaveUnitModifierEntry> ExportAllModifiers()
    {
        List<SaveUnitModifierEntry> result = new List<SaveUnitModifierEntry>();
        foreach (var civPair in modifiers)
        {
            int civID = civPair.Key;
            foreach (var typePair in civPair.Value)
            {
                UnitType unitType = typePair.Key;
                foreach (UnitModifier mod in typePair.Value)
                {
                    result.Add(new SaveUnitModifierEntry
                    {
                        civID        = civID,
                        unitType     = unitType,
                        bonusAttack  = mod.bonusAttack,
                        bonusDefense = mod.bonusDefense,
                        bonusHP      = mod.bonusHP,
                        source       = mod.source
                    });
                }
            }
        }
        return result;
    }

    // ── 보정 전체 Import (SaveManager용) ──────────────────
    public void ImportAllModifiers(List<SaveUnitModifierEntry> entries)
    {
        modifiers.Clear();
        if (entries == null) return;
        foreach (SaveUnitModifierEntry e in entries)
        {
            AddModifier(e.civID, e.unitType, new UnitModifier
            {
                bonusAttack  = e.bonusAttack,
                bonusDefense = e.bonusDefense,
                bonusHP      = e.bonusHP,
                source       = e.source
            });
        }
    }

    // ── 내부: 보정 리스트 조회 ──────────────────────────────
    private List<UnitModifier> GetModifiers(int civID, UnitType type)
    {
        if (modifiers.TryGetValue(civID, out var byType))
        {
            if (byType.TryGetValue(type, out var list))
                return list;
        }
        return null;
    }
}
