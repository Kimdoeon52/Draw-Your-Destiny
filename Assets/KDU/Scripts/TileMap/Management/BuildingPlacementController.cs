using UnityEngine;

// ============================================================
// BuildingPlacementController — 건물 배치 입력(Input) 처리
//
// 역할: 키보드/마우스 입력을 감지해 BuildingPlacementService에 전달
// 설계 원칙: 입력 처리만 담당. 실제 배치 로직은 BuildingPlacementService에 있음
//
// 같은 GameObject에 BuildingPlacementService가 함께 붙어 있어야 한다.
//
// 현재 키 바인딩 (Inspector에서 변경 가능):
//   T        — 테스트 건물로 배치 모드 시작
//   좌클릭   — 배치 시도
//   우클릭/ESC — 배치 취소
//
// 나중에 카드 시스템이 생기면 GameManager.StartBuildingPlacement()를 통해
// 이 컨트롤러 대신 카드가 배치 모드를 시작하게 된다.
// ============================================================
public class BuildingPlacementController : MonoBehaviour
{
    [Header("References")]
    private BuildingPlacementService placementService;

    [Header("테스트용 임시 건물 (Inspector에서 BuildingData 연결)")]
    public BuildingData testBuilding;

    [Header("Key Bindings")]
    public KeyCode testModeKey = KeyCode.T;
    public KeyCode cancelKey   = KeyCode.Escape;

    private void Awake()
    {
        placementService = GetComponent<BuildingPlacementService>();
    }

    private void Update()
    {
        if (placementService == null) return;

        // T 키 — 테스트 건물로 배치 모드 시작 (배치 중이 아닐 때만)
        if (Input.GetKeyDown(testModeKey) && testBuilding != null && !placementService.IsPlacing)
        {
            placementService.StartPlacing(testBuilding);
        }

        // 배치 중이 아니면 이하 입력 무시
        if (!placementService.IsPlacing) return;

        // 매 프레임 마우스 타일 좌표 계산 → 프리뷰 위치/색상 갱신
        Vector3Int tilePos = placementService.GetMouseTilePos();
        placementService.UpdatePreview(tilePos);

        // 이거 키면 카드에서 배치할때 뺏겨서 마우스 클릭 배치 안됨
        // if (Input.GetMouseButtonDown(0))
        // {
        //     placementService.TryPlaceBuilding(tilePos);
        // }

        // 우클릭 또는 ESC — 배치 취소
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(cancelKey))
        {
            placementService.CancelPlacing();
        }
    }
}
