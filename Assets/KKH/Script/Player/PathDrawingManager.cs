using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Cysharp.Threading.Tasks;

/// <summary>
/// 타일맵 위에 마우스 클릭으로 이동 경로를 그리고,
/// Spacebar 입력 시 유닛을 해당 경로를 따라 비동기(UniTask)로 이동시키는 테스트용 매니저.
/// 카드 SO가 완성되기 전까지 Inspector의 testMaxMove 값으로 이동 칸 수를 대체한다.
/// </summary>
/// <remarks>
/// ──────────────────────────────────────────────────────────────
/// ■ 씬 배치 가이드
/// ──────────────────────────────────────────────────────────────
///   1. 빈 GameObject 생성 → 이름: "PathDrawingManager"
///   2. 이 스크립트를 부착
///   3. Inspector에서 아래 필드를 할당:
///      ┌──────────────────┬──────────────────────────────────┐
///      │ 필드              │ 할당 대상                         │
///      ├──────────────────┼──────────────────────────────────┤
///      │ Grid             │ 씬의 Grid 오브젝트 (Tilemap 부모) │
///      │ Tilemap          │ Ground 타일맵                     │
///      │ Player Unit      │ 이동시킬 유닛 Transform           │
///      │ Blue Tile Prefab │ 파란색 사각형 시각화 프리팹         │
///      │ Main Camera      │ 메인 카메라 (비워두면 자동 할당)    │
///      │ Enemy Layer Mask │ 적 유닛 레이어                    │
///      └──────────────────┴──────────────────────────────────┘
///
/// ■ BlueTilePrefab 만드는 법
///   1. 빈 GameObject → SpriteRenderer 추가
///   2. Sprite: Unity 기본 "Square"
///   3. Color: (R:0.2, G:0.4, B:1.0, A:0.6)
///   4. Scale: (0.9, 0.9, 1) — Grid 셀 크기 1 기준
///   5. Order in Layer: 10 (타일맵보다 위)
///   6. Project 폴더에 드래그하여 프리팹 저장
///
/// ■ 조작법
///   좌클릭   — 인접 타일에 경로 추가 (이미 있는 타일 클릭 시 Undo)
///   Spacebar — 경로를 따라 유닛 이동 실행
///   R 키     — 경로 수동 초기화
///
/// ■ 카드 시스템 연동 시 교체 포인트
///   - testMaxMove → cardData.moveCount 로 교체
///   - MoveToNextCell() → 기존 이동 함수 호출로 교체
/// ──────────────────────────────────────────────────────────────
/// </remarks>
public class PathDrawingManager : MonoBehaviour
{
    #region ── Inspector: 씬 레퍼런스 ──

    [Header("씬 레퍼런스")]
    [Tooltip("씬의 Grid 컴포넌트 (Tilemap의 부모 오브젝트)")]
    [SerializeField] private Grid grid;

    [Tooltip("경로를 그릴 대상 Tilemap (HasTile 검증에 사용)")]
    [SerializeField] private Tilemap tilemap;

    [Tooltip("이동시킬 플레이어 유닛의 Transform")]
    [SerializeField] private Transform playerUnit;

    [Tooltip("경로 시각화용 파란색 타일 프리팹")]
    [SerializeField] private GameObject blueTilePrefab;

    [Tooltip("메인 카메라 (미할당 시 Camera.main 자동 사용)")]
    [SerializeField] private Camera mainCamera;

    #endregion

    #region ── Inspector: 테스트 설정 (카드 SO 대체) ──

    [Header("테스트 설정 (카드 SO 대체)")]
    [Tooltip("이동 가능한 최대 칸 수 (예: 2 = 진군, 4 = 급속 진격)")]
    [SerializeField] private int testMaxMove = 4;

    [Tooltip("타일 간 이동 속도 (units/sec)")]
    [SerializeField] private float moveSpeed = 5f;

    #endregion

    #region ── Inspector: 위치 보정 및 충돌 ──

    [Header("위치 보정 및 충돌")]
    [Tooltip("유닛 피벗이 하단일 경우 타일 중앙으로 보정하는 오프셋 (Y를 조절)")]
    [SerializeField] private Vector3 unitPositionOffset = new Vector3(0f, 0.5f, 0f);

    [Tooltip("적 유닛이 속한 레이어 (해당 레이어 콜라이더가 있으면 경로 추가 차단)")]
    [SerializeField] private LayerMask enemyLayerMask;

