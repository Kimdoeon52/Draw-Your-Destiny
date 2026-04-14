using System.Collections.Generic;
using UnityEngine;

// ============================================================
// BankBehaviour — 은행 건물 Behaviour
//
// 영지 뷰에서 클릭 시 운송 UI를 열고, 턴당 1회 인접 노드로 자원 전송.
// BuildingBehaviour를 직접 상속 (유닛 생산 없음).
// ============================================================
public class BankBehaviour : BuildingBehaviour
{
    // 이번 턴에 운송을 사용했는지 여부
    private bool usedThisTurn = false;

    public bool UsedThisTurn => usedThisTurn;

    // 배치 시 초기화
    public override void OnPlaced()
    {
        usedThisTurn = false;
    }

    // 턴 종료 시 사용 횟수 리셋
    public override void OnTurnEnd()
    {
        usedThisTurn = false;
    }

    // 운송 실행 — UI에서 호출
    // targetNodeID: 자원을 보낼 인접 노드 ID
    // goldAmount, foodAmount, researchAmount: 각 자원 전송량
    // 반환: 성공 여부
    public bool TransferResources(int targetNodeID, int goldAmount, int foodAmount, int researchAmount)
    {
        if (usedThisTurn) return false;

        WorldMapManager worldMap = WorldMapManager.Instance;
        if (worldMap == null) return false;

        // 현재 노드
        int currentNodeID = worldMap.CurrentNodeID;
        NodeData currentNode = worldMap.GetNode(currentNodeID);
        if (currentNode == null) return false;

        // 대상 노드 검증: 인접 + 플레이어 소유
        NodeData targetNode = worldMap.GetNode(targetNodeID);
        if (targetNode == null) return false;
        if (targetNode.ownerCivID != 0) return false;
        if (!currentNode.adjacentNodeIDs.Contains(targetNodeID)) return false;

        // 전송량 검증: 보유량 초과 불가
        goldAmount     = Mathf.Clamp(goldAmount,     0, currentNode.gold);
        foodAmount     = Mathf.Clamp(foodAmount,     0, currentNode.food);
        researchAmount = Mathf.Clamp(researchAmount, 0, currentNode.research);

        if (goldAmount == 0 && foodAmount == 0 && researchAmount == 0)
            return false;

        // 자원 이동
        currentNode.gold     -= goldAmount;
        currentNode.food     -= foodAmount;
        currentNode.research -= researchAmount;

        targetNode.gold     += goldAmount;
        targetNode.food     += foodAmount;
        targetNode.research += researchAmount;

        usedThisTurn = true;

        Debug.Log($"[Bank] {currentNodeID} → {targetNodeID}: 금 {goldAmount}, 식량 {foodAmount}, 연구 {researchAmount} 운송 완료");
        return true;
    }

    // 인접 플레이어 노드 목록 반환 (UI에서 사용)
    public List<NodeData> GetTransferableNodes()
    {
        List<NodeData> result = new List<NodeData>();

        WorldMapManager worldMap = WorldMapManager.Instance;
        if (worldMap == null) return result;

        NodeData currentNode = worldMap.GetNode(worldMap.CurrentNodeID);
        if (currentNode == null) return result;

        foreach (int adjID in currentNode.adjacentNodeIDs)
        {
            NodeData adj = worldMap.GetNode(adjID);
            if (adj != null && adj.ownerCivID == 0)
                result.Add(adj);
        }

        return result;
    }

    // 노드 이탈 시 상태 저장
    public override BuildingRuntimeState SaveState()
    {
        var state = new BuildingRuntimeState();
        // usedThisTurn은 턴 종료 시 리셋되므로 저장 불필요
        return state;
    }

    public override void LoadState(BuildingRuntimeState state)
    {
        usedThisTurn = false;
    }

    // 은행 클릭 시 운송 UI 열기
    private void OnMouseDown()
    {
        if (usedThisTurn)
        {
            Debug.Log("[Bank] 이번 턴에 이미 운송했습니다.");
            return;
        }

        BankTransferUI ui = BankTransferUI.Instance;
        if (ui != null)
            ui.Open(this);
        else
            Debug.LogWarning("[Bank] BankTransferUI가 씬에 없습니다.");
    }
}
