using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// ============================================================
// CitySpawnManager — 문명별 시작 도시 좌표/범위 제공
//
// 이전: 씬에 깔린 단일 cityTilemap을 BFS로 스캔해 도시 영역 탐지
// 현재: WorldMapManager.allNodes의 ownerCivID를 기준으로
//       NodeDataManager의 소스 타일맵에서 bounds를 읽어 저장
//
// 외부 사용:
//   TryGetSpawnedCityBounds(civID) — PlayerLordCastle 위치 계산
//   SpawnedCityCenters[civID]      — GetManorOuterTiles 기준점
//
// [DefaultExecutionOrder(10)] — WorldMapManager/NodeDataManager 이후 실행
// ============================================================
[DefaultExecutionOrder(10)]
public class CitySpawnManager : MonoBehaviour
{
    // civID 순서: 0=플레이어, 1~3=AI
    private List<Vector3Int> spawnedCityCenters = new List<Vector3Int>();
    public List<Vector3Int> SpawnedCityCenters => spawnedCityCenters;

    private List<BoundsInt> spawnedCityBounds = new List<BoundsInt>();

    private void Start()
    {
        InitializeCities();
    }

    private void InitializeCities()
    {
        spawnedCityCenters.Clear();
        spawnedCityBounds.Clear();

        WorldMapManager worldMap = WorldMapManager.Instance;
        NodeDataManager nodeDataMgr = NodeDataManager.Instance;

        if (worldMap == null || nodeDataMgr == null)
        {
            Debug.LogError("[CitySpawnManager] WorldMapManager 또는 NodeDataManager를 찾을 수 없습니다.");
            return;
        }

        // civID 0~3 순서대로 소유 노드의 city 범위 수집
        for (int civID = 0; civID < 4; civID++)
        {
            NodeData node = worldMap.GetNodeByCivID(civID);
            if (node == null)
            {
                // 해당 civID 소유 노드 없음 — 빈 값으로 채움
                spawnedCityCenters.Add(Vector3Int.zero);
                spawnedCityBounds.Add(new BoundsInt());
                continue;
            }

            if (nodeDataMgr.TryGetCityTilemap(node.nodeID, out Tilemap cityTilemap))
            {
                cityTilemap.CompressBounds();
                BoundsInt bounds = cityTilemap.cellBounds;
                spawnedCityCenters.Add(GetCenter(bounds));
                spawnedCityBounds.Add(bounds);
            }
            else
            {
                spawnedCityCenters.Add(Vector3Int.zero);
                spawnedCityBounds.Add(new BoundsInt());
                Debug.LogWarning($"[CitySpawnManager] civID {civID} 노드({node.nodeID})의 cityTilemap을 찾을 수 없습니다.");
            }
        }
    }

    public bool TryGetSpawnedCityBounds(int civID, out BoundsInt bounds)
    {
        if (civID < 0 || civID >= spawnedCityBounds.Count)
        {
            bounds = default;
            return false;
        }
        bounds = spawnedCityBounds[civID];
        return true;
    }

    private Vector3Int GetCenter(BoundsInt bounds)
    {
        int centerX = bounds.xMin + (bounds.size.x - 1) / 2;
        int centerY = bounds.yMin + (bounds.size.y - 1) / 2;
        return new Vector3Int(centerX, centerY, 0);
    }
}
