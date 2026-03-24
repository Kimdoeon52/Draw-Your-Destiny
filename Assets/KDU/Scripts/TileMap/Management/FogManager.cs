using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// 안개 전쟁 관리 싱글톤
// 상태: Hidden(완전 암흑) → Explored(지형+스냅샷) → Visible(완전 공개)
[DefaultExecutionOrder(-10)]
public class FogManager : Singleton<FogManager>
{
    // 지형 타일맵은 TileMapManager에서 참조
    private TileMapManager tileMapManager;

    // 안개 상태 맵
    private Dictionary<Vector3Int, FogState> fogMap = new Dictionary<Vector3Int, FogState>();

    [Header("디버그")]
    public KeyCode debugToggleKey = KeyCode.F1;
    private bool debugFogDisabled = false;

    // 영구 시야 타일 (아웃포스트 건물 주변)
    private HashSet<Vector3Int> outpostVisionTiles = new HashSet<Vector3Int>();

    // 안개 타일 (코드에서 자동 생성)
    private Tile fogTile;

    // 안개 색상
    private static readonly Color FogHidden   = new Color(0f, 0f, 0f, 1f);
    private static readonly Color FogExplored = new Color(0f, 0f, 0f, 0.4f);
    private static readonly Color FogVisible  = new Color(0f, 0f, 0f, 0f);

    // 지형 색상
    private static readonly Color TerrainHidden   = new Color(1f, 1f, 1f, 0f);
    private static readonly Color TerrainExplored = new Color(0.6f, 0.6f, 0.6f, 1f);
    private static readonly Color TerrainVisible  = Color.white;

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        tileMapManager = TileMapManager.Instance;
        if (tileMapManager == null)
        {
            Debug.LogError("[FogManager] TileMapManager를 찾을 수 없습니다.");
            return;
        }

