using System;
using System.Collections.Generic;

// ============================================================
// SaveData — JSON 저장/로드의 최상위 컨테이너
//
// 게임 전역 상태 + 노드 + 카드 전부를 하나의 객체로 묶는다.
// JsonUtility.ToJson(saveData) 한 번으로 전체 직렬화.
// ============================================================
[Serializable]
public class SaveData
{
    // ── 게임 전역 ─────────────────────────────────────────
    public int currentTurn;
    public Era playerEra;
    public int selectedTree;    // 0=전투, 1=경제 (게임 시작 시 선택)
    public int currentNodeID;   // 저장 시 진입 중이던 노드 (-1이면 월드맵)

    // ── 재화 ──────────────────────────────────────────────
    public int gold;
    public int research;
    public int food;
    public int population;
    public int maxPopulation;

    // ── 노드 (10개) ───────────────────────────────────────
    public List<SaveNodeData> nodes = new List<SaveNodeData>();

    // ── 카드 ──────────────────────────────────────────────
    public SaveCardState civilizationCards = new SaveCardState();
    public SaveCardState battleCards = new SaveCardState();
}
