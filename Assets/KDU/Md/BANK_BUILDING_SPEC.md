# Bank (은행) 건물 설계 명세서

## 개요

은행은 노드 간 자원 운송을 가능하게 하는 기반 건물이다.
은행이 있는 노드에서 인접한 자기 소유 노드로 자원(금, 식량, 연구)을 보낼 수 있다.

---

## 건물 스펙

| 항목 | 값 |
|------|-----|
| BuildingType | `Bank` |
| requiredEra | `Stone` (시대 제한 없음) |
| isAutoUpgrade | `true` (시대별 스프라이트 자동 교체) |
| 크기 (width × height) | 2×2 (팀 협의 필요, 우선 2×2) |
| allowedTiles | `[City]` |
| maxPerTerritory | `1` (영지당 1개) |
| isUniqueGlobal | `false` |
| goldCost | 팀 협의 필요 (우선 50) |
| goldPerTurn / researchPerTurn | 0 (생산 건물 아님) |
| populationCapBonus | 0 |
| unitCapacity | 0 (유닛 생산 안 함) |

---

## 핵심 기능: 자원 운송

### 동작 규칙

| 항목 | 내용 |
|------|------|
| 사용 시점 | 영지 뷰에서 은행 건물 클릭 |
| 사용 횟수 | 턴당 1회 |
| 방향 | 은행이 있는 노드 → 인접 자기 노드 (보내기만) |
| 이동 자원 | 금, 식량, 연구 전부 가능 |
| 이동량 제한 | 없음 (해당 노드 보유량까지) |
| 대상 노드 조건 | 인접 노드 && ownerCivID == 0 (플레이어 소유) |

### UI 흐름

```
영지 뷰에서 은행 건물 클릭
 → 운송 패널 열림
 → 인접 자기 노드 리스트 표시 (이름 + 현재 보유 자원)
 → 노드 선택
 → 자원 종류별(금/식량/연구) 수량 입력 (슬라이더 또는 입력 필드)
 → "전송" 버튼 클릭
 → 현재 노드 자원 차감, 대상 노드 자원 증가
 → usedThisTurn = true → 이번 턴 추가 운송 불가
 → 운송 패널 닫힘
```

---

## 구현 범위

### 1. Enums.cs — BuildingType 추가

```csharp
// 기반 건물 섹션, Farm 아래에 추가
Bank,       // 은행 — 인접 자기 노드로 자원 운송 (턴당 1회)
```

### 2. NodeData — 노드별 자원 필드 추가

```csharp
// NodeData.cs에 추가
public int gold = 0;
public int food = 0;
public int research = 0;
```

> 현재 NodeData에 자원 필드가 없음.
> 은행은 노드 간 자원 이동이므로 NodeData에 자원이 있어야 함.
> 기존 ResourceManager가 전역 자원을 관리하고 있다면, 노드별 자원으로 전환하는 작업이 선행되어야 함.
> **이 명세에서는 NodeData에 자원 필드가 있다고 가정하고 진행한다.**

### 3. BuildingData SO 생성

시대별 3개 SO (스프라이트만 다름, 기능 동일):

| SO 이름 | id | buildingType | requiredEra | isAutoUpgrade | upgradesTo |
|---------|-----|-------------|-------------|---------------|------------|
| Building_Bank_Stone | "bank_stone" | Bank | Stone | true | Building_Bank_Bronze |
| Building_Bank_Bronze | "bank_bronze" | Bank | Bronze | true | Building_Bank_Iron |
| Building_Bank_Iron | "bank_iron" | Bank | Iron | false | null |

### 4. BankBehaviour.cs — 신규 스크립트

파일 위치: `Assets/KDU/Scripts/TileMap/Behaviour/BankBehaviour.cs`

```csharp
// BankBehaviour — 은행 건물 Behaviour
//
// 영지 뷰에서 클릭 시 운송 UI를 열고, 턴당 1회 인접 노드로 자원 전송.
// BuildingBehaviour를 직접 상속 (유닛 생산 없음).
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
        // 향후 추가 상태가 생기면 extraKeys/extraValues 사용
        return state;
    }

    public override void LoadState(BuildingRuntimeState state)
    {
        usedThisTurn = false;
    }
}
```

### 5. 은행 프리팹

파일 위치: `Assets/KDU/Prefab/Building_Bank.prefab`

구성:
- SpriteRenderer (스프라이트는 SO에서 자동 적용)
- BankBehaviour 스크립트 부착
- BoxCollider2D (클릭 감지용)

### 6. 은행 클릭 감지

