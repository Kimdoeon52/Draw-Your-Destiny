using System.Collections.Generic;
using System.IO;
using UnityEngine;
using NYH.CoreCardSystem;

// ============================================================
// SaveManager — JSON 기반 세이브/로드 싱글톤
//
// 공개 메서드 3개는 모두 매개변수 없는 void — Unity Button.OnClick 직접 연결 가능.
//
// 저장 타이밍:
//   - 수동: 저장 버튼 OnClick → OnClickSave()
//   - 자동: GameManager.EndTurn() 끝에서 호출
//
// 전투 중 저장:
//   - BattleSessionController.GetSavedCivilizationState() 로 전투 전 카드 상태 사용
//   - currentNodeID = -1 로 저장 → 로드 시 월드맵에서 시작
// ============================================================
public class SaveManager : Singleton<SaveManager>
{
    private string SaveFilePath => Path.Combine(Application.persistentDataPath, "save.json");

    // 저장 파일 존재 여부 (타이틀 화면 "이어하기" 버튼 활성화 판단용)
    public bool HasSaveFile()
    {
        return File.Exists(SaveFilePath);
    }

    // ── Button.OnClick 연결용 메서드 ──────────────────────────────

    // 현재 게임 상태를 JSON 파일로 저장
    public void OnClickSave()
    {
        SaveData saveData = CollectSaveData();
        string json = JsonUtility.ToJson(saveData, prettyPrint: true);
        File.WriteAllText(SaveFilePath, json);
        Debug.Log("[SaveManager] 저장 완료: " + SaveFilePath);
    }

    // JSON 파일에서 게임 상태 복원
    public void OnClickLoad()
    {
        if (!HasSaveFile())
        {
            Debug.LogWarning("[SaveManager] 저장 파일이 없습니다.");
            return;
        }

        string json = File.ReadAllText(SaveFilePath);
        SaveData saveData = JsonUtility.FromJson<SaveData>(json);
        ApplySaveData(saveData);
        Debug.Log("[SaveManager] 로드 완료");
    }

    // 저장 파일 삭제
    public void OnClickDeleteSave()
    {
        if (!HasSaveFile()) return;
        File.Delete(SaveFilePath);
        Debug.Log("[SaveManager] 저장 파일 삭제 완료");
    }

    // ── 저장 데이터 수집 ───────────────────────────────────────────

    private SaveData CollectSaveData()
    {
        SaveData save = new SaveData();

        // 1. 영지 뷰 진입 중이면 건물 상태를 NodeData에 먼저 동기화
        if (WorldMapManager.Instance != null && WorldMapManager.Instance.IsInTerritoryView)
        {
            int currentID = WorldMapManager.Instance.CurrentNodeID;
            NodeData node = WorldMapManager.Instance.GetNode(currentID);
            if (NodeDataManager.Instance != null && node != null)
                NodeDataManager.Instance.SyncBuildingsToNodeData(node);
        }

        // 2. 전역 상태
        if (GameManager.Instance != null)
        {
            save.currentTurn = GameManager.Instance.currentTurn;
            save.playerEra   = GameManager.Instance.playerEra;
        }
        save.selectedTree = 0; // 미구현 — 기본값

        // 전투 중이면 -1(월드맵)으로 저장
        bool isBattle = IsBattleActive();
        save.currentNodeID = isBattle ? -1
            : (WorldMapManager.Instance != null ? WorldMapManager.Instance.CurrentNodeID : -1);

        // 3. 재화
        if (ResourceManager.Instance != null)
        {
            save.gold          = ResourceManager.Instance.Gold;
            save.research      = ResourceManager.Instance.Research;
            save.food          = ResourceManager.Instance.Food;
            save.population    = ResourceManager.Instance.Population;
            save.maxPopulation = ResourceManager.Instance.MaxPopulation;
        }

        // 4. 노드 데이터
        if (WorldMapManager.Instance != null)
        {
            foreach (NodeData node in WorldMapManager.Instance.AllNodes)
                save.nodes.Add(ConvertToSaveNode(node));
        }

        // 5. 카드 상태 (전투 중이면 전투 전 백업 상태 사용)
        CardPileRuntimeState cardState = GetCivilizationCardState(isBattle);
        save.civilizationCards = ConvertToSaveCardState(cardState);
        // 전투 카드는 현재 미구현 — 빈 상태로 저장
        save.battleCards = new SaveCardState();

        // 6. 유닛 보정치
        if (UnitDatabase.Instance != null)
            save.unitModifiers = UnitDatabase.Instance.ExportAllModifiers();

        return save;
    }