    #endregion

    #region ── 내부 상태 (런타임) ──

    /// <summary>경로를 구성하는 Grid 셀 좌표 리스트.</summary>
    private List<Vector3Int> pathCells = new List<Vector3Int>();

    /// <summary>경로 시각화용으로 생성된 프리팹 인스턴스 리스트.</summary>
    private List<GameObject> pathVisuals = new List<GameObject>();

    /// <summary>유닛이 이동 중이면 true. 이동 중에는 모든 입력을 차단한다.</summary>
    private bool isMoving = false;

    #endregion

    // =====================================================================
    #region ── Unity 생명주기 ──
    // =====================================================================

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Update()
    {
        if (isMoving) return;

        if (Input.GetMouseButtonDown(0))
            HandleMouseClick();

        if (Input.GetKeyDown(KeyCode.Space))
            TryExecuteMove();

        if (Input.GetKeyDown(KeyCode.R))
        {
            ClearPath();
            Debug.Log("[PathDrawingManager] 경로가 수동으로 초기화되었습니다.");
        }
    }

    #endregion

    // =====================================================================
    #region ── 입력 처리: 마우스 클릭 → 경로 추가 / Undo ──
    // =====================================================================

    /// <summary>
    /// 마우스 좌클릭 시 호출. 클릭 위치를 Grid 셀로 변환한 뒤
    /// 유효성 검사(범위·인접·중복·적 존재)를 거쳐 경로에 추가한다.
    /// </summary>
    private void HandleMouseClick()
    {
        // ① 스크린 → 월드 좌표 변환
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        // ② 월드 → Grid 셀 좌표 변환
        Vector3Int clickedCell = grid.WorldToCell(mouseWorldPos);

        // ③ 타일맵 범위 검증 (음수 좌표 포함, 범위 밖 차단)
        if (!tilemap.HasTile(clickedCell))
        {
            Debug.LogWarning($"[PathDrawingManager] 셀 {clickedCell} — 타일 없음. 타일맵 범위 내만 선택 가능.");
            return;
        }

        // ④ 이미 경로에 있는 타일 → Undo (해당 지점부터 끝까지 제거)
        int existingIndex = pathCells.IndexOf(clickedCell);
        if (existingIndex >= 0)
        {
            UndoPathFrom(existingIndex);
            Debug.Log($"[PathDrawingManager] Undo → 셀 {clickedCell} 이후 제거. 남은 경로: {pathCells.Count}");
            return;
        }

        // ⑤ 최대 이동 칸 수 초과 확인
        if (pathCells.Count >= testMaxMove)
        {
            Debug.LogWarning($"[PathDrawingManager] 최대 이동 칸 수({testMaxMove})에 도달.");
            return;
        }

        // ⑥ 인접성 검증 (상하좌우만 허용, 대각선 불가)
        Vector3Int lastCell = GetLastPathCell();
        if (!IsAdjacent(lastCell, clickedCell))
        {
            Debug.LogWarning($"[PathDrawingManager] 셀 {clickedCell} — {lastCell}과 인접하지 않음.");
            return;
        }

        // ⑦ 유닛 현재 위치와 동일한 타일 차단
        Vector3Int unitCell = grid.WorldToCell(playerUnit.position);
        if (clickedCell == unitCell)
        {
            Debug.LogWarning("[PathDrawingManager] 유닛 현재 위치에는 경로를 추가할 수 없습니다.");
            return;
        }

        // ⑧ 적군 유닛 존재 여부 (Physics2D 콜라이더 검사)
        Vector3 cellCenter = grid.GetCellCenterWorld(clickedCell);
        if (Physics2D.OverlapPoint(cellCenter, enemyLayerMask) != null)
        {
            Debug.LogWarning("[PathDrawingManager] 해당 타일에 적 유닛이 존재하여 경로 추가 불가.");
            return;
        }

        // ⑨ 모든 검증 통과 → 경로에 추가
        AddCellToPath(clickedCell);
        Debug.Log($"[PathDrawingManager] 경로 추가: {clickedCell} ({pathCells.Count}/{testMaxMove})");
    }