은행 건물을 클릭했을 때 운송 UI를 여는 처리가 필요하다.

방법 A (권장): 기존 건물 클릭 시스템이 있다면 거기에 Bank 분기 추가
방법 B: BankBehaviour에 OnMouseDown() 추가

```csharp
// 기존 클릭 시스템이 없을 경우 BankBehaviour에 추가
private void OnMouseDown()
{
    if (usedThisTurn)
    {
        Debug.Log("[Bank] 이번 턴에 이미 운송했습니다.");
        return;
    }

    // BankTransferUI.Instance.Open(this);
    // ↑ UI 매니저에서 이 BankBehaviour를 받아서 패널을 열도록 연결
}
```

### 7. BankTransferUI — 운송 UI 패널 (신규)

파일 위치: `Assets/KDU/Scripts/UI/BankTransferUI.cs`

UI 구성:
```
BankTransferPanel (Panel)
├── Title: "자원 운송"
├── NodeListArea
│   └── NodeButton들 (동적 생성)
│       └── 노드 이름 + 보유 자원 표시
├── ResourceInputArea (노드 선택 후 활성화)
│   ├── GoldSlider   + 입력 필드 + "현재 보유: N"
│   ├── FoodSlider   + 입력 필드 + "현재 보유: N"
│   └── ResearchSlider + 입력 필드 + "현재 보유: N"
├── TransferButton: "전송"
└── CloseButton: "닫기"
```

주요 로직:
```csharp
public class BankTransferUI : MonoBehaviour
{
    // 싱글톤 또는 참조로 접근
    public static BankTransferUI Instance;

    // UI 참조
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform nodeListParent;
    [SerializeField] private GameObject nodeButtonPrefab;
    [SerializeField] private Slider goldSlider, foodSlider, researchSlider;
    [SerializeField] private Button transferButton;
    [SerializeField] private Button closeButton;

    private BankBehaviour currentBank;
    private int selectedTargetNodeID = -1;

    // 은행 클릭 시 호출
    public void Open(BankBehaviour bank)
    {
        currentBank = bank;
        selectedTargetNodeID = -1;
        panel.SetActive(true);

        // 인접 플레이어 노드 목록으로 버튼 생성
        List<NodeData> nodes = bank.GetTransferableNodes();
        // ... 노드 버튼 동적 생성
        // 각 버튼 클릭 시 selectedTargetNodeID 설정 + 슬라이더 활성화
    }

    // "전송" 버튼 클릭
    public void OnTransferClicked()
    {
        if (currentBank == null || selectedTargetNodeID < 0) return;

        int gold     = (int)goldSlider.value;
        int food     = (int)foodSlider.value;
        int research = (int)researchSlider.value;

        bool success = currentBank.TransferResources(selectedTargetNodeID, gold, food, research);
        if (success)
        {
            panel.SetActive(false);
            // 자원 UI 갱신
        }
    }
}
```

---

## 의존성 & 선행 조건

| 항목 | 상태 | 비고 |
|------|------|------|
| BuildingType.Bank enum 추가 | 필요 | Enums.cs |
| NodeData 자원 필드 (gold/food/research) | 필요 | 현재 NodeData에 없음 |
| WorldMapManager.CurrentNodeID | 확인 필요 | 현재 진입 중인 노드 ID getter |
| 건물 클릭 감지 시스템 | 확인 필요 | 없으면 OnMouseDown 사용 |
| BuildingData SO 3개 생성 | 에디터 작업 | Stone/Bronze/Iron |
| 은행 프리팹 제작 | 에디터 작업 | SpriteRenderer + BankBehaviour |
| 운송 UI 프리팹 제작 | 에디터 작업 | Canvas 하위 Panel |

---

## WorldMapManager 필요 메서드 확인

BankBehaviour가 사용하는 WorldMapManager 메서드:
- `WorldMapManager.Instance` — 싱글톤 접근
- `WorldMapManager.CurrentNodeID` — 현재 진입 중인 노드 ID (없으면 추가 필요, `currentNodeID` 필드가 private이면 public getter 추가)
- `WorldMapManager.GetNode(int nodeID)` — 이미 존재
- `NodeData.adjacentNodeIDs` — 이미 존재

---

## 점령 시스템 연동 (향후)

- 적 노드 점령 시 은행이 있으면: 군사 건물이 아니므로 **잔해 상태**로 전환 (isRuined=true)
- 복구 카드로 복구 시: 플레이어 시대에 맞는 Bank SO로 교체 + isRuined=false
- AI 노드의 은행: AI는 은행 기능을 사용하지 않음 (AI는 자원을 수치로만 관리)
