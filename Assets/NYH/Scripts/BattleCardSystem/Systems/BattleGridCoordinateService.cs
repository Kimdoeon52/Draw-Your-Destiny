namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /*
     * BattleGridCoordinateService
     *
     * 역할:
     * - TileMapManager의 타일맵을 전투용 그리드 좌표/월드 좌표 캐시로 변환합니다.
     * - 전투 셀 여부, 타일 타입, 셀 중심 월드 좌표를 빠르게 조회하게 합니다.
     *
     * 담당하지 않는 것:
     * - 유닛 점유 상태는 BattleBoardSystem이 담당합니다.
     * - 이동/공격 가능성 규칙은 query service가 담당합니다.
     */
    public sealed class BattleGridCoordinateService
    {
        private static readonly Vector2Int InvalidCell = new(int.MinValue, int.MinValue);
        private static BattleGridCoordinateService instance;

        private readonly Dictionary<Vector2Int, BattleTileType> tileTypesByCell = new();
        private readonly Dictionary<Vector2Int, Vector3> worldCentersByCell = new();
        private readonly List<Vector2Int> allCombatCells = new();

        private Tilemap cachedGroundTilemap;
        private Tilemap cachedRiverTilemap;
        private Tilemap cachedForestTilemap;
        private bool isCacheValid;
        private bool hasLoggedMissingTilemapWarning;

        public static BattleGridCoordinateService Instance => instance ??= new BattleGridCoordinateService();

        // 현재 캐시된 모든 전투 가능 셀을 반환합니다.
        public IReadOnlyCollection<Vector2Int> GetAllCombatCells()
        {
            EnsureCache();
            return allCombatCells;
        }

        // 타일맵을 다시 읽어 전투 셀/타일 타입/월드 중심 캐시를 갱신합니다.
        public bool RefreshFromTilemaps()
        {
            tileTypesByCell.Clear();
            worldCentersByCell.Clear();
            allCombatCells.Clear();
            isCacheValid = false;

            TileMapManager tileMapManager = TileMapManager.Instance;
            if (tileMapManager == null)
            {
                LogMissingTilemapWarning("TileMapManager가 없어 전투 좌표를 구성할 수 없습니다.");
                return false;
            }

            cachedGroundTilemap = tileMapManager.groundTilemap;
            cachedRiverTilemap = tileMapManager.riverTilemap;
            cachedForestTilemap = tileMapManager.forestTilemap;

            if (cachedGroundTilemap == null && cachedRiverTilemap == null && cachedForestTilemap == null)
            {
                LogMissingTilemapWarning("전투 타일맵(ground/river/forest)이 모두 비어 있습니다.");
                return false;
            }

            // Lowest priority first, then overwrite with higher-priority tile types.
            CacheTilemap(cachedGroundTilemap, BattleTileType.Plain, overwriteExisting: false);
            CacheTilemap(cachedRiverTilemap, BattleTileType.River, overwriteExisting: true);
            CacheTilemap(cachedForestTilemap, BattleTileType.Forest, overwriteExisting: true);

            isCacheValid = allCombatCells.Count > 0;
            if (isCacheValid)
            {
                hasLoggedMissingTilemapWarning = false;
            }
            else
            {
                LogMissingTilemapWarning("전투 타일맵에서 유효한 셀을 찾지 못했습니다.");
            }

            return isCacheValid;
        }

        // 해당 셀이 전투 타일맵에 포함된 유효한 셀인지 확인합니다.
        public bool IsCombatCell(Vector2Int cell)
        {
            EnsureCache();
            return tileTypesByCell.ContainsKey(cell);
        }

        // 전투 셀의 지형 타입을 반환하며, 없으면 Rock으로 취급합니다.
        public BattleTileType GetBattleTileType(Vector2Int cell)
        {
            EnsureCache();
            return tileTypesByCell.TryGetValue(cell, out BattleTileType tileType)
                ? tileType
                : BattleTileType.Rock;
        }

        // 그리드 셀의 월드 중심 좌표를 조회합니다.
        public bool TryGetWorldCenter(Vector2Int cell, out Vector3 world)
        {
            EnsureCache();
            return worldCentersByCell.TryGetValue(cell, out world);
        }

        // 월드 좌표가 포함된 전투 셀을 찾습니다.
        public bool TryGetCell(Vector3 world, out Vector2Int cell)
        {
            EnsureCache();
            if (!isCacheValid)
            {
                cell = InvalidCell;
                return false;
            }

            float bestDistanceSqr = float.MaxValue;
            Vector2Int bestCell = InvalidCell;
            bool found = false;

            EvaluateNeighborCandidates(cachedGroundTilemap, world, ref bestCell, ref bestDistanceSqr, ref found);
            EvaluateNeighborCandidates(cachedRiverTilemap, world, ref bestCell, ref bestDistanceSqr, ref found);
            EvaluateNeighborCandidates(cachedForestTilemap, world, ref bestCell, ref bestDistanceSqr, ref found);

            if (!found)
            {
                for (int i = 0; i < allCombatCells.Count; i++)
                {
                    Vector2Int candidateCell = allCombatCells[i];
                    Vector3 candidateWorld = worldCentersByCell[candidateCell];
                    float distanceSqr = (candidateWorld - world).sqrMagnitude;
                    if (distanceSqr >= bestDistanceSqr)
                    {
                        continue;
                    }

                    bestDistanceSqr = distanceSqr;
                    bestCell = candidateCell;
                    found = true;
                }
            }

            cell = bestCell;
            return found;
        }

        private void EnsureCache()
        {
            if (isCacheValid && AreTilemapReferencesUnchanged())
            {
                return;
            }

            RefreshFromTilemaps();
        }

        private bool AreTilemapReferencesUnchanged()
        {
            TileMapManager tileMapManager = TileMapManager.Instance;
            if (tileMapManager == null)
            {
                return false;
            }

            return cachedGroundTilemap == tileMapManager.groundTilemap
                && cachedRiverTilemap == tileMapManager.riverTilemap
                && cachedForestTilemap == tileMapManager.forestTilemap;
        }

        private void CacheTilemap(Tilemap tilemap, BattleTileType tileType, bool overwriteExisting)
        {
            if (tilemap == null)
            {
                return;
            }

            tilemap.CompressBounds();
            foreach (Vector3Int tileCell in tilemap.cellBounds.allPositionsWithin)
            {
                if (!tilemap.HasTile(tileCell))
                {
                    continue;
                }

                Vector2Int cell = new(tileCell.x, tileCell.y);
                if (!overwriteExisting && tileTypesByCell.ContainsKey(cell))
                {
                    continue;
                }

                if (!tileTypesByCell.ContainsKey(cell))
                {
                    allCombatCells.Add(cell);
                }

                tileTypesByCell[cell] = tileType;
                worldCentersByCell[cell] = tilemap.GetCellCenterWorld(tileCell);
            }
        }

        private void EvaluateNeighborCandidates(
            Tilemap tilemap,
            Vector3 world,
            ref Vector2Int bestCell,
            ref float bestDistanceSqr,
            ref bool found)
        {
            if (tilemap == null)
            {
                return;
            }

            Vector3Int baseCell = tilemap.WorldToCell(world);
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                for (int offsetY = -1; offsetY <= 1; offsetY++)
                {
                    Vector2Int candidateCell = new(baseCell.x + offsetX, baseCell.y + offsetY);
                    if (!worldCentersByCell.TryGetValue(candidateCell, out Vector3 candidateWorld))
                    {
                        continue;
                    }

                    float distanceSqr = (candidateWorld - world).sqrMagnitude;
                    if (distanceSqr >= bestDistanceSqr)
                    {
                        continue;
                    }

                    bestDistanceSqr = distanceSqr;
                    bestCell = candidateCell;
                    found = true;
                }
            }
        }

        private void LogMissingTilemapWarning(string message)
        {
            if (hasLoggedMissingTilemapWarning)
            {
                return;
            }

            hasLoggedMissingTilemapWarning = true;
            Debug.LogWarning($"[BattleGridCoordinateService] {message}");
        }
    }
}
