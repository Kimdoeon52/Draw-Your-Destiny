# AI 건물 시스템 통합 가이드

적 담당 팀원(LSH)을 위한 건물 시스템 연동 가이드.

---

## 왜 통합이 필요한가

플레이어가 적 노드를 점령하면:
1. 그 영지에 진입해서 **건물들을 눈으로 볼 수 있어야** 함
2. 군사 건물은 **제거**, 경제 건물은 **잔해로 전환**해야 함
3. 복구 카드를 쓰면 잔해 건물이 **플레이어 시대에 맞게 복구**됨

이걸 하려면 적이 지은 건물 정보가 `NodeData.buildings`에 `BuildingInstance`로 등록되어 있어야 함.

현재 `EnemyBrainBase`는 자체 프리팹을 `Instantiate`하고 있어서 우리 쪽에서 건물 정보를 읽을 수가 없음.

---

## 핵심 원칙

> **AI는 건물을 "눈에 보이게" 만들 필요 없음. 데이터만 등록하면 됨.**
>
> 플레이어가 그 노드에 진입할 때 `NodeDataManager`가 알아서 시각화함.

---

## 변경 방법

### 1단계: Inspector에 BuildingData SO 연결

현재 `EnemyBrainBase`에 있는 프리팹 필드:

```csharp
// 현재 (변경 전)
[SerializeField] protected GameObject FarmPrefabs;
[SerializeField] protected GameObject MarketPrefabs;
[SerializeField] protected List<GameObject> BarracksPrefabs;
```

이걸 `BuildingData` SO로 바꿈:

```csharp
// 변경 후
[Header("건물 SO — Assets/KDU/Data/Buildings/ 에서 연결")]
[SerializeField] protected BuildingData farmData;          // Building_Farm_Stone
[SerializeField] protected BuildingData marketData;        // Building_Market_Stone
[SerializeField] protected List<BuildingData> barracksData; // 시대별 병영 SO 리스트
```

> BuildingData SO는 도언이 이미 만들어둔 것을 쓰면 됨.
> 아직 안 만든 것은 도언한테 요청.

---

### 2단계: 건물 짓기를 NodeData.buildings에 등록하는 방식으로 변경

현재 건물 짓기 코드:
```csharp
// 현재 (변경 전) — BuildLevelOne 예시
Instantiate(FarmPrefabs, GetRandomPosition(), Quaternion.identity);
```

변경 후:
```csharp
// 변경 후
AddBuildingToNode(nodeID, farmData);
```

`EnemyBrainBase`에 아래 헬퍼 메서드를 추가:

```csharp
// ── AI 건물 등록 ──────────────────────────────────────────
// AI는 타일맵 시각화가 필요 없음 — NodeData에 데이터만 등록.
// 플레이어가 이 노드에 진입하면 NodeDataManager가 알아서 시각화함.
protected bool AddBuildingToNode(int nodeID, BuildingData buildingData)
{
    if (buildingData == null) return false;

    NodeData node = WorldMapManager.Instance.GetNode(nodeID);
    if (node == null) return false;

    // BuildingInstance 생성 (데이터만, visual은 null)
    BuildingInstance instance = new BuildingInstance
    {
        data = buildingData,
        origin = Vector3Int.zero,      // AI는 정확한 타일 위치 불필요
        footprint = new List<Vector3Int>(),
        ownerCivID = node.ownerCivID,
        visual = null                  // 시각화는 진입 시 자동 처리
    };

    node.buildings.Add(instance);

    Debug.Log($"[AI] 노드 {nodeID}에 {buildingData.buildingName} 등록 완료");
    return true;
}
```

> `origin`이 `Vector3Int.zero`인 이유:
> AI 노드에는 플레이어처럼 타일 단위로 위치를 계산할 필요가 없음.
> 나중에 플레이어가 점령해서 진입하면, 잔해 건물은 시각적으로는
> 도시 영역 내 랜덤 배치 또는 고정 위치에 보여주는 별도 처리를 할 예정.

---

### 3단계: BuildLevelOne/Two/Three 수정

`EnemyBrainBase`에서 `this`가 어떤 노드 소속인지 알아야 함.
`enemyID`는 이미 있으니, 그 AI가 소유한 노드 ID를 알 수 있어야 함.

```csharp
// EnemyBrainBase에 필드 추가
[Header("소속 노드 ID")]
[SerializeField] public int ownerNodeID;  // 이 Brain이 관리하는 노드 ID (101~110)
```

그러면 건물 짓기가 이렇게 바뀜:

```csharp
// 변경 전
protected virtual void BuildLevelOne()
{
    int buildingChoice = Random.Range(0, 3);
    switch (buildingChoice)
    {
        case 0:
            Instantiate(FarmPrefabs, GetRandomPosition(), Quaternion.identity);
            break;
        case 1:
            Instantiate(MarketPrefabs, GetRandomPosition(), Quaternion.identity);
            break;
        case 2:
            Instantiate(BarracksPrefabs[0], GetRandomPosition(), Quaternion.identity);
            break;
    }
}

// 변경 후
protected virtual void BuildLevelOne()
{
    int buildingChoice = Random.Range(0, 3);
    switch (buildingChoice)
    {
        case 0:
            AddBuildingToNode(ownerNodeID, farmData);
            break;
        case 1:
            AddBuildingToNode(ownerNodeID, marketData);
            break;
        case 2:
            AddBuildingToNode(ownerNodeID, barracksData[0]);
            break;
    }
}
```

