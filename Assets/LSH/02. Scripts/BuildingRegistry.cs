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

    [Header("≥Û¿Â¥Á √÷¥Î ≥Û∫Œ ºˆ")]
    [SerializeField] private int maxFarmersPerFarm = 5;

    // ≥Û¿Â∫∞ πË¡§µ» ≥Û∫Œ ∏Ò∑œ
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

        OnBuildingRemoved?.Invoke(building);
    }

    public List<BuildingInstance> GetBuildings(BuildingType type)
    {
        if (buildingsByType.TryGetValue(type, out var list))
            return list;

        return new List<BuildingInstance>();
    }

    public int GetAssignedFarmerCount(BuildingInstance farm)
    {
        if (farm == null)
            return 0;

        if (!farmAssignments.TryGetValue(farm, out var assigned))
            return 0;

        assigned.RemoveWhere(h => h == null || !h.gameObject.activeInHierarchy);
        return assigned.Count;
    }

    public bool HasVacancy(BuildingInstance farm)
    {
        if (farm == null || farm.data == null)
            return false;

        if (farm.data.buildingType != BuildingType.Farm)
            return false;

        return GetAssignedFarmerCount(farm) < maxFarmersPerFarm;
    }

    public bool TryAssignFarmer(BuildingInstance farm, HumanUnit farmer)
    {
        if (farm == null || farmer == null || farm.data == null)
            return false;

        if (farm.data.buildingType != BuildingType.Farm)
            return false;

        if (!farmAssignments.ContainsKey(farm))
            farmAssignments[farm] = new HashSet<HumanUnit>();

        HashSet<HumanUnit> assigned = farmAssignments[farm];

        assigned.RemoveWhere(h => h == null || !h.gameObject.activeInHierarchy);

        if (assigned.Contains(farmer))
            return true;

        if (assigned.Count >= maxFarmersPerFarm)
            return false;

        assigned.Add(farmer);
        return true;
    }

    public void UnassignFarmer(HumanUnit farmer)
    {
        if (farmer == null)
            return;

        foreach (var pair in farmAssignments)
        {
            if (pair.Value.Contains(farmer))
            {
                pair.Value.Remove(farmer);
                return;
            }
        }
    }

    public BuildingInstance FindNearestAvailableFarm(Vector3Int currentCell, int ownerCivID)
    {
        if (!buildingsByType.TryGetValue(BuildingType.Farm, out var farms))
            return null;

        BuildingInstance nearestFarm = null;
        float minDist = float.MaxValue;

        foreach (var farm in farms)
        {
            if (farm == null || farm.data == null)
                continue;

            if (farm.ownerCivID != ownerCivID)
                continue;

            if (!HasVacancy(farm))
                continue;

            float dist = Vector3Int.Distance(currentCell, farm.origin);
            if (dist < minDist)
            {
                minDist = dist;
                nearestFarm = farm;
            }
        }

        return nearestFarm;
    }
}