# 영지 점령 & 잔해 복구 시스템 설계 명세서

## 개요

적 노드를 전투로 점령하면:
1. 군사 건물은 **전부 제거**
2. 경제 건물은 **잔해 상태**로 전환 (기능 정지, 잔해 스프라이트)
3. 해당 노드의 **보유 자원(금/식량/연구)을 전부 획득**
4. 손패에 **"영지 복구 카드"** 자동 지급

이후 복구 카드를 사용하면 잔해 건물이 플레이어 현재 시대에 맞게 일괄 복구.

---

## 1. 군사 / 경제 건물 분류

군사 건물 판별 기준: `BuildingData.unitCapacity > 0`

| 분류 | BuildingType 예시 | 판별 조건 | 점령 시 처리 |
|------|-------------------|-----------|-------------|
| 군사 | Barracks_*, 기마병, 기사, 자이언트 | `unitCapacity > 0` | 완전 제거 |
| 경제 | House, Market, Lab, Farm, Bank | `unitCapacity == 0` | 잔해 전환 |
| 특수 | Mansion | buildingType == Mansion | 완전 제거 (재건 카드로 별도 복구) |
| 특수 | PotionBuilding, TrapWorkshop | `unitCapacity == 0` | 잔해 전환 |

> Mansion은 경제 건물이지만 영지 잠금 해제 조건이므로 점령 시 제거한다.
> 플레이어가 영지 복구 카드로 잔해를 복구한 후, 별도의 영주성 재건 카드를 사용해야 배치 UI가 열린다.
> → **또는** 복구 카드 사용 시 영주성도 자동으로 재건하는 방식 (팀 협의 필요)

---

## 2. 데이터 변경

### 2-1. BuildingData.cs — 잔해 스프라이트 추가

```csharp
[Header("잔해")]
public Sprite ruinSprite;           // 잔해 상태 스프라이트 (null이면 기본 잔해 이미지 사용)
```

### 2-2. BuildingInstance.cs — 잔해 플래그 추가

```csharp
// 점령 시 잔해 상태 — true면 기능 정지, 잔해 스프라이트 표시
public bool isRuined = false;
```

### 2-3. NodeData.cs — 복구 대상 표시

```csharp
// 이 노드에 잔해 건물이 존재하는지 여부
// 점령 시 true, 복구 카드 사용 시 false
public bool hasRuins = false;
```

---

## 3. 점령 처리 흐름

### 호출 시점

전투 시스템에서 공격자 승리 확정 후 호출:

```csharp
WorldMapManager.Instance.ConquerNode(nodeID);
```

### WorldMapManager.ConquerNode() — 신규 메서드

```csharp
// 적 노드 점령 처리
// 전투 승리 후 호출. 건물 분류 → 군사 제거 / 경제 잔해 전환 → 자원 약탈 → 복구 카드 지급
public void ConquerNode(int nodeID)
{
    NodeData node = GetNode(nodeID);
    if (node == null) return;

    int previousOwner = node.ownerCivID;

    // ── 1. 소유권 변경 ──
    node.ownerCivID = 0;
    node.isMansionBuilt = false;
    node.hasPlayerUnits = true;

    // ── 2. 건물 처리 ──
    int ruinCount = 0;
    List<BuildingInstance> surviving = new List<BuildingInstance>();

    foreach (BuildingInstance b in node.buildings)
    {
        if (b?.data == null) continue;

        // 영주성 → 제거
        if (b.data.buildingType == BuildingType.Mansion)
            continue;

        // 군사 건물 (유닛 생산) → 제거
        if (b.data.unitCapacity > 0)
            continue;

        // 경제 건물 → 잔해 전환
        b.isRuined = true;
        b.isActive = false;
        b.ownerCivID = 0;
        ruinCount++;
        surviving.Add(b);
    }

    node.buildings.Clear();
    node.buildings.AddRange(surviving);
    node.hasRuins = (ruinCount > 0);

    // ── 3. 자원 약탈 ──
    // 해당 노드 보유 자원 전량 → 플레이어 전역 자원으로 이동
    if (ResourceManager.Instance != null)
    {
        ResourceManager.Instance.AddGold(node.gold);
        ResourceManager.Instance.AddFood(node.food);
        ResourceManager.Instance.AddResearch(node.research);
    }
    Debug.Log($"[Conquest] 노드 {nodeID} 자원 약탈: 금 {node.gold}, 식량 {node.food}, 연구 {node.research}");
    node.gold = 0;
    node.food = 0;
    node.research = 0;

    // ── 4. 복구 카드 지급 ──
    if (ruinCount > 0)
    {
        int restoreCost = CalculateRestoreCost(surviving);
        // TODO: 카드 시스템에 복구 카드 생성 요청
        // CardManager.Instance.AddRestoreCardToHand(nodeID, restoreCost);
        Debug.Log($"[Conquest] 영지 복구 카드 지급 — 대상 노드 {nodeID}, 잔해 {ruinCount}개, 코스트 {restoreCost}G");
    }

    // ── 5. 후처리 ──
    RebalanceUniqueGlobalBuildings();
    RefreshAllNodeButtons();
}

// 복구 비용 계산 — 잔해 건물의 원래 건설 비용 합산의 50% (팀 협의용 기본값)
private int CalculateRestoreCost(List<BuildingInstance> ruins)
{
    int totalOriginalCost = 0;
    foreach (BuildingInstance b in ruins)
    {
        if (b?.data != null)
            totalOriginalCost += b.data.goldCost;
    }
    return Mathf.CeilToInt(totalOriginalCost * 0.5f);
}
```