    private bool IsBattleActive()
    {
        NYH.BattleCardSystem.BattleSessionController battleSession =
            FindFirstObjectByType<NYH.BattleCardSystem.BattleSessionController>(FindObjectsInactive.Include);
        return battleSession != null && battleSession.IsBattleActive;
    }

    private CardPileRuntimeState GetCivilizationCardState(bool isBattle)
    {
        if (isBattle)
        {
            NYH.BattleCardSystem.BattleSessionController battleSession =
                FindFirstObjectByType<NYH.BattleCardSystem.BattleSessionController>(FindObjectsInactive.Include);
            if (battleSession != null)
                return battleSession.GetSavedCivilizationState();
        }

        if (CardSystem.Instance != null)
            return CardSystem.Instance.CaptureRuntimeState();

        return null;
    }

    // ── 저장 데이터 적용 ───────────────────────────────────────────

    private void ApplySaveData(SaveData save)
    {
        // 전역 상태 복원
        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentTurn = save.currentTurn;
            GameManager.Instance.playerEra   = save.playerEra;
        }

        // 재화 복원
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.SetAll(save.gold, save.research, save.food, save.population, save.maxPopulation);

        // 노드 데이터 복원
        if (WorldMapManager.Instance != null)
        {
            foreach (SaveNodeData saveNode in save.nodes)
            {
                NodeData node = WorldMapManager.Instance.GetNode(saveNode.nodeID);
                if (node != null)
                    RestoreNodeData(node, saveNode);
            }
        }

        // 카드 상태 복원
        if (CardSystem.Instance != null)
        {
            CardPileRuntimeState cardState = RestoreCardPileState(save.civilizationCards);
            CardSystem.Instance.RestoreRuntimeState(cardState);
        }

        // 유닛 보정치 복원
        if (UnitDatabase.Instance != null)
            UnitDatabase.Instance.ImportAllModifiers(save.unitModifiers);

        // 노드 버튼 갱신
        if (WorldMapManager.Instance != null)
            WorldMapManager.Instance.RefreshAllNodeButtons();

