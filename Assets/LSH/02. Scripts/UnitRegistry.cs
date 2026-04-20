using System.Collections.Generic;
using UnityEngine;

// 활성화된 아군 유닛을 노드별로 관리.
// 유닛은 건물 자식이 아니라 UnitsRoot 아래에 두어 건물 Destroy의 영향을 받지 않는다.
// 노드 진입 시 해당 노드 소속 유닛만 SetActive(true), 이탈 시 모두 SetActive(false).
public class UnitRegistry : Singleton<UnitRegistry>
{
    private Transform unitsRoot;
    private readonly List<Base.HumanBase> units = new List<Base.HumanBase>();

    public Transform UnitsRoot
    {
        get
        {
            if (unitsRoot == null)
            {
                GameObject go = new GameObject("UnitsRoot");
                go.transform.SetParent(transform);
                unitsRoot = go.transform;
            }
            return unitsRoot;
        }
    }

    public void Register(Base.HumanBase unit)
    {
        if (unit != null && !units.Contains(unit))
        {
            unit.transform.SetParent(UnitsRoot);
            unit.transform.localScale = Vector3.one;
            units.Add(unit);
        }
        else
            return;
            
    }

    public void Unregister(Base.HumanBase unit)
    {
        if (unit != null) units.Remove(unit);
    }

    public void ShowUnitsForNode(int nodeID)
    {
        for (int i = units.Count - 1; i >= 0; i--)
        {
            var u = units[i];
            if (u == null) { units.RemoveAt(i); continue; }
            u.gameObject.SetActive(u.homeNodeID == nodeID);
        }
    }

    public void HideAllUnits()
    {
        for (int i = units.Count - 1; i >= 0; i--)
        {
            var u = units[i];
            if (u == null) { units.RemoveAt(i); continue; }
            u.gameObject.SetActive(false);
        }
    }
}
