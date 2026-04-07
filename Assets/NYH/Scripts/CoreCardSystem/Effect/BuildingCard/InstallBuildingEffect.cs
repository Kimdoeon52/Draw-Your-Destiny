using NYH.CoreCardSystem;
using UnityEngine;

namespace NYH.CoreCardSystem
{
    // ============================================================
    // InstallBuildingEffect — 건물 설치 카드 이펙트
    //
    // [소규모 영지 카드 만드는 법 — NYH 참고]
    //
    // 1. CardData SO 생성 (Project 우클릭 → Create > Data > Card)
    //    - cardName: "소규모 영지"
    //    - Cost: 금 비용 설정
    //    - Effects 리스트에 "InstallBuildingEffect" 추가
    //    - buildingData 슬롯에 아래에서 만든 BuildingData SO 연결
    //
    // 2. BuildingData SO 생성 (Create > ScriptableObject > BuildingData)
    //    - buildingType: Outpost
    //    - width / height: 1 / 1  ← 클릭 지점이 곧 배치 기준점
    //    - allowedTiles: [Plain]   ← 일반 평지 타일에만 배치 가능
    //    - goldCost: 0 (카드 코스트로 이미 지불, 이 값은 PlaceBuilding에서 재차감됨)
    //
    // 3. TileMapManager Inspector에 연결 필요
    //    - outpostManorData: 영주성 2×2 BuildingData SO
    //    - cityTileAsset   : 8×8 영역에 깔릴 City 타일 에셋
    //    - farmlandTileAsset: 테두리 2칸에 깔릴 Farmland 타일 에셋
    //
    // [배치 흐름 요약 — KDU 참고]
    //   카드 사용
    //   → BuildingPlacementService.StartPlacing(buildingData)  프리뷰 시작
    //   → 플레이어 Plain 타일 클릭
    //   → CardSystem.TryQueuePlacementCard() → CanPlace() 검사
    //      (8×8 City 영역 + 2칸 Farmland 테두리 전체 범위 검사)
    //   → TileMapManager.PlaceBuilding()
    //      → 8×8 City 타일 자동 생성
    //      → 2칸 Farmland 테두리 자동 생성
    //      → 중앙에 2×2 영주성 자동 배치 (비용 없음)
    //      → 해당 영역 안개 영구 해제
    // ============================================================
    [System.Serializable]
    public class InstallBuildingEffect : PlacementEffect
    {
        [Header("설치할 건물 데이터")]
        public BuildingData buildingData;

        public override Sprite GetPreviewSprite() => buildingData != null ? buildingData.sprite : null;

        public override GameAction GetGameAction(int effectIndex = 0, Card sourceCard = null)
        {
            // 좌표는 나중에 주입되므로 초기값은 zero로 생성합니다.
            return new PlayBuildingGA(sourceCard, buildingData, Vector3Int.zero, false);
        }
    }
}
