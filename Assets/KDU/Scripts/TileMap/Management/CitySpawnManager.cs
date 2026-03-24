using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// 게임 시작 시 도시 영역(BFS 감지) 8개 중 4곳 랜덤 선택, 나머지 city+farmland 제거
[DefaultExecutionOrder(10)]
public class CitySpawnManager : MonoBehaviour
{
    [Header("시야 설정")]
    public int initialVisionRadius = 12; // 20x20 도시 커버 (중심→모서리 ~14칸)

    private TileMapManager tileMapManager;

    // civID 순서대로 선택된 도시 중심 좌표 (0=플레이어, 1~3=AI)
    private List<Vector3Int> spawnedCityCenters = new List<Vector3Int>();
    public List<Vector3Int> SpawnedCityCenters => spawnedCityCenters;

    private void Start()
    {
        tileMapManager = TileMapManager.Instance;
        InitializeCities();
    }

    private void InitializeCities()
    {
        // 1. BFS로 도시 타일을 연결된 영역 단위로 묶음 (20x20 → 1개 영역)
        List<List<Vector3Int>> cityRegions = FindCityRegions();

        if (cityRegions.Count < 4)
        {
            Debug.LogError($"[CitySpawnManager] 도시 영역 {cityRegions.Count}개 감지 — 최소 4개 필요.");
            return;
        }

        // 2. 랜덤으로 4개 인덱스 선택
        List<int> selectedIdx = PickRandomIndices(cityRegions.Count, 4);

        // 3. 미사용 도시 제거 (city 타일 전체 + 주변 farmland)
        for (int i = 0; i < cityRegions.Count; i++)
        {
            if (!selectedIdx.Contains(i))
                RemoveCityRegion(cityRegions[i]);
        }

        // 4. 선택된 도시에 문명 배치
        for (int i = 0; i < selectedIdx.Count; i++)
        {
            List<Vector3Int> region = cityRegions[selectedIdx[i]];
            Vector3Int center = GetCenter(region);
            spawnedCityCenters.Add(center);
            SpawnCivilization(region, center, civID: i);
        }
    }

    // BFS로 연결된 city 타일 그룹 탐지
    private List<List<Vector3Int>> FindCityRegions()
    {
        Tilemap cityTilemap = tileMapManager.cityTilemap;
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
        List<List<Vector3Int>> regions = new List<List<Vector3Int>>();
        Vector3Int[] dirs = { Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down };

        BoundsInt bounds = cityTilemap.cellBounds;
        foreach (Vector3Int startPos in bounds.allPositionsWithin)
        {
            if (!cityTilemap.HasTile(startPos) || visited.Contains(startPos)) continue;

            List<Vector3Int> region = new List<Vector3Int>();
            Queue<Vector3Int> queue = new Queue<Vector3Int>();
            queue.Enqueue(startPos);
            visited.Add(startPos);

            while (queue.Count > 0)
            {
                Vector3Int cur = queue.Dequeue();
                region.Add(cur);

                foreach (Vector3Int dir in dirs)
                {
                    Vector3Int next = cur + dir;
                    if (cityTilemap.HasTile(next) && !visited.Contains(next))
                    {
                        visited.Add(next);
                        queue.Enqueue(next);
                    }
                }
            }

            regions.Add(region);
        }

        return regions;
    }

    // 미사용 도시: city 타일 전체 제거 + 주변 2칸 farmland 제거
    private void RemoveCityRegion(List<Vector3Int> region)
    {
        // city 타일 전체 제거
        foreach (Vector3Int pos in region)
            tileMapManager.EraseTile(tileMapManager.cityTilemap, pos);

        // 도시 영역의 각 타일 기준 2칸 안의 farmland 제거
        HashSet<Vector3Int> toRemove = new HashSet<Vector3Int>();
        foreach (Vector3Int cityPos in region)
        {
            for (int dx = -2; dx <= 2; dx++)
            for (int dy = -2; dy <= 2; dy++)
            {
                Vector3Int pos = cityPos + new Vector3Int(dx, dy, 0);
                if (tileMapManager.farmlandTilemap != null && tileMapManager.farmlandTilemap.HasTile(pos))
                    toRemove.Add(pos);
            }
        }

        foreach (Vector3Int pos in toRemove)
            tileMapManager.EraseTile(tileMapManager.farmlandTilemap, pos);
    }

    // 문명 초기 배치: 도시 영역 전체 영토 점령 + 플레이어 안개 해제
    private void SpawnCivilization(List<Vector3Int> region, Vector3Int center, int civID)
    {
        // 도시 타일 전체를 해당 문명 영토로 등록
        foreach (Vector3Int pos in region)
            tileMapManager.SetOwner(pos, civID);

        // 플레이어(civID 0)만 안개 해제
        if (civID == 0)
            FogManager.Instance?.SetVisible(center, initialVisionRadius);
    }

    // 영역의 평균 중심 좌표
    private Vector3Int GetCenter(List<Vector3Int> region)
    {
        int sumX = 0, sumY = 0;
        foreach (Vector3Int pos in region) { sumX += pos.x; sumY += pos.y; }
        return new Vector3Int(sumX / region.Count, sumY / region.Count, 0);
    }

    // total개 중 count개 랜덤 비복원 추출
    private List<int> PickRandomIndices(int total, int count)
    {
        List<int> pool = new List<int>();
        for (int i = 0; i < total; i++) pool.Add(i);

        List<int> result = new List<int>();
        for (int i = 0; i < count; i++)
        {
            int idx = Random.Range(0, pool.Count);
            result.Add(pool[idx]);
            pool.RemoveAt(idx);
        }
        return result;
    }
}