---

## 4. 복구 카드 시스템

### 복구 카드 데이터

| 항목 | 값 |
|------|-----|
| 카드 이름 | "영지 복구" |
| 코스트 | 잔해 건물 원래 건설 비용 합산 × 50% (팀 협의 필요) |
| 대상 | 특정 nodeID에만 사용 가능 |
| 사용 조건 | 해당 노드의 영지 뷰 안에서만 사용 가능 |
| 효과 | 해당 노드의 모든 잔해 건물 일괄 복구 |

### 사용 가능 조건 판별

```csharp
// 카드 시스템에서 사용 가능 여부 판별 시
bool CanUseRestoreCard(int targetNodeID)
{
    WorldMapManager wm = WorldMapManager.Instance;
    if (wm == null) return false;

    // 해당 노드 영지 뷰에 진입 중이어야 함
    if (wm.CurrentNodeID != targetNodeID) return false;

    NodeData node = wm.GetNode(targetNodeID);
    if (node == null) return false;

    // 잔해가 있어야 함
    return node.hasRuins;
}
```

### 복구 실행

```csharp
// 카드 사용 시 호출
WorldMapManager.Instance.RestoreRuins(nodeID);
```

### WorldMapManager.RestoreRuins() — 신규 메서드

```csharp
// 잔해 건물 일괄 복구 — 플레이어 현재 시대에 맞게 업그레이드
public void RestoreRuins(int nodeID)
{
    NodeData node = GetNode(nodeID);
    if (node == null) return;

    Era currentEra = GameManager.Instance.CurrentEra; // 현재 시대

    foreach (BuildingInstance b in node.buildings)
    {
        if (b == null || !b.isRuined) continue;

        // 잔해 해제
        b.isRuined = false;
        b.isActive = true;

        // 플레이어 현재 시대에 맞는 BuildingData로 업그레이드
        b.data = GetEraAppropriateData(b.data, currentEra);

        // 상태 초기화
        b.savedState = new BuildingRuntimeState();
    }

    node.hasRuins = false;

    // 현재 영지 뷰 진입 중이면 시각 갱신
    if (currentNodeID == nodeID)
        RefreshTerritoryVisuals(node);

    RebalanceUniqueGlobalBuildings();
    Debug.Log($"[Conquest] 노드 {nodeID} 잔해 복구 완료 — 시대: {currentEra}");
}

// 현재 시대에 맞는 BuildingData 반환 (자동 업그레이드 체인 탐색)
// 예: 석기 Market → 현재 시대가 철기면 체인을 따라가서 철기 Market SO 반환
private BuildingData GetEraAppropriateData(BuildingData data, Era currentEra)
{
    if (data == null) return null;

    BuildingData result = data;

    // 업그레이드 체인의 가장 낮은 단계로 먼저 내려간다 (적이 상위 시대였을 수 있으므로)
    // → 체인 최하단(Stone급)을 찾고, 거기서 플레이어 시대까지 올린다
    BuildingData root = FindChainRoot(data);

    // root부터 플레이어 시대까지 체인을 따라간다
    result = root;
    while (result.isAutoUpgrade && result.upgradesTo != null)
    {
        if ((int)result.upgradesTo.requiredEra <= (int)currentEra)
            result = result.upgradesTo;
        else
            break;
    }

    return result;
}

// 업그레이드 체인의 최하단(root) SO를 찾는다
// 현재 SO들은 체인이 단방향(upgradesTo만 존재)이므로,
// 전체 BuildingData SO를 스캔해서 같은 buildingType이면서 가장 낮은 requiredEra를 가진 것을 찾는다
private BuildingData FindChainRoot(BuildingData data)
{
    // 같은 buildingType의 모든 SO 중 이 체인에 속하는 root를 찾아야 함
    // → 방법: upgradesTo 체인을 역추적할 수 없으므로
    //   data 자체가 이미 requiredEra가 Stone이면 그게 root
    //   아니면 Resources.FindObjectsOfTypeAll 등으로 같은 타입 전체 스캔

    // 간단한 구현: SO 목록을 따로 관리하지 않으므로
    // data.requiredEra == Stone이면 자기 자신이 root
    // 아니면 현재 data를 그대로 반환 (체인 root를 미리 캐싱하는 방식 권장)

    // ※ 아래는 확실한 구현을 위한 Inspector 기반 접근 (권장)
    // buildingChainRoots: Dictionary<BuildingType, BuildingData> 를 WorldMapManager에 캐싱
    // → 게임 시작 시 모든 BuildingData SO를 스캔해서 requiredEra가 가장 낮은 것을 root로 등록

    return data; // 임시 — 체인 root 캐싱 구현 후 교체
}

// 영지 뷰 시각 갱신 (복구 후 스프라이트 교체)
private void RefreshTerritoryVisuals(NodeData node)
{
    // TileMapManager에 현재 건물 시각 전부 갱신 요청
    // → ClearAllBuildings + 건물 재복원 방식이 가장 확실
    TileMapManager tm = TileMapManager.Instance;
    if (tm == null) return;

    tm.ClearAllBuildings();
    foreach (BuildingInstance b in node.buildings)
    {
        if (b?.data != null)
            tm.RestoreBuildingInstance(b);
    }
}
```