    /// <summary>
    /// Spacebar 입력 시 호출. 경로가 있으면 이동 실행, 없으면 경고 로그.
    /// </summary>
    private void TryExecuteMove()
    {
        if (pathCells.Count > 0)
        {
            ExecuteMove().Forget();
        }
        else
        {
            Debug.LogWarning("[PathDrawingManager] 경로가 비어있습니다. 먼저 클릭으로 경로를 그려주세요.");
        }
    }

    #endregion

    // =====================================================================
    #region ── 경로 리스트 조작 ──
    // =====================================================================

    /// <summary>
    /// 경로에 새 셀을 추가하고, 해당 위치에 시각화 프리팹을 생성한다.
    /// </summary>
    private void AddCellToPath(Vector3Int cell)
    {
        pathCells.Add(cell);

        if (blueTilePrefab != null)
        {
            Vector3 centerPos = grid.GetCellCenterWorld(cell);
            GameObject visual = Instantiate(blueTilePrefab, centerPos, Quaternion.identity);
            pathVisuals.Add(visual);
        }
    }

    /// <summary>
    /// fromIndex 이후의 경로를 모두 되돌린다 (Undo).
    /// 해당 인덱스의 타일 자체도 제거된다.
    /// </summary>
    private void UndoPathFrom(int fromIndex)
    {
        for (int i = pathVisuals.Count - 1; i >= fromIndex; i--)
        {
            if (pathVisuals[i] != null)
                Destroy(pathVisuals[i]);
            pathVisuals.RemoveAt(i);
        }

        pathCells.RemoveRange(fromIndex, pathCells.Count - fromIndex);
    }

    /// <summary>
    /// 경로 시각화 프리팹을 모두 파괴하고, 경로 리스트를 초기화한다.
    /// </summary>
    private void ClearPath()
    {
        foreach (var visual in pathVisuals)
        {
            if (visual != null) Destroy(visual);
        }
        pathVisuals.Clear();
        pathCells.Clear();
    }

    #endregion

    // =====================================================================
    #region ── 유틸리티 ──
    // =====================================================================

    /// <summary>
    /// 경로의 마지막 셀을 반환한다.
    /// 경로가 비어있으면 유닛의 현재 Grid 셀을 반환한다.
    /// </summary>
    private Vector3Int GetLastPathCell()
    {
        return pathCells.Count > 0
            ? pathCells[pathCells.Count - 1]
            : grid.WorldToCell(playerUnit.position);
    }

    /// <summary>
    /// 두 Grid 셀이 상하좌우로 인접한지 판별한다 (맨해튼 거리 == 1).
    /// </summary>
    private bool IsAdjacent(Vector3Int a, Vector3Int b)
    {
        return (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y)) == 1;
    }

    #endregion

    // =====================================================================
    #region ── 유닛 비동기 이동 (UniTask) ──
    // =====================================================================

    /// <summary>
    /// 경로 리스트(pathCells)를 순차적으로 따라가며 유닛을 이동시킨다.
    /// 이동 완료 후 시각화를 정리하고 경로를 초기화한다.
    /// </summary>
    private async UniTaskVoid ExecuteMove()
    {
        isMoving = true;
        Debug.Log($"[PathDrawingManager] ▶ 이동 시작 — 경로 {pathCells.Count}칸");

        foreach (Vector3Int cell in pathCells)
        {
            Vector3 targetPos = grid.GetCellCenterWorld(cell) + unitPositionOffset;
            await MoveToNextCell(targetPos);
        }

        Debug.Log("[PathDrawingManager] ■ 이동 완료 — 경로 초기화");
        ClearPath();
        isMoving = false;
    }

    /// <summary>
    /// 유닛을 현재 위치에서 targetPos까지 MoveTowards로 부드럽게 이동시킨다.
    /// </summary>
    /// <remarks>
    /// ★ 카드 시스템 연동 시 교체 포인트 ★
    /// 기존 프로젝트의 이동 함수가 있다면 이 메서드 내용을 한 줄로 교체:
    ///   await unitMover.MoveToAsync(targetPos);
    /// </remarks>
    private async UniTask MoveToNextCell(Vector3 targetPos)
    {
        while (Vector3.Distance(playerUnit.position, targetPos) > 0.01f)
        {
            playerUnit.position = Vector3.MoveTowards(
                playerUnit.position,
                targetPos,
                moveSpeed * Time.deltaTime
            );
            await UniTask.Yield();
        }

        playerUnit.position = targetPos; // 최종 위치 스냅
    }

    #endregion
}
