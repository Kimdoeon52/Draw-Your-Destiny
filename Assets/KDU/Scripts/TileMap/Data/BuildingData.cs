using UnityEngine;

// ============================================================
// BuildingData — 건물 1종의 고정 데이터 (ScriptableObject)
//
// 에디터에서 우클릭 → Create/KDU/Building 으로 생성.
// 실제로 배치된 건물의 런타임 상태는 BuildingInstance가 따로 관리.
//
// 자동 업그레이드 체인 예시 (군사 건물):
//   TribePracticeGround(isAutoUpgrade=true, upgradesTo=TrainingCamp)
//     → TrainingCamp(isAutoUpgrade=true, upgradesTo=Barracks)
//       → Barracks(isAutoUpgrade=false, upgradesTo=null)  ← 최종
// ============================================================
[CreateAssetMenu(fileName = "Building_", menuName = "KDU/Building")]
public class BuildingData : ScriptableObject
{
    [Header("기본 정보")]
    public string buildingName;
    public BuildingType buildingType;   // 코드에서 타입 분기 시 사용 (Outpost 등)
    public Sprite sprite;               // 배치 시 표시될 스프라이트

    [Header("시대 조건")]
    public Era requiredEra;             // 이 시대 이상이어야 배치/해금 가능
    public bool isAutoUpgrade;          // true면 시대 전환 시 upgradesTo로 자동 교체
    public BuildingData upgradesTo;     // 다음 단계 BuildingData SO (null = 최종 단계)

    [Header("비용")]
    public int goldCost;                // 최초 배치 시 금 소모량 (업그레이드는 무료)

    [Header("크기")]
    [Range(1, 8)]
    public int width = 1;
    [Range(1, 8)]
    public int height = 1;

    [Header("배치 제약")]
    // 이 배열에 포함된 TileType 위에만 배치 가능
    // 일반 건물: City / 농장: Farmland / Outpost: Plain
    public TileType[] allowedTiles;

    [Header("생산 효과 (턴당)")]
    public int goldPerTurn;             // 매 턴 금 생산량
    public int populationCapBonus;      // 배치 즉시 인구 한도 증가 (민가 전용)

    [Header("군사")]
    public int soldierCapacity;         // 이 건물이 수용할 수 있는 병사 수 (군사 건물 전용)
}