---

## 5. 잔해 상태에서의 동작 제한

### BuildingBehaviour — 기존 OnTurnEnd에 잔해 체크 추가

모든 Behaviour의 `OnTurnEnd()`에서 잔해 체크를 하는 대신, 호출하는 쪽(GameManager)에서 필터링하는 것이 효율적:

```csharp
// GameManager.EndTurn() 내 건물 턴 처리 부분
foreach (BuildingInstance b in currentBuildings)
{
    if (b == null || b.isRuined) continue;    // 잔해 건물은 턴 효과 없음
    if (!b.isActive) continue;
    b.behaviour?.OnTurnEnd();
}
```

### WorldMapManager.TickOffscreenBuildings() — 잔해 스킵 추가

```csharp
private static void TickOffscreen(BuildingInstance b)
{
    if (b == null || b.data == null) return;
    if (b.isRuined) return;                    // 잔해는 오프스크린 tick도 안 함
    if (b.data.unitCapacity <= 0) return;
    // ... 기존 로직
}
```

---

## 6. 잔해 시각화

### TileMapManager.RestoreBuildingInstance() 수정

건물 복원 시 `isRuined == true`이면 잔해 스프라이트를 적용:

```csharp
// RestoreBuildingInstance 내부, 스프라이트 적용 부분
SpriteRenderer sr = visual.GetComponent<SpriteRenderer>();
if (sr != null)
{
    if (instance.isRuined && instance.data.ruinSprite != null)
        sr.sprite = instance.data.ruinSprite;
    else
        sr.sprite = instance.data.sprite;
}
```

### 잔해 클릭 방지

잔해 상태 건물은 클릭해도 반응하지 않아야 함:

```csharp
// BankBehaviour.OnMouseDown() 등 클릭 핸들러에 공통 가드 추가
private void OnMouseDown()
{
    if (instance != null && instance.isRuined) return;  // 잔해 상태면 무시
    // ... 기존 로직
}
```

---

## 7. 업그레이드 체인 Root 캐싱 (권장 구현)

체인 root를 매번 검색하지 않으려면 WorldMapManager에 캐싱:

```csharp
[Header("건물 체인 Root — 시대별 자동 업그레이드 최하단 SO")]
[Tooltip("같은 buildingType 체인에서 requiredEra가 가장 낮은 SO를 등록")]
[SerializeField] private BuildingData[] buildingChainRoots;

private Dictionary<BuildingType, BuildingData> chainRootMap;

private void InitChainRootMap()
{
    chainRootMap = new Dictionary<BuildingType, BuildingData>();
    if (buildingChainRoots == null) return;

    foreach (BuildingData data in buildingChainRoots)
    {
        if (data == null) continue;
        // 같은 타입이면 requiredEra가 낮은 것을 우선
        if (!chainRootMap.ContainsKey(data.buildingType) ||
            data.requiredEra < chainRootMap[data.buildingType].requiredEra)
        {
            chainRootMap[data.buildingType] = data;
        }
    }
}
```

이렇게 하면 `FindChainRoot()`가 O(1):

```csharp
private BuildingData FindChainRoot(BuildingData data)
{
    if (chainRootMap != null && chainRootMap.TryGetValue(data.buildingType, out BuildingData root))
        return root;
    return data;
}
```

---

## 8. 전체 흐름 요약

```
전투 승리
 → ConquerNode(nodeID) 호출
    → 군사 건물 제거 / 경제 건물 isRuined=true / 영주성 제거
    → 노드 자원 전량 → ResourceManager 이동
    → 복구 카드를 손패에 추가 (코스트 = 잔해 원가 합산 × 50%)
 → SetPlayerUnitsPresent(nodeID, true)

이후 플레이어 턴 (같은 턴 또는 나중 턴)
 → 해당 노드 영지 진입
 → 영주성 없으므로 배치 UI 잠금
 → 잔해 건물들이 잔해 스프라이트로 표시
 → 복구 카드 사용 (해당 노드 영지 뷰에서만 사용 가능)
    → RestoreRuins(nodeID) 호출
    → 모든 잔해 → 플레이어 시대 맞게 복구 + 스프라이트 정상화
    → hasRuins = false
 → 영주성 재건 카드 사용
    → isMansionBuilt = true → 배치 UI 잠금 해제
 → 이후 건물 배치/생산 정상 운영
```

---

## 9. 코드 변경 목록

| 파일 | 변경 | 내용 |
|------|------|------|
| BuildingData.cs | 필드 추가 | `ruinSprite` |
| BuildingInstance.cs | 필드 추가 | `isRuined` |
| NodeData.cs | 필드 추가 | `hasRuins` |
| WorldMapManager.cs | 메서드 추가 | `ConquerNode()`, `RestoreRuins()`, `GetEraAppropriateData()`, `FindChainRoot()`, `RefreshTerritoryVisuals()`, `CalculateRestoreCost()` |
| WorldMapManager.cs | 필드 추가 (선택) | `buildingChainRoots[]`, `chainRootMap` |
| WorldMapManager.cs | 메서드 수정 | `TickOffscreen()` — isRuined 스킵 |
| GameManager.cs | 수정 | EndTurn 건물 처리에 isRuined 스킵 추가 |
| TileMapManager.cs | 수정 | `RestoreBuildingInstance()` — 잔해 스프라이트 분기 |
| Enums.cs | 변경 없음 | |
| 카드 시스템 | 연동 필요 | 복구 카드 생성/사용 로직 (타 팀원) |

---

## 10. 미확정 / 팀 협의 필요

| 항목 | 선택지 | 비고 |
|------|--------|------|
| 복구 비용 비율 | 50%? 30%? 100%? | 잔해 건물 원가 합산 × n% |
| Mansion 처리 | 복구 카드에 포함? 별도 재건 카드? | 포함이면 복구 카드 하나로 배치 UI까지 열림 |
| 잔해 스프라이트 | 건물별 개별? 공용 1장? | ruinSprite가 null이면 공용 fallback |
| 복구 카드 사용 불가 시 | 덱으로 들어감 (확정) | 해당 노드 영지에서만 사용 가능 제한 |
| 적이 재점령 시 잔해 처리 | 잔해 그대로? 적 시대로 복구? | AI는 자원 수치만 관리하므로 잔해 무시 가능 |
| isUniqueGlobal 잔해 | 잔해 상태에서도 unique 카운트? | false 권장 (잔해는 기능 정지니까 카운트 안 함) |
