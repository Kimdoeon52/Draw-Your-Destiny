# EnemyBrain 리팩터 플랜 (B안 — 문명 브레인 + 턴당 랜덤 노드 1개)

## 배경

현재 `EnemyBrainBase`는 문명 단위 상태(`gold`, `science`, `enemyLevel`)와 영지 단위 상태(`farmCount`, `maxHuman`, 주둔 병력)를 한 클래스에 섞어서 들고 있음. 또한 `currentNodeID`를 고정 필드로 들고 있어서 "1 노드 = 1 브레인" 구조를 전제함.

문제점:
- CLAUDE.md 설계상 금/연구는 **문명(civID) 단위** 재화인데, 노드마다 브레인을 두면 지갑이 소유 노드 수만큼 복제됨
- `enemyLevel`(시대)도 문명 단위
- 점령 시마다 브레인을 활성화해야 하므로 노드 개수만큼 오브젝트 필요

## 결정

- **브레인은 문명당 1개** (AI 3체 = 브레인 3개)
- **턴 시작 시 소유 노드 중 랜덤 1개 선정**해서 그 노드에 대해 행동 (플레이어와 대칭: 플레이어도 한 턴에 영지 1개에서 행동)
- 노드 단위 상태(`farmCount`, `maxHuman`)는 `NodeData`로 이동
- 주둔 병력 수치는 일단 브레인에 유지 (문명 전체 군대 총량으로 취급. 추후 노드별 주둔으로 세분화 가능)

## Awake NullRef 동반 수정

`EnemyBrainManager.Instance`가 `Awake` 순서 문제로 null인 이슈도 같이 고친다. Instance를 lazy-init 프로퍼티로 전환.

---

## 파일별 변경

### 1. `Assets/KDU/Scripts/WorldMap/NodeData.cs`

필드 추가:

```csharp
// AI 영지 상태 (영지 단위로 누적되는 수치)
public int farmCount = 0;
public int maxHuman = 10;
```

### 2. `Assets/KDU/Scripts/WorldMap/WorldMapManager.cs`

기존 `GetNodeByCivID(int civID)`는 **첫 번째 소유 노드만** 반환 → 소유 노드 전부를 반환하는 메서드 추가.

기존 `GetAdjacentNodes` 근처(파일 하단 AI 전용 영역)에 추가:

```csharp
public List<NodeData> GetNodesByCivID(int civID)
{
    List<NodeData> result = new List<NodeData>();
    foreach (NodeData node in allNodes)
        if (node.ownerCivID == civID) result.Add(node);
    return result;
}
```

`using System.Collections.Generic;`는 이미 있음.

### 3. `Assets/LSH/02. Scripts/Enemy/EnemyBrain/EnemyBrainManager.cs`

civID 기반 등록으로 변경 + lazy-init.

```csharp
using System.Collections.Generic;
using UnityEngine;

public class EnemyBrainManager : MonoBehaviour
{
    private static EnemyBrainManager _instance;
    public static EnemyBrainManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<EnemyBrainManager>();
                if (_instance == null)
                {
                    var go = new GameObject("EnemyBrainManager");
                    _instance = go.AddComponent<EnemyBrainManager>();
                }
            }
            return _instance;
        }
    }

    private Dictionary<int, EnemyBrainBase> brainsByCivID = new();

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
    }

    public void Register(int civID, EnemyBrainBase brain)
    {
        if (!brainsByCivID.ContainsKey(civID))
            brainsByCivID.Add(civID, brain);
    }

    public EnemyBrainBase GetBrain(int civID)
    {
        brainsByCivID.TryGetValue(civID, out var brain);
        return brain;
    }
}
```

점령 시마다 브레인을 `SetActive`로 켜는 로직은 **제거**. 브레인은 게임 시작부터 상시 활성.

### 4. `Assets/LSH/02. Scripts/Enemy/EnemyBrain/EnemyBrainBase.cs`

변경 포인트:

**(a) 필드 제거**
- `currentNodeID`
- `farmCount`
- `maxHuman`