        InitializeFog();
    }

    private void Update()
    {
        if (Input.GetKeyDown(debugToggleKey))
            ToggleFogDebug();
    }

    // F1로 안개 껐다 켰다 (디버그용)
    public void ToggleFogDebug()
    {
        debugFogDisabled = !debugFogDisabled;

        Tilemap fogTilemap = tileMapManager.fogTilemap;

        foreach (Vector3Int pos in fogMap.Keys)
        {
            if (debugFogDisabled)
            {
                // 안개 완전 투명 + 지형 풀 컬러
                if (fogTilemap != null) fogTilemap.SetColor(pos, FogVisible);
                SetTerrainColor(pos, TerrainVisible);
            }
            else
            {
                // 실제 안개 상태로 복원
                UpdateTileVisual(pos);
            }
        }

        Debug.Log($"[FogManager] 디버그 안개 {(debugFogDisabled ? "OFF" : "ON")}");
    }


    // 모든 타일을 Hidden으로 초기화
    private void InitializeFog()
    {
        Tilemap fogTilemap = tileMapManager.fogTilemap;
        if (fogTilemap == null)
        {
            Debug.LogError("[FogManager] fogTilemap이 연결되지 않았습니다.");
            return;
        }

        fogTile = ScriptableObject.CreateInstance<Tile>();
        fogTile.sprite = CreateSolidSprite();
        fogTile.color = Color.white;

        foreach (Vector3Int pos in tileMapManager.GetAllTilePositions())
        {
            fogMap[pos] = FogState.Hidden;

            // 안개 타일 배치
            fogTilemap.SetTile(pos, fogTile);
            fogTilemap.SetTileFlags(pos, TileFlags.None);
            fogTilemap.SetColor(pos, FogHidden);

            // 지형 타일 숨김
            SetTerrainColor(pos, TerrainHidden);
        }
    }

    // ── 공개 API ──────────────────────────────────────────────────

    // 반경 내 타일을 Visible로 전환 (정찰 카드, 아웃포스트 건설 시)
    public void SetVisible(Vector3Int center, int radius)
    {
        ForEachInRadius(center, radius, pos =>
        {
            fogMap[pos] = FogState.Visible;

            BuildingInstance building = tileMapManager.GetBuildingAt(pos);
            if (building != null)
                building.wasEverSeen = true;

            UpdateTileVisual(pos);
        });
    }

    // 반경 내 Visible 타일을 Explored로 되돌림 (정찰 지속 시간 종료 시)
    public void SetExplored(Vector3Int center, int radius)
    {
        ForEachInRadius(center, radius, pos =>
        {
            // 아웃포스트 시야 범위면 Visible 유지
            if (outpostVisionTiles.Contains(pos)) return;

            fogMap[pos] = FogState.Explored;
            UpdateTileVisual(pos);
        });
    }

    // 아웃포스트 건설 시 주변 영구 Visible 등록
    public void OnOutpostBuilt(Vector3Int origin, int buildingSize)
    {
        int margin = 2;
        for (int dx = -margin; dx < buildingSize + margin; dx++)
        for (int dy = -margin; dy < buildingSize + margin; dy++)
        {
            Vector3Int pos = origin + new Vector3Int(dx, dy, 0);
            if (!fogMap.ContainsKey(pos)) continue;

            outpostVisionTiles.Add(pos);
            fogMap[pos] = FogState.Visible;

            BuildingInstance b = tileMapManager.GetBuildingAt(pos);
            if (b != null) b.wasEverSeen = true;

            UpdateTileVisual(pos);
        }
    }

    // 아웃포스트 철거 시 영구 시야 해제
    public void OnOutpostDestroyed(Vector3Int origin, int buildingSize)
    {
        int margin = 2;
        for (int dx = -margin; dx < buildingSize + margin; dx++)
        for (int dy = -margin; dy < buildingSize + margin; dy++)
        {
            Vector3Int pos = origin + new Vector3Int(dx, dy, 0);
            outpostVisionTiles.Remove(pos);

            if (fogMap.ContainsKey(pos))
                fogMap[pos] = FogState.Explored;

            UpdateTileVisual(pos);
        }
    }

    // 건물 배치 시 호출 (TileMapManager에서 호출)
    public void OnBuildingPlaced(BuildingInstance building)
    {
        // 현재 Visible 타일에 배치된 내 건물 → 즉시 표시
        foreach (Vector3Int pos in building.footprint)
        {
            if (fogMap.TryGetValue(pos, out FogState state) && state == FogState.Visible)
                building.wasEverSeen = true;
        }

        RenderBuilding(building);
    }

    // 건물 철거/파괴 시 호출 (TileMapManager에서 호출)
    public void OnBuildingDestroyed(BuildingInstance building)
    {
        // 재탐험 시 빈 땅으로 갱신됨 — visual은 TileMapManager에서 Destroy
    }

    // 단일 타일의 안개 상태 조회
    public FogState GetFogState(Vector3Int pos)
    {
        return fogMap.TryGetValue(pos, out FogState state) ? state : FogState.Hidden;
    }

    // ── 렌더링 ───────────────────────────────────────────────────

    private void UpdateTileVisual(Vector3Int pos)
    {
        FogState state = fogMap.TryGetValue(pos, out FogState s) ? s : FogState.Hidden;

        Tilemap fogTilemap = tileMapManager.fogTilemap;

        // 안개 오버레이
        if (fogTilemap != null)
        {
            Color fogColor = state switch
            {
                FogState.Visible  => FogVisible,
                FogState.Explored => FogExplored,
                _                 => FogHidden
            };
            fogTilemap.SetColor(pos, fogColor);
        }

        // 지형 타일맵 색상
        Color terrainColor = state switch
        {
            FogState.Visible  => TerrainVisible,
            FogState.Explored => TerrainExplored,
            _                 => TerrainHidden
        };
        SetTerrainColor(pos, terrainColor);

        // 건물 렌더링 (스냅샷 방식)
        BuildingInstance building = tileMapManager.GetBuildingAt(pos);
        if (building != null)
            RenderBuilding(building);
    }

    // 건물 시각화 (스냅샷 방식)
    // Visible: 항상 표시
    // Explored + wasEverSeen: 마지막 본 상태 유지
    // Explored + !wasEverSeen: 숨김 (탐험 이후 지어진 적 건물)
    // Hidden: 숨김
    private void RenderBuilding(BuildingInstance building)
    {
        if (building?.visual == null) return;

        SpriteRenderer sr = building.visual.GetComponent<SpriteRenderer>();
        if (sr == null) return;

        // 건물이 차지하는 타일 중 하나라도 Visible이면 표시
        FogState highestState = FogState.Hidden;
        foreach (Vector3Int pos in building.footprint)
        {
            FogState tileState = fogMap.TryGetValue(pos, out FogState s) ? s : FogState.Hidden;
            if (tileState > highestState) highestState = tileState;
        }

        bool show = highestState == FogState.Visible
                 || (highestState == FogState.Explored && building.wasEverSeen);

        sr.color = show ? Color.white : new Color(1f, 1f, 1f, 0f);
    }

    // 지형 타일맵들 색상 일괄 설정
    private void SetTerrainColor(Vector3Int pos, Color color)
    {
        SetTilemapColor(tileMapManager.groundTilemap,    pos, color);
        SetTilemapColor(tileMapManager.farmlandTilemap,  pos, color);
        SetTilemapColor(tileMapManager.riverTilemap,     pos, color);
        SetTilemapColor(tileMapManager.goldMineTilemap,  pos, color);
        SetTilemapColor(tileMapManager.cityTilemap,      pos, color);
    }

    private void SetTilemapColor(Tilemap target, Vector3Int pos, Color color)
    {
        if (target == null || !target.HasTile(pos)) return;
        target.SetTileFlags(pos, TileFlags.None);
        target.SetColor(pos, color);
    }

    // ── 유틸 ─────────────────────────────────────────────────────

    // 원형 반경 순회 (Chebyshev 원)
    private void ForEachInRadius(Vector3Int center, int radius, System.Action<Vector3Int> action)
    {
        for (int dx = -radius; dx <= radius; dx++)
        for (int dy = -radius; dy <= radius; dy++)
        {
            if (dx * dx + dy * dy > radius * radius) continue;

            Vector3Int pos = center + new Vector3Int(dx, dy, 0);
            if (!fogMap.ContainsKey(pos)) continue;

            action(pos);
        }
    }

    // 1x1 흰색 단색 스프라이트 생성 (안개/영토 타일용)
    public static Sprite CreateSolidSprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
    }
}
