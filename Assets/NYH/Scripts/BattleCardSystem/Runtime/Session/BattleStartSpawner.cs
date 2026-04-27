namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    /*
     * BattleStartSpawner
     *
     * 역할:
     * - BattleUnitRoster를 받아 실제로 유닛을 전투 그리드에 소환합니다.
     * - 배치 위치 계산(스폰 영역, 열 수 제한, 빈 셀 탐색)을 캡슐화합니다.
     * - 월드맵 없이 테스트할 수 있도록 인스펙터 fallback을 지원합니다.
     *
     * 사용 시점:
     * - BattleManager.SetupBattle()에서 Setup 페이즈 중 호출됩니다.
     *
     * 의존:
     * - BattleUnitSpawnService (실제 프리팹 생성 + 보드 등록)
     * - BattleBoardSystem (빈 셀 확인)
     */
    public class BattleStartSpawner : MonoBehaviour
    {
        [SerializeField] private BattleUnitSpawnService spawnService;

        [Header("Player Spawn Zone")]
        [Tooltip("플레이어 유닛 스폰 시작 셀 (좌하단)")]
        [SerializeField] private Vector2Int playerSpawnOrigin = new(0, 0);
        [Tooltip("플레이어 스폰 행 방향 (양수 = 위로, 음수 = 아래로)")]
        [SerializeField] private int playerRowDirection = 1;

        [Header("Enemy Spawn Zone")]
        [Tooltip("적 유닛 스폰 시작 셀 (좌하단)")]
        [SerializeField] private Vector2Int enemySpawnOrigin = new(0, 5);
        [Tooltip("적 스폰 행 방향 (양수 = 위로, 음수 = 아래로)")]
        [SerializeField] private int enemyRowDirection = 1;

        [Header("Spawn Layout")]
        [Tooltip("한 행에 배치할 최대 유닛 수")]
        [SerializeField] private int maxColumns = 5;

        [Header("Fallback (인스펙터 테스트용)")]
        [Tooltip("Roster가 없을 때 사용할 플레이어 유닛 프리팹")]
        [SerializeField] private List<BattleUnit> fallbackPlayerUnits = new();
        [Tooltip("fallback 플레이어 유닛 스폰 위치")]
        [SerializeField] private List<Vector2Int> fallbackPlayerPositions = new();
        [Tooltip("Roster가 없을 때 사용할 적 유닛 프리팹")]
        [SerializeField] private List<BattleUnit> fallbackEnemyUnits = new();
        [Tooltip("fallback 적 유닛 스폰 위치")]
        [SerializeField] private List<Vector2Int> fallbackEnemyPositions = new();

        private void Awake()
        {
            if (spawnService == null)
            {
                spawnService = GetComponent<BattleUnitSpawnService>();
            }

            if (spawnService == null)
            {
                spawnService = FindFirstObjectByType<BattleUnitSpawnService>(FindObjectsInactive.Include);
            }
        }

        // Roster 기반 스폰 — 종류별 1기만 소환하고 count를 스탯 배율로 적용합니다.
        // Roster 기반 스폰 — 지정된 타일 리스트가 있으면 우선 사용
        public List<BattleUnit> SpawnFromRoster(BattleUnitRoster roster, List<Vector2Int> designatedTiles = null)
        {
            List<BattleUnit> spawned = new();
            if (roster == null || roster.Entries.Count == 0)
            {
                return spawned;
            }

            if (spawnService == null)
            {
                Debug.LogWarning("[BattleStartSpawner] BattleUnitSpawnService가 없어 스폰할 수 없습니다.");
                return spawned;
            }

            Vector2Int origin = roster.Team == BattleTeam.Player
                ? playerSpawnOrigin
                : enemySpawnOrigin;
            int rowDirection = roster.Team == BattleTeam.Player
                ? playerRowDirection
                : enemyRowDirection;

            // 종류별 1기만 스폰 (count는 스탯에 반영)
            for (int entryIndex = 0; entryIndex < roster.Entries.Count; entryIndex++)
            {
                BattleUnitRosterEntry entry = roster.Entries[entryIndex];
                if (entry == null || entry.Prefab == null || entry.Count <= 0)
                {
                    continue;
                }

                Vector2Int cell;
                if (designatedTiles != null && entryIndex < designatedTiles.Count)
                {
                    cell = designatedTiles[entryIndex];
                }
                else
                {
                    cell = CalculateSpawnCell(origin, entryIndex, rowDirection);
                }
                cell = FindEmptyCell(cell, origin, rowDirection);

                // count를 스탯 배율로 적용하여 1기만 스폰
                BattleUnit unit = spawnService.SpawnUnitWithCount(
                    entry.Prefab, cell, entry.Health, entry.AttackPower, entry.Count);
                if (unit != null)
                {
                    spawned.Add(unit);
                    Debug.Log($"[BattleStartSpawner] 스폰: {entry.UnitType} x{entry.Count} → HP={unit.MaxHealth}, ATK={unit.CurrentAttackPower}");
                }
                else
                {
                    Debug.LogWarning($"[BattleStartSpawner] 스폰 실패: type={entry.UnitType}, cell={cell}");
                }
            }

            Debug.Log($"[BattleStartSpawner] Roster 스폰 완료: team={roster.Team}, 종류={roster.Entries.Count}, 스폰={spawned.Count}");
            return spawned;
        }

        // 인스펙터 fallback 스폰 — Roster 없이 테스트할 때 사용합니다.
        // fallbackPlayerUnits와 fallbackEnemyUnits에 등록된 프리팹을 사용합니다.
        public List<BattleUnit> SpawnFallbackUnits()
        {
            List<BattleUnit> spawned = new();
            if (spawnService == null)
            {
                Debug.LogWarning("[BattleStartSpawner] BattleUnitSpawnService가 없어 fallback 스폰할 수 없습니다.");
                return spawned;
            }

            SpawnFallbackList(fallbackPlayerUnits, fallbackPlayerPositions, playerSpawnOrigin, playerRowDirection, spawned);
            SpawnFallbackList(fallbackEnemyUnits, fallbackEnemyPositions, enemySpawnOrigin, enemyRowDirection, spawned);

            Debug.Log($"[BattleStartSpawner] Fallback 스폰 완료: total={spawned.Count}");
            return spawned;
        }

        // Roster 또는 fallback 중 적절한 쪽으로 스폰합니다.
        // 지정된 타일맵에서 유효한 타일 좌표 목록을 추출합니다.
        private List<Vector2Int> GetSpawnTilesFromTilemap(string tilemapName)
        {
            List<Vector2Int> tiles = new List<Vector2Int>();
            GameObject tmObj = GameObject.Find(tilemapName);
            if (tmObj != null)
            {
                var tm = tmObj.GetComponent<UnityEngine.Tilemaps.Tilemap>();
                if (tm != null)
                {
                    tm.CompressBounds();
                    foreach (var pos in tm.cellBounds.allPositionsWithin)
                    {
                        if (tm.HasTile(pos))
                        {
                            tiles.Add(new Vector2Int(pos.x, pos.y));
                        }
                    }
                }
            }
            return tiles;
        }

        // Roster 또는 fallback 중 적절한 쪽으로 스폰합니다.
        public List<BattleUnit> SpawnAll(BattleUnitRoster playerRoster, BattleUnitRoster enemyRoster)
        {
            List<BattleUnit> spawned = new();

            bool hasRoster = (playerRoster != null && playerRoster.Entries.Count > 0)
                || (enemyRoster != null && enemyRoster.Entries.Count > 0);

            if (hasRoster)
            {
                // KDU 님이 지정한 타일맵에서 좌표 추출
                List<Vector2Int> attackTiles = GetSpawnTilesFromTilemap("Tilemap_AttackSpawn");
                List<Vector2Int> defenceTiles = GetSpawnTilesFromTilemap("Tilemap_DefenceSpawn");

                // 공격자와 수비자 식별
                bool playerIsAttacker = true; // 기본값
                var kduManager = FindFirstObjectByType<WorldMapManager>();
                if (kduManager != null)
                {
                    // lastAttackContext의 AttackerCivID가 0이면 플레이어가 공격자
                    // private 필드이므로 현재 코드 구조상 reflection 또는 문맥으로 파악 가능하지만, 
                    // 지금은 Player가 Attack 모달을 띄웠는지, 아니면 AI 턴인지에 따라 결정됨.
                    // WorldMapManager에 직접 접근하기 까다로우므로 BattleTeam을 확인.
                }

                // BattleSessionController를 우회하지 않고, WorldMap 공격 파견은 항상 플레이어가 선공임
                // AI 침투 방어가 구현되면 해당 스크립트에서 플래그를 넘겨주는 것이 더 깔끔함.
                // 임시: 플레이어가 공격자라고 간주. 나중에 AI 전투가 엮이면 수정 필요.
                List<Vector2Int> playerTiles = playerIsAttacker ? attackTiles : defenceTiles;
                List<Vector2Int> enemyTiles = playerIsAttacker ? defenceTiles : attackTiles;

                if (playerRoster != null)
                {
                    spawned.AddRange(SpawnFromRoster(playerRoster, playerTiles));
                }

                if (enemyRoster != null)
                {
                    spawned.AddRange(SpawnFromRoster(enemyRoster, enemyTiles));
                }
            }
            else
            {
                spawned.AddRange(SpawnFallbackUnits());
            }

            return spawned;
        }

        private void SpawnFallbackList(
            List<BattleUnit> prefabs,
            List<Vector2Int> positions,
            Vector2Int defaultOrigin,
            int rowDirection,
            List<BattleUnit> outSpawned)
        {
            if (prefabs == null)
            {
                return;
            }

            for (int i = 0; i < prefabs.Count; i++)
            {
                if (prefabs[i] == null)
                {
                    continue;
                }

                Vector2Int cell = (positions != null && i < positions.Count)
                    ? positions[i]
                    : CalculateSpawnCell(defaultOrigin, i, rowDirection);
                cell = FindEmptyCell(cell, defaultOrigin, rowDirection);

                BattleUnit unit = spawnService.SpawnUnit(prefabs[i], cell);
                if (unit != null)
                {
                    outSpawned.Add(unit);
                }
            }
        }

        private Vector2Int CalculateSpawnCell(Vector2Int origin, int index, int rowDirection)
        {
            int columns = Mathf.Max(1, maxColumns);
            int col = index % columns;
            int row = index / columns;
            return new Vector2Int(origin.x + col, origin.y + row * rowDirection);
        }

        // 지정 셀이 점유되어 있으면 인접 빈 셀을 탐색합니다.
        // 최대 탐색 반경을 넘으면 원래 셀을 반환합니다.
        private Vector2Int FindEmptyCell(Vector2Int startCell, Vector2Int origin, int rowDirection)
        {
            BattleBoardSystem board = BattleBoardSystem.Instance;
            if (board == null)
            {
                return startCell;
            }

            if (board.GetUnitAt(startCell) == null)
            {
                return startCell;
            }

            int maxSearchRadius = 10;
            for (int radius = 1; radius <= maxSearchRadius; radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius)
                        {
                            continue;
                        }

                        Vector2Int candidate = new Vector2Int(startCell.x + dx, startCell.y + dy);
                        if (board.GetUnitAt(candidate) == null
                            && BattleGridCoordinateService.Instance.IsCombatCell(candidate))
                        {
                            return candidate;
                        }
                    }
                }
            }

            Debug.LogWarning($"[BattleStartSpawner] 빈 셀을 찾지 못했습니다. start={startCell}");
            return startCell;
        }
    }
}
