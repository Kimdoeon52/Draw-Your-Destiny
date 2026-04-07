using System;
using System.Collections.Generic;
using UnityEngine;

public class BuildingRegistry : MonoBehaviour
{
    public static BuildingRegistry Instance { get; private set; }

    private Dictionary<BuildingType, List<BuildingInstance>> buildingsByType
        = new Dictionary<BuildingType, List<BuildingInstance>>();

    public event Action<BuildingInstance> OnBuildingRegistered;
    public event Action<BuildingInstance> OnBuildingRemoved;

    // 빈자리 알림 이벤트
    public event Action<BuildingInstance> OnFarmVacancyAvailable;

    [SerializeField] private int maxFarmersPerFarm = 5;

    private Dictionary<BuildingInstance, HashSet<HumanUnit>> farmAssignments
        = new Dictionary<BuildingInstance, HashSet<HumanUnit>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void Register(BuildingInstance building)
    {
        if (building == null || building.data == null)
            return;

        BuildingType type = building.data.buildingType;

        if (!buildingsByType.ContainsKey(type))
            buildingsByType[type] = new List<BuildingInstance>();

        if (!buildingsByType[type].Contains(building))
            buildingsByType[type].Add(building);

        if (type == BuildingType.Farm && !farmAssignments.ContainsKey(building))
            farmAssignments[building] = new HashSet<HumanUnit>();

        OnBuildingRegistered?.Invoke(building);

        // 새 농장 등록 시 한 번만 빈자리 알림
        if (type == BuildingType.Farm && HasVacancy(building))
        {
            NotifyFarmVacancy(building);
        }
    }

    public void Remove(BuildingInstance building)
    {
        if (building == null || building.data == null)
            return;

        BuildingType type = building.data.buildingType;

        if (buildingsByType.ContainsKey(type))
            buildingsByType[type].Remove(building);

        if (type == BuildingType.Farm && farmAssignments.ContainsKey(building))
            farmAssignments.Remove(building);

        OnBuildingRemoved?.Invoke(building); //?는 null 조건부 연산자임. 이 이벤트에 구독자가 있으면 호출하고, 없으면 그냥 넘어가라는 뜻임.
    }

    public List<BuildingInstance> GetBuildings(BuildingType type)
    {
        if (buildingsByType.TryGetValue(type, out var list))
            return list;

        return new List<BuildingInstance>();
    }


    public bool HasVacancy(BuildingInstance farm)
    {
        if (farm == null || farm.data == null) // 농장이 null이거나 데이터가 없으면 빈자리 여부 판단 불가니까 무시해버렷
            return false;

        if (farm.data.buildingType != BuildingType.Farm) //농장이 아니면 무시해버렸~!
            return false;

        return GetAssignedFarmerCount(farm) < maxFarmersPerFarm; // 농장에 배정된 농부 수가 최대 허용 수보다 적으면 빈자리 있음
    }
    public int GetAssignedFarmerCount(BuildingInstance farm) // 농장에 배정된 농부 수 반환, 농장이 null이거나 데이터가 없으면 0 반환
    {
        if (farm == null) //농장없으면 리턴
            return 0;

        if (!farmAssignments.TryGetValue(farm, out var assigned)) // 농장에 대한 배정 정보가 없으면 0 반환
            return 0;

        assigned.RemoveWhere(h => h == null || !h.gameObject.activeInHierarchy); // 배정된 농부 중에서 null이거나 비활성화된 농부 제거
        return assigned.Count; // 남은 배정된 농부 수 반환
    }
    public bool TryAssignFarmer(BuildingInstance farm, HumanUnit farmer)
    {//가까운 농장과 이 유닛의 배치를 시도하는 함수
        if (farm == null || farmer == null || farm.data == null) //농장이나 유닛이 없으면 false
            return false;

        if (farm.data.buildingType != BuildingType.Farm) //농장이 아니면 false
            return false;

        if (!farmAssignments.ContainsKey(farm)) // 농장에 대한 배정 정보가 없으면 새로 만들어버렷
            farmAssignments[farm] = new HashSet<HumanUnit>();

        HashSet<HumanUnit> assigned = farmAssignments[farm]; // 해당 농장에 배정된 농부 집합
        assigned.RemoveWhere(h => h == null || !h.gameObject.activeInHierarchy); // 배정된 농부 중에서 null이거나 비활성화된 농부 제거

        if (assigned.Contains(farmer)) // 이미 배정된 농부면
            return true;

        if (assigned.Count >= maxFarmersPerFarm) // 이미 최대 배정 수에 도달했으면
            return false;

        assigned.Add(farmer); // 농부를 배정
        return true;
    }

    public void UnassignFarmer(HumanUnit farmer) // 농부의 배정을 해제하는 함수
    {
        if (farmer == null) // 농부가 null이면 리턴
            return;

        foreach (var pair in farmAssignments) // 모든 농장에 대해
        {
            if (pair.Value.Contains(farmer)) // 해당 농장에 이 농부가 배정되어 있으면
            {
                pair.Value.Remove(farmer); // 배정 해제
                return;
            }
        }
    }

    public BuildingInstance FindNearestAvailableFarm(Vector3Int currentCell, int ownerCivID)
    {// 이 메서드는 현재 위치와 소유 문명 ID를 기준으로 가장 가까운 빈자리가 있는 농장을 찾는거임.
        if (!buildingsByType.TryGetValue(BuildingType.Farm, out var farms))// 농장이 하나도 없으면 null 반환
            return null;

        BuildingInstance nearestFarm = null; // 가장 가까운 농장 초기화
        float minDist = float.MaxValue; // 최소 거리 초기화
        foreach (var farm in farms) // 모든 농장 순회
        {
            if (farm == null || farm.data == null) //농장이 없으면 무시
                continue;

            if (farm.ownerCivID != ownerCivID) // 소유 문명과 다르면 무시
                continue;

            if (!HasVacancy(farm)) // 빈자리가 없으면 무시
                continue;

            float dist = Vector3Int.Distance(currentCell, farm.origin); //현재 위치와 농장 위치 간의 거리 계산
            if (dist < minDist) //계산된 거리가 현재까지 발견된 최소 거리보다 작으면
            {
                minDist = dist; // 최소 거리 업데이트
                nearestFarm = farm; // 가장 가까운 농장 업데이트
            }
        }

        return nearestFarm; // 가장 가까운 농장 반환, 없으면 null 반환
    }

    // 외부에서 안전하게 빈자리 알림을 보낼 때만 사용
    public void NotifyFarmVacancy(BuildingInstance farm)
    {
        if (farm == null || farm.data == null) //농장이 없으면 return
            return;

        if (farm.data.buildingType != BuildingType.Farm) //농장이 아니라면 return
            return;

        if (!HasVacancy(farm)) // 빈자리가 없으면 return
            return;

        OnFarmVacancyAvailable?.Invoke(farm); // 빈자리 알림 이벤트 호출
    }
}