(유닛 카운트, `gold`, `science`, `enemyLevel`, `enemyID`는 유지)

**(b) Awake — civID로 등록**

```csharp
protected virtual void Awake()
{
    EnemyBrainManager.Instance.Register(enemyID, this);
}
```

※ 자식 클래스(EnemyA)에서 `enemyID`를 Inspector SerializeField로 지정하거나 Awake 오버라이드에서 `base.Awake()` 전에 설정.

**(c) Setting — maxHuman 초기화 제거** (NodeData에서 기본값 10으로 초기화됨)

```csharp
protected virtual void Setting()
{
    rockWarriorCount = 0;
    archerCount = 0;
    healerCount = 0;
    wizzardCount = 0;
    horseWarriorCount = 0;
    superUnitCount = 0;
}
```

**(d) StartEnemyTurn — 랜덤 소유 노드 1개 선정**

```csharp
protected virtual void StartEnemyTurn()
{
    CheckLevelUp();
    UpdateActionCases();

    List<NodeData> owned = WorldMapManager.Instance.GetNodesByCivID(enemyID);
    if (owned.Count == 0)
    {
        Debug.Log($"<color=gray>[{enemyID}] 소유 노드 없음 — 턴 패스</color>");
        return;
    }

    NodeData target = owned[Random.Range(0, owned.Count)];
    int nodeID = target.nodeID;

    EnemyAction action = GetWeightedRandomAction();
    if (!CheckAction(action))
    {
        Debug.Log("<color=yellow>[돈이 없어서 강제로 골드]</color>");
        action = EnemyAction.GetGold;
    }

    switch (action)
    {
        case EnemyAction.Building:
            CheckWhichBuilding(nodeID);
            break;
        case EnemyAction.GetGold:
            GetGold();
            break;
        case EnemyAction.TryOccupy:
            TryToOccupyAdjacentNode(nodeID);
            break;
    }

    countEnemyTurn++;
    OnTurnPassed?.Invoke();
}
```

**(e) 행동 메서드 시그니처 — nodeID 파라미터 추가**

| Before | After |
|---|---|
| `CheckWhichBuilding()` | `CheckWhichBuilding(int nodeID)` |
| `BuildLevelOne()` | `BuildLevelOne(int nodeID)` |
| `BuildLevelTwo()` | `BuildLevelTwo(int nodeID)` |
| `BuildLevelThree()` | `BuildLevelThree(int nodeID)` |
| `SpawnBuilding(BuildingData)` | `SpawnBuilding(int nodeID, BuildingData)` |
| `FindRandomPlace(BuildingData)` | `FindRandomPlace(int nodeID, BuildingData)` |
| `TryToOccupyAdjacentNode()` | `TryToOccupyAdjacentNode(int nodeID)` |

**(f) farmCount / maxHuman 접근을 NodeData로**

`BuildLevelOne/Two/Three` 내부의 `farmCount` → `node.farmCount`, `maxHuman` → `node.maxHuman`으로 교체. 각 메서드 상단에서 `NodeData node = WorldMapManager.Instance.GetNode(nodeID);` 조회.

예시 (BuildLevelOne 농장 분기):

```csharp
protected virtual void BuildLevelOne(int nodeID)
{
    NodeData node = WorldMapManager.Instance.GetNode(nodeID);
    int buildingChoice = Random.Range(0, 3);
    if (node.maxHuman <= 100)
        buildingChoice = Random.Range(0, 4);

    switch (buildingChoice)
    {
        case 0:
            if (node.farmCount >= 3) { CheckWhichBuilding(nodeID); return; }
            node.farmCount += 1;
            SpawnBuilding(nodeID, farmData);
            break;
        case 1:
            SpawnBuilding(nodeID, marketData);
            break;
        case 2:
            SpawnBuilding(nodeID, barracksData[0]);
            rockWarriorCount += 5;
            break;
        case 3:
            SpawnBuilding(nodeID, houseData);
            node.maxHuman += 10;
            break;
    }
}
```