        // 저장 시 영지 뷰에 있었다면 재진입
        if (save.currentNodeID != -1 && WorldMapManager.Instance != null)
            WorldMapManager.Instance.OnNodeClicked(save.currentNodeID);
    }

    // ── 데이터 변환: NodeData → SaveNodeData ────────────────────────

    private SaveNodeData ConvertToSaveNode(NodeData node)
    {
        SaveNodeData save = new SaveNodeData();
        save.nodeID          = node.nodeID;
        save.adjacentNodeIDs = new List<int>(node.adjacentNodeIDs);
        save.ownerCivID      = node.ownerCivID;
        save.isMansionBuilt  = node.isMansionBuilt;
        save.hasPlayerUnits  = node.hasPlayerUnits;
        save.gold            = node.gold;
        save.food            = node.food;
        save.research        = node.research;
        save.farmCount       = node.farmCount;
        save.maxHuman        = node.maxHuman;
        save.playerPopulationCapacity = node.playerPopulationCapacity;
        save.oldUnitCount             = node.oldUnitCount;
        save.units           = new List<NodeUnit>(node.units);
        save.buildings       = new List<SaveBuildingData>();
        foreach (BuildingInstance b in node.buildings)
            save.buildings.Add(ConvertToSaveBuilding(b));
        return save;
    }

    // ── 데이터 변환: BuildingInstance → SaveBuildingData ────────────

    private SaveBuildingData ConvertToSaveBuilding(BuildingInstance b)
    {
        SaveBuildingData save = new SaveBuildingData();
        save.buildingDataId = b.data != null ? b.data.id : "";
        save.originX        = b.origin.x;
        save.originY        = b.origin.y;
        save.originZ        = b.origin.z;
        save.ownerCivID     = b.ownerCivID;
        save.isActive       = b.isActive;
        save.isRuin         = b.isRuin;
        save.populationCapApplied = b.populationCapApplied;
        save.savedState     = new BuildingRuntimeState();
        save.savedState.tick         = b.savedState.tick;
        save.savedState.activeCount  = b.savedState.activeCount;
        save.savedState.waiting      = b.savedState.waiting;
        save.savedState.extraKeys    = new List<string>(b.savedState.extraKeys);
        save.savedState.extraValues  = new List<int>(b.savedState.extraValues);
        return save;
    }

    // ── 데이터 변환: CardPileRuntimeState → SaveCardState ──────────

    private SaveCardState ConvertToSaveCardState(CardPileRuntimeState state)
    {
        SaveCardState save = new SaveCardState();
        if (state == null) return save;
        save.drawPile      = ConvertCardEntries(state.DrawPile);
        save.hand          = ConvertCardEntries(state.Hand);
        save.discardPile   = ConvertCardEntries(state.DiscardPile);
        save.extinctionPile = ConvertCardEntries(state.ExtinctionPile);
        return save;
    }

    private List<SaveCardEntry> ConvertCardEntries(List<CardRuntimeStateEntry> entries)
    {
        List<SaveCardEntry> result = new List<SaveCardEntry>();
        if (entries == null) return result;
        foreach (CardRuntimeStateEntry e in entries)
        {
            if (e == null) continue;
            result.Add(new SaveCardEntry { cardId = e.CardId, currentCost = e.CurrentCost });
        }
        return result;
    }

    // ── 데이터 복원: SaveNodeData → NodeData ────────────────────────

    private void RestoreNodeData(NodeData target, SaveNodeData save)
    {
        target.ownerCivID     = save.ownerCivID;
        target.isMansionBuilt = save.isMansionBuilt;
        target.hasPlayerUnits = save.hasPlayerUnits;
        target.gold           = save.gold;
        target.food           = save.food;
        target.research       = save.research;
        target.farmCount      = save.farmCount;
        target.maxHuman       = save.maxHuman;
        target.playerPopulationCapacity = save.playerPopulationCapacity;
        target.oldUnitCount             = save.oldUnitCount;
        target.units          = new List<NodeUnit>(save.units);
        target.buildings      = new List<BuildingInstance>();
        foreach (SaveBuildingData sb in save.buildings)
            target.buildings.Add(RestoreBuildingInstance(sb));
    }

    // ── 데이터 복원: SaveBuildingData → BuildingInstance ────────────

    private BuildingInstance RestoreBuildingInstance(SaveBuildingData save)
    {
        BuildingData data = BuildingRegistry.Instance != null
            ? BuildingRegistry.Instance.GetByID(save.buildingDataId)
            : null;

        Vector3Int origin = new Vector3Int(save.originX, save.originY, save.originZ);

        BuildingInstance instance = new BuildingInstance();
        instance.data       = data;
        instance.origin     = origin;
        instance.footprint  = data != null ? RecalculateFootprint(origin, data) : new List<Vector3Int>();
        instance.ownerCivID = save.ownerCivID;
        instance.isActive   = save.isActive;
        instance.isRuin     = save.isRuin;
        instance.populationCapApplied = save.populationCapApplied;
        instance.visual     = null;    // 노드 진입 시 재생성
        instance.behaviour  = null;   // 노드 진입 시 재생성
        instance.savedState = new BuildingRuntimeState();
        instance.savedState.tick        = save.savedState.tick;
        instance.savedState.activeCount = save.savedState.activeCount;
        instance.savedState.waiting     = save.savedState.waiting;
        instance.savedState.extraKeys   = new List<string>(save.savedState.extraKeys);
        instance.savedState.extraValues = new List<int>(save.savedState.extraValues);
        return instance;
    }

    // footprint 재계산 (origin + BuildingData width/height)
    private static List<Vector3Int> RecalculateFootprint(Vector3Int origin, BuildingData data)
    {
        List<Vector3Int> footprint = new List<Vector3Int>();
        for (int x = 0; x < data.width; x++)
        for (int y = 0; y < data.height; y++)
            footprint.Add(new Vector3Int(origin.x + x, origin.y + y, origin.z));
        return footprint;
    }

    // ── 데이터 복원: SaveCardState → CardPileRuntimeState ──────────

    private CardPileRuntimeState RestoreCardPileState(SaveCardState save)
    {
        CardPileRuntimeState state = new CardPileRuntimeState();
        state.DrawPile      = RestoreCardEntries(save.drawPile);
        state.Hand          = RestoreCardEntries(save.hand);
        state.DiscardPile   = RestoreCardEntries(save.discardPile);
        state.ExtinctionPile = RestoreCardEntries(save.extinctionPile);
        return state;
    }

    private List<CardRuntimeStateEntry> RestoreCardEntries(List<SaveCardEntry> entries)
    {
        List<CardRuntimeStateEntry> result = new List<CardRuntimeStateEntry>();
        if (entries == null) return result;
        foreach (SaveCardEntry e in entries)
        {
            CardData data = CardCatalog.Instance != null
                ? CardCatalog.Instance.GetByID(e.cardId)
                : null;
            if (data == null) continue;
            result.Add(new CardRuntimeStateEntry
            {
                CardId      = e.cardId,
                CurrentCost = e.currentCost,
                SourceData  = data
            });
        }
        return result;
    }
}
