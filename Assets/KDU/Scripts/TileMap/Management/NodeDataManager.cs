using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// ============================================================
// NodeDataManager — 노드 진입/이탈 저장·복원 싱글톤
//
// 흐름 (진입):
//   1. TileMapManager의 기존 지형·건물 클리어
//   2. 해당 노드의 NodeTerrainSource 타일맵에서 지형 복사
//   3. TileMapManager.ReinitializeTileDataMap() — tileDataMap 재구성
//   4. NodeData.buildings에 저장된 BuildingInstance 시각 복원
//
// 흐름 (이탈):
//   1. TileMapManager.allBuildings → NodeData.buildings에 저장 (visual=null)
//   2. TileMapManager 지형·건물 전체 클리어
//
// Inspector 연결:
//   nodeTerrains 배열에 nodeID 순서대로 NodeTerrainSource를 연결한다.
//   각 NodeTerrainSource는 씬에 비활성화된 채로 상주하는
//   NodePrefab_XX GameObject의 Tilemap 레퍼런스를 가리킨다.
// ============================================================
public class NodeDataManager : Singleton<NodeDataManager>
{
    // ── 노드별 지형 소스 ─────────────────────────────────────��────
    [System.Serializable]
    public class NodeTerrainSource
    {
        public int nodeID;
        [Tooltip("NodePrefab_XX의 Tilemap_City")]
        public Tilemap cityTilemap;
        [Tooltip("NodePrefab_XX의 Tilemap_Farmland")]
        public Tilemap farmlandTilemap;
        [Tooltip("NodePrefab_XX의 CombatArea/Tilemap_Ground")]
        public Tilemap groundTilemap;
        [Tooltip("NodePrefab_XX의 CombatArea/Tilemap_Forest")]
        public Tilemap forestTilemap;
        [Tooltip("NodePrefab_XX의 CombatArea/Tilemap_River")]
        public Tilemap riverTilemap;
    }

    [Header("노드 지형 소스 — nodeID 순서대로 연결")]
    public NodeTerrainSource[] nodeTerrains;

    private TileMapManager tileMapManager;

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        tileMapManager = TileMapManager.Instance;
    }

    // ── 영지 진입 ─────────────────────────────────────────��───────
    public void EnterNode(NodeData nodeData)
    {
        if (tileMapManager == null)
        {
            Debug.LogError("[NodeDataManager] TileMapManager를 찾을 수 없습니다.");
            return;
        }

        // 기존 상태 클리어
        tileMapManager.ClearAllBuildings();
        tileMapManager.ClearAllTerrainTiles();

        // 지형 복사
        NodeTerrainSource src = FindTerrain(nodeData.nodeID);
        if (src != null)
        {
            CopyTilemap(src.cityTilemap,     tileMapManager.cityTilemap);
            CopyTilemap(src.farmlandTilemap, tileMapManager.farmlandTilemap);
            CopyTilemap(src.groundTilemap,   tileMapManager.groundTilemap);
            CopyTilemap(src.forestTilemap,   tileMapManager.forestTilemap);
            CopyTilemap(src.riverTilemap,    tileMapManager.riverTilemap);
        }
        else
        {
            Debug.LogWarning($"[NodeDataManager] nodeID {nodeData.nodeID} 에 대한 지형 소스가 없습니다.");
        }

        // tileDataMap 재초기화
        tileMapManager.ReinitializeTileDataMap();

        // 저장된 ��물 복원
        foreach (BuildingInstance instance in nodeData.buildings)
        {
            if (instance?.data != null)
                tileMapManager.RestoreBuildingInstance(instance);
        }
    }

    // ── 영지 이탈 ─────────────────────────────────────────────────
    public void ExitNode(NodeData nodeData)
    {
        if (tileMapManager == null) return;

        // 현재 배치된 건물들을 NodeData에 저장 (visual만 null로 클리어)
        List<BuildingInstance> current = tileMapManager.GetAllBuildings();
        nodeData.buildings.Clear();

        foreach (BuildingInstance instance in current)
        {
            if (instance == null) continue;
            // Behaviour 상태를 스냅샷으로 저장
            if (instance.behaviour != null)
                instance.savedState = instance.behaviour.SaveState();
            // visual은 ClearAllBuildings에서 Destroy됨 — 저장 전에 null 처리
            instance.visual = null;
            nodeData.buildings.Add(instance);
        }

        tileMapManager.ClearAllBuildings();
        tileMapManager.ClearAllTerrainTiles();
    }

    // 특정 노드의 cityTilemap 반환 (CitySpawnManager에서 bounds 계산용)
    public bool TryGetCityTilemap(int nodeID, out Tilemap tilemap)
    {
        NodeTerrainSource src = FindTerrain(nodeID);
        tilemap = src?.cityTilemap;
        return tilemap != null;
    }

    // ── 지형 소스 조회 ────────────────────────────────────────────
    private NodeTerrainSource FindTerrain(int nodeID)
    {
        if (nodeTerrains == null) return null;
        foreach (NodeTerrainSource src in nodeTerrains)
        {
            if (src.nodeID == nodeID)
                return src;
        }
        return null;
    }

    // ── Tilemap 복사 (source → dest) ──────────────────────────────
    private static void CopyTilemap(Tilemap src, Tilemap dst)
    {
        if (src == null || dst == null) return;

        src.CompressBounds();
        BoundsInt bounds = src.cellBounds;

        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            TileBase tile = src.GetTile(pos);
            if (tile != null)
                dst.SetTile(pos, tile);
        }
    }
}