BuildLevelTwo, BuildLevelThree도 같은 패턴으로.

**(g) SpawnBuilding / FindRandomPlace**

```csharp
protected void SpawnBuilding(int nodeID, BuildingData data)
{
    AddBuildingToNode(nodeID, data);
}

protected virtual Vector3Int FindRandomPlace(int nodeID, BuildingData data)
{
    Tilemap cityTilemap = null;
    if (data.buildingType == BuildingType.Farm)
    {
        if (!NodeDataManager.Instance.TryGetFarmlandTilemap(nodeID, out cityTilemap))
            return Vector3Int.zero;
    }
    else
    {
        if (!NodeDataManager.Instance.TryGetCityTilemap(nodeID, out cityTilemap))
            return Vector3Int.zero;
    }
    // 이하 동일 (단, 내부에서 currentNodeID 쓰던 부분도 nodeID로 교체)
    cityTilemap.CompressBounds();
    BoundsInt bounds = cityTilemap.cellBounds;
    NodeData node = WorldMapManager.Instance.GetNode(nodeID);
    // ... 루프 그대로
}
```

`AddBuildingToNode(nodeID, data)` 내부의 `FindRandomPlace(data)` 호출을 `FindRandomPlace(nodeID, data)`로 교체.

**(h) TryToOccupyAdjacentNode**

```csharp
protected virtual void TryToOccupyAdjacentNode(int nodeID)
{
    List<NodeData> adjacentNodes = WorldMapManager.Instance.GetAdjacentNodes(nodeID);
    foreach (NodeData adjacentNode in adjacentNodes)
    {
        if (adjacentNode.ownerCivID == -1)
        {
            adjacentNode.ownerCivID = enemyID;
            // EnemyBrainManager.Activate 호출 제거 (브레인 상시 활성)
            return;
        }
        else if (adjacentNode.ownerCivID == 0) { /* 전투 진입 */ return; }
        else if (adjacentNode.ownerCivID == enemyID) continue;
        else { /* 다른 적 전투 */ return; }
    }
}
```

### 5. `Assets/LSH/02. Scripts/Enemy/EnemyBrain/Enemy/EnemyA.cs`

`FindRandomPlace` 오버라이드 시그니처 변경:

```csharp
protected override Vector3Int FindRandomPlace(int nodeID, BuildingData data)
{
    return base.FindRandomPlace(nodeID, data);
}
```

`Start()`의 `gameObject.SetActive(false)` 제거 — 브레인은 상시 활성.

`OnEnable()`의 `enemyID = 1; enemyLevel = 1;` 처리는 Awake로 이동 필요 (Register 전에 enemyID 확정):

```csharp
protected override void Awake()
{
    enemyID = 1;
    enemyLevel = 1;
    base.Awake();
}
```

---

## 씬 작업 (도언 파트 영향 없음 — LSH 본인 작업)

- `AITest.unity` 씬의 기존 EnemyA 오브젝트들을 정리
- AI 문명당 1개씩, 총 3개의 브레인 오브젝트 (EnemyA/B/C)만 배치
- 각 오브젝트 Inspector에서 `enemyID`를 1, 2, 3으로 설정
- `EnemyBrainManager` 빈 GameObject를 씬에 배치(또는 lazy-init에 맡김)

## 동작 검증 체크리스트

- [ ] `E` 키로 턴 진행 시 `GetNodesByCivID(enemyID)`가 비어있지 않은 브레인만 행동
- [ ] 소유 노드 2개 이상일 때, 여러 턴 반복하면 두 노드 모두 번갈아 건물 생김
- [ ] 농장 3개 도달 시 더 이상 농장 안 짓음 (노드별 카운트 유지 확인)
- [ ] 점령 시 `adjacentNode.ownerCivID = enemyID`만으로 다음 턴부터 그 노드도 행동 후보로 편입
- [ ] `EnemyBrainManager.Instance.Register` NullRef 더 이상 발생 안 함