`BuildLevelTwo`, `BuildLevelThree`도 동일한 패턴으로 변경.

---

### 4단계: 자원 관리 연동 (선택)

현재 `EnemyResources`에서 `enemygold`, `enemyfood`를 독자적으로 관리 중.
`NodeData`에 `gold`, `food`, `research` 필드가 추가되었으므로, 두 가지 방법 중 택 1:

#### 방법 A: EnemyResources를 그대로 유지 (당장 쉬운 방법)

점령 시 `EnemyResources`의 자원을 `NodeData`로 복사하는 처리를 도언이 `ConquerNode()`에서 함.

```csharp
// ConquerNode 내부에서
EnemyResources enemyRes = /* 적 팀원이 알려주는 방법으로 접근 */;
if (enemyRes != null)
{
    node.gold = enemyRes.enemygold;
    node.food = enemyRes.enemyfood;
}
```

#### 방법 B: NodeData.gold/food를 직접 사용 (권장, 나중에)

`EnemyResources` 대신 `NodeData`의 자원 필드를 직접 사용하도록 전환.
`faction.AddGold(100)` → `node.gold += 100` 방식.
이러면 점령 시 자원 약탈이 자연스럽게 동작함.

> 당장은 방법 A로 가고, 나중에 여유 있을 때 방법 B로 전환해도 됨.

---

### 5단계: 건물 턴 효과 (생산)

AI 건물의 턴 효과(금 생산, 식량 생산 등)는 두 가지 방법:

#### 지금 방식 유지 (간단)
`EnemyBrainBase.GetGold()`에서 하드코딩으로 자원 추가하는 현재 방식 유지.
건물 개수에 비례해서 자원을 주고 싶으면:

```csharp
protected override void GetGold()
{
    NodeData node = WorldMapManager.Instance.GetNode(ownerNodeID);
    if (node == null) return;

    // 등록된 건물 중 상점 개수만큼 금 생산
    int marketCount = 0;
    foreach (BuildingInstance b in node.buildings)
    {
        if (b.data != null && b.data.buildingType == BuildingType.Market)
            marketCount++;
    }
    faction.AddGold(50 + marketCount * 30);
    faction.AddFood(50);
}
```

---

## 전체 변경 요약

| 파일 | 변경 내용 |
|------|----------|
| EnemyBrainBase.cs | GameObject 프리팹 필드 → BuildingData SO 필드로 교체 |
| EnemyBrainBase.cs | `ownerNodeID` 필드 추가 |
| EnemyBrainBase.cs | `AddBuildingToNode()` 헬퍼 메서드 추가 |
| EnemyBrainBase.cs | `BuildLevelOne/Two/Three()` — Instantiate → AddBuildingToNode 호출로 변경 |
| EnemyBrainBase.cs | `GetRandomPosition()` 삭제 가능 (더 이상 불필요) |
| Inspector | FarmPrefabs/MarketPrefabs/BarracksPrefabs → BuildingData SO 연결 |
| Inspector | ownerNodeID 설정 (각 적 Brain마다 소속 노드 ID) |

---

## 변경 안 해도 되는 것

| 항목 | 이유 |
|------|------|
| EnemyResources | 당장은 그대로 유지 가능 |
| EnemyA/B/C 클래스 구조 | 그대로 EnemyBrainBase 상속 |
| 행동 확률/가중치 시스템 | 건물 시스템과 무관 |
| 유닛 풀/소환 시스템 | 건물 시스템과 무관 |
| 점령 시도 로직 | 그대로 유지, WorldMapManager API 호출만 추가하면 됨 |

---

## 완성 후 동작 흐름

```
AI 턴 → EnemyBrainBase.StartEnemyTurn()
 → CheckWhichBuilding() → BuildLevelOne()
 → AddBuildingToNode(ownerNodeID, farmData)
 → NodeData.buildings에 BuildingInstance 등록 (visual=null)

... 나중에 플레이어가 이 노드를 점령 ...

ConquerNode(nodeID)
 → NodeData.buildings 순회
 → 군사 건물(unitCapacity > 0) 제거
 → 경제 건물 → isRuined = true (잔해)
 → 자원 약탈

플레이어가 영지 진입
 → NodeDataManager.EnterNode() → 잔해 건물 시각화 (잔해 스프라이트)

복구 카드 사용
 → RestoreRuins() → 잔해 해제 + 플레이어 시대로 업그레이드
```

---

## 질문이 있으면

- BuildingData SO가 뭔지, 어디 있는지 → 도언한테
- Inspector 연결 방법 → 도언이 SO 만들어서 전달하면 드래그앤드롭
- 건물 배치 위치(origin)를 의미 있게 쓰고 싶으면 → 나중에 같이 논의
