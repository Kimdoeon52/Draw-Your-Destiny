using System.Collections.Generic;
using UnityEngine;

// ============================================================
// BuildingRegistry — BuildingData SO를 id 문자열로 조회하는 레지스트리
//
// CardCatalog와 동일한 패턴. Inspector에서 allBuildings에 SO 등록.
// 자동 업그레이드 체인의 모든 단계 SO 포함 (Lab_Stone, Lab_Bronze 등).
//
// 사용법:
//   BuildingRegistry.Instance.GetByID("lab_stone") → BuildingData 반환
// ============================================================
public class BuildingRegistry : Singleton<BuildingRegistry>
{
    [Header("전체 건물 SO 목록 (인스펙터에서 BuildingData SO 등록)")]
    [SerializeField] private List<BuildingData> allBuildings = new List<BuildingData>();

    private Dictionary<string, BuildingData> idMap = new Dictionary<string, BuildingData>();

    protected override void Awake()
    {
        base.Awake();
        BuildIDMap();
    }

    private void BuildIDMap()
    {
        idMap.Clear();
        foreach (BuildingData bd in allBuildings)
        {
            if (bd == null || string.IsNullOrEmpty(bd.id)) continue;
            if (idMap.ContainsKey(bd.id))
            {
                Debug.LogWarning($"[BuildingRegistry] id 중복: {bd.id} ({bd.name})");
                continue;
            }
            idMap[bd.id] = bd;
        }
        Debug.Log($"[BuildingRegistry] 건물 {idMap.Count}종 등록 완료");
    }

    // id 문자열로 BuildingData SO 조회 — 없으면 null 반환
    public BuildingData GetByID(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        idMap.TryGetValue(id, out BuildingData data);
        return data;
    }
}
