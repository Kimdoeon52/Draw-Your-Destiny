using UnityEngine;

// 건물 정보 ScriptableObject
[CreateAssetMenu(fileName = "Building_", menuName = "KDU/Building")]
public class BuildingData : ScriptableObject
{
    [Header("기본 정보")]
    public string buildingName;
    public BuildingType buildingType;
    public Sprite sprite;

    [Header("시대 조건")]
    public Era requiredEra;             // 설치 가능 최소 시대
    public bool isAutoUpgrade;          // 시대 전환 시 자동 업그레이드 여부
    public BuildingData upgradesTo;     // 업그레이드 후 데이터 (null = 최종 단계)

    [Header("비용")]
    public int goldCost;                // 최초 설치 비용 (업그레이드는 무료)

    [Header("크기")]
    [Range(1, 4)]
    public int width = 1;
    [Range(1, 4)]
    public int height = 1;

    [Header("배치 제약")]
    public TileType[] allowedTiles;

    [Header("생산 효과")]
    public int goldPerTurn;
    public int researchPerTurn;
    public int populationCapBonus;      // 민가: 인구 한도 증가량

    [Header("군사")]
    public int soldierCapacity;         // 군사 건물 전용: 병사 수용 인원
}
