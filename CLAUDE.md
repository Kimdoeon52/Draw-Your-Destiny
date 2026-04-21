# OHP Project — CLAUDE.md

Unity로 개발 중인 턴제 전략 + 덱빌딩 게임 프로젝트입니다.
이 파일은 Claude Code가 프로젝트 구조와 맥락을 이해하기 위한 가이드입니다.

---

## 프로젝트 개요

| 항목 | 내용 |
|------|------|
| 장르 | 턴제 전략 + 덱빌딩 |
| 엔진 | Unity (C#) |
| 맵 방식 | 세계지도형 노드 맵 (삼국지 책략전 방식) + 영지 진입 시 타일맵 뷰 전환 |
| 플레이어 | 1인 vs AI 3체 (1:1:1:1) |
| 턴 단위 | 1턴 단위 (년도 없음, 한 판 40~50턴) |
| 승리 조건 | 모든 상대 문명의 영지(노드) 점령 |
| 팀 규모 | 5인 |

---

## 내 담당 역할 (도언)

**맵 / 건설 파트**

- 세계지도형 노드 맵 UI 구조 구현
- 노드별 영지 프리팹 제작 및 지형 편집
- NodeDataManager (노드 진입/이탈 저장·복원)
- 건물 배치 시스템 (StarCraft 방식 프리뷰) ✅
- 영주성 재건 카드 동작 구현

**기술 배경**

- Unity 플랫포머 타일맵 경험 2회
- A* 알고리즘 학습 완료
- BFS/DFS, Dijkstra 학습 완료
- C/C#/C++ 사용 가능

---

## 핵심 시스템 구조

### 화면 구성

```
게임 전체
├── WorldMapView (UI Canvas 기반) ← SetActive로 전환
│   ├── MapBackground: 손그림 스타일 지도 이미지
│   ├── Nodes/
│   │   ├── NodeButton_101 ~ NodeButton_110  // NodeButton.cs 부착
│   │   └── (노드 연결선 — 추후 추가)
│   └── HUD: 재화, 턴 수, 카드 UI 등
│
├── TerritoryView (Tilemap 기반) ← SetActive로 전환 (기본 비활성)
│   ├── Grid
│   │   ├── ProductionArea (활성)
│   │   │   ├── Tilemap_City        // 도시 타일 (일반 건물 배치)
│   │   │   ├── Tilemap_Farmland    // 농경지 (Farm 전용)
│   │   │   └── Tilemap_Buildings   // 런타임 건물 GO 부모
│   │   └── CombatArea (비활성, 전투 시 전환)
│   │       ├── Tilemap_Ground
│   │       ├── Tilemap_Forest      // 이동 불가
│   │       ├── Tilemap_River       // 이동 2칸 소비
│   │       └── Tilemap_Units
│   └── ExitButton  // WorldMapManager.ExitTerritoryView() 연결
│
├── NodePrefabs/ (모두 비활성 — NodeDataManager 지형 소스용)
│   ├── NodePrefab_01 ~ NodePrefab_10
│   │   ├── ProductionArea
│   │   │   ├── Tilemap_City
│   │   │   └── Tilemap_Farmland
│   │   └── CombatArea
│   │       ├── Tilemap_Ground
│   │       ├── Tilemap_Forest
│   │       ├── Tilemap_River
│   │       └── Tilemap_Units
│   └── ...
│
└── FadeCanvas
    └── FadePanel  // CanvasGroup 컴포넌트 (alpha=0 시작)
```

- 월드맵은 UI Canvas 기반. 타일맵 사용 안함.
- TerritoryView는 NodePrefab 타일을 복사받는 단일 공유 뷰 (풀링).
- 평소에는 ProductionArea 활성화, 전투 선언 시 CombatArea로 전환.
- 안개 전쟁 없음. 모든 노드 항상 공개.
- 미니맵 없음.

### 턴 흐름

```
① 영지 선택 + 카드 드로우 (5장)
② 내 영지 행동 — 건물 건설, 재화 생산 (중립/경제 카드 사용)
③ 적 영지 공격 — 인접 적 노드 클릭 → 노드 진입 후 전투 버튼 → 출발 플레이어 노드 1개 + 파견 병력 수 선택 → 전투 돌입
   └── 승리: 생존 유닛이 해당 노드에 주둔 (hasPlayerUnits=true). 점령은 영주성 재건 후 확정
   └── 패배: 파견한 병력 전부 소모
④ 빈 노드 이동 — 인접 빈 노드 클릭 → 노드 진입 후 이동 버튼 → 출발 플레이어 노드 1개 + 파견 병력 수 선택 → 유닛 주둔
   └── 이후 영주성 재건 카드 사용 시 ownerCivID=0 전환 → 점령 완료
⑤ 카드 3택 — 문명카드+전투카드 세트 3개 제시, 1세트 선택 또는 패스
⑥ ①~⑤ 반복

자세한 이동/전투 시스템 설계: `Assets/KDU/Scripts/Md/UNIT_MOVEMENT_COMBAT_SPEC.md`
```

### 재화 (3종 확정)

| 재화 | 역할 | 획득 | 주요 용도 |
|------|------|------|------|
| 금 | 범용 기본 재화 | 상점 | 문명/중립 카드 코스트, 건물 건설, 노드 점령 비용 |
| 식량 | 전투 유지 재화 | 농장 | 전투 카드 코스트. 전투 중 매 턴 유닛 수 × n 소모. 고갈 시 전투 패배 |
| 연구 | 시대 전환 재화 | 연구소 | 시대 전환 조건 (청동기 100, 철기 200). 시대 전환 후 리셋 |

※ 인구는 재화 아님. 유닛 생산 최대 한도 개념 (민가 건물로 한도 증가).

### 트리 (2종 + 중립)

| 트리 | 특성 |
|------|------|
| 전투 | 전투에 유리한 카드 중심. 코스트: 식량 |
| 경제 | 재화 생산 및 시대 발전에 유리한 카드 중심. 코스트: 금 |
| 중립 | 트리 무관. 건물 관련. 모든 문명 획득 가능. 코스트: 금 |

게임 시작 시 전투 또는 경제 트리 중 하나 선택. 선택한 트리에 맞는 기본팩 지급.

### 시대 진행

| 시대 | 전환 조건 | 참고 예상 구간 |
|------|------|------|
| 석기시대 | 시작 | 1~15턴 |
| 청동기시대 | 연구 포인트 100 달성 | ~30턴 |
| 철기시대 | 연구 포인트 200 달성 | 31턴~ |

---

## 파일 구조

```
Assets/KDU/Scripts/
├── GameManager.cs
├── TotalStaticClass.cs
├── WorldMap/
│   ├── NodeData.cs                 // 노드 런타임 상태
│   ├── WorldMapManager.cs          // 노드 맵 전체 관리 싱글톤
│   └── NodeButton.cs               // 노드 UI 버튼 동작
├── TileMap/
│   ├── Data/
│   │   ├── Enums.cs                // TileType, BuildingType, Era
│   │   ├── BuildingData.cs         // 건물 ScriptableObject
│   │   └── BuildingInstance.cs     // 배치된 건물 인스턴스
│   ├── Management/
│   │   ├── TileMapManager.cs       // 타일맵 관리 싱글톤
│   │   ├── NodeDataManager.cs      // 노드 진입/이탈 저장·복원
│   │   ├── CitySpawnManager.cs     // 문명별 시작 도시 좌표/범위 제공
│   │   ├── BuildingPlacementService.cs
│   │   └── BuildingPlacementController.cs
│   └── Visualization/
│       └── BuildingPreview.cs      // 프리뷰 렌더링
├── Util/
│   └── Singleton.cs
└── Md/
    └── BUILDING_DATA_SPEC.md
```

---

## 노드 시스템

### 노드 구성

- 총 10개, nodeID = 101 ~ 110
- NodePrefab_01 → nodeID 101, NodePrefab_02 → nodeID 102, ... NodePrefab_10 → nodeID 110

### NodeData

```csharp
public class NodeData
{
    public int nodeID;
    public List<int> adjacentNodeIDs;   // 인접 노드 ID (공격/점령 가능 여부 판단)
    public int ownerCivID;              // -1 = 빈 노드, 0~3 = 문명 ID
    public bool isMansionBuilt;         // 영주성 재건 여부 (false면 배치 UI 잠금)
    public bool hasPlayerUnits;         // 플레이어 유닛 주둔 여부 (ownerCivID 무관하게 진입 허용)
    public List<BuildingInstance> buildings;
}
```

civID: 0=플레이어(파랑), 1=AI1(빨강), 2=AI2(초록), 3=AI3(노랑)

### 노드 종류

| 종류 | 설명 |
|------|------|
| 아군 노드 | ownerCivID==0 && isMansionBuilt==true. 영지 진입 및 건물 배치 가능. 타 노드로의 병력 파견 기점(출발지) |
| 유닛 주둔 노드 | hasPlayerUnits==true. ownerCivID 무관하게 진입 가능. 배치는 isMansionBuilt 여부에 따름. 영주성 재건 전까지는 이 노드에서 다른 노드로 병력 파견 불가 |
| 적 노드 | AI 소유. 인접 아군 노드에서 전투 버튼으로 공격 가능 |
| 빈 노드 | 미점령. 인접 아군 노드에서 이동 버튼으로 유닛 파견 → 영주성 재건 시 점령 완료 |

### 노드 진입 조건

```
ownerCivID == 0  →  영지 진입 가능
hasPlayerUnits == true  →  영지 진입 가능 (적/빈 노드여도)
그 외  →  진입 불가
```

### 노드 진입/이탈 흐름

```
월드맵에서 진입 가능 노드 클릭
 → 페이드 아웃
 → TerritoryView 활성, WorldMapView 비활성
 → TileMapManager 클리어 → NodePrefab_XX에서 cityTilemap/farmlandTilemap 복사
 → NodeData.buildings 복원 (RestoreBuildingInstance)
 → isMansionBuilt == false → 배치 UI 전체 잠금
 → 페이드 인

영지 뷰에서 ExitButton 클릭
 → NodeData에 현재 buildings 저장
 → TileMapManager 클리어
 → TerritoryView 비활성, WorldMapView 활성
 → 페이드 인
```

### 영주성 재건 카드 흐름

```
빈 노드 유닛 파견 또는 전투 승리 후
 → hasPlayerUnits = true (ownerCivID는 아직 변경되지 않음)
 → 노드 진입 가능, 배치 UI 잠금 상태 (이 노드를 기점으로 다른 노드 파견 불가)

영지 뷰 진입 후 영주성 재건 카드 사용
 → WorldMapManager.OnMansionRebuilt() 호출
 → isMansionBuilt = true
 → ownerCivID = 0 (점령 완료)
 → 배치 UI 잠금 해제
 → 이후 건물/농장 배치 가능 + 이 노드를 기점으로 다른 노드 파견 가능
```

### WorldMapManager 주요 메서드

```csharp
OnNodeClicked(int nodeID)               // 노드 버튼 클릭 진입점
ExitTerritoryView()                     // ExitButton에서 호출
OnMansionRebuilt()                      // 영주성 재건 카드에서 호출
SetNodeOwner(int nodeID, int civID)     // 전투 결과 등에서 소유권 변경
SetPlayerUnitsPresent(int nodeID, bool) // 전투 승리/철수 시 유닛 주둔 상태 변경
GetNode(int nodeID)                     // nodeID로 NodeData 조회
GetNodeByCivID(int civID)               // civID로 첫 번째 소유 NodeData 조회
```

### NodeDataManager 역할

- Inspector: `nodeTerrains` 배열에 nodeID + NodePrefab_XX의 cityTilemap/farmlandTilemap 연결
- 노드 진입 시: TileMapManager 클리어 → 지형 복사 → tileDataMap 재초기화 → buildings 복원
- 노드 이탈 시: buildings → NodeData 저장 → TileMapManager 클리어
- Tilemap은 View 전용. 상태 변경은 반드시 NodeDataManager를 통해서만.

### CitySpawnManager 역할

- 게임 시작 시 WorldMapManager.allNodes에서 civID 0~3 소유 노드를 찾아 city bounds 계산
- 직접 cityTilemap을 스캔하지 않음 — NodeDataManager 소스 타일맵에서 bounds 읽음
- `TryGetSpawnedCityBounds(civID)` — PlayerLordCastle 위치 계산에 사용
- `SpawnedCityCenters[civID]` — GetManorOuterTiles 기준점에 사용

---

## 영지 프리팹 구조

노드마다 프리팹 1개. 지형은 Unity 에디터에서 직접 타일 찍어 고정.
NodePrefab_XX는 항상 비활성 상태 유지 — NodeDataManager가 지형 소스로만 참조.

```
NodePrefab_XX
├── ProductionArea
│   ├── Tilemap_City        // 도시 타일 (일반 건물 배치 가능)
│   └── Tilemap_Farmland    // 농경지 (Farm 전용)
└── CombatArea              // 전투 시스템 구현 시 사용 예정
    ├── Tilemap_Ground
    ├── Tilemap_Forest      // 이동 불가
    ├── Tilemap_River       // 이동 2칸 소비
    └── Tilemap_Units       // 런타임 유닛 배치 레이어
```

※ Tilemap_Buildings는 TileMapManager가 런타임에 GO로 생성하므로 NodePrefab에 불필요.

### 타일 종류 (TileType)

| 타입 | 건설 가능 | 특성 |
|------|------|------|
| Farmland | △ | Farm(농장) 전용 |
| City | O | 일반 건물 배치 가능 |
| River | X | 타일 없는 위치의 기본값. 이동/건설 불가 |
| Forest | X | 전투 공간 전용. 이동 불가 |

※ Plain / Resource 타입은 현재 미사용 (Ground/Gold 타일맵 제거됨)

### TileMapManager Inspector 연결 필드

```
cityTilemap     → TerritoryView/Grid/ProductionArea/Tilemap_City
farmlandTilemap → TerritoryView/Grid/ProductionArea/Tilemap_Farmland
```

---

## 건물 시스템

### BuildingType

```csharp
// 기반 건물
Mansion,  // 영주성 — 영주성 재건 카드로만 설치. 영지 잠금 해제 조건
House,    // 민가 — 인구 한도 증가
Market,   // 상점 — 매 턴 금 획득
Lab,      // 연구소 — 매 턴 연구 포인트 획득. 게임당 1개 제한 (isUniqueGlobal=true)
          //         3단계 자동 업그레이드 체인: Lab(석기)→LabBronze(청동기)→LabIron(철기)
          //         buildingType은 세 단계 모두 Lab 유지 (LabBehaviour 공유, researchPerTurn만 다름)
Farm,     // 농장 — 매 턴 식량 획득. 영지당 최대 2개 제한 (3×3 크기)
Bank,     // 은행 — 인접 자기 노드로 자원 운송 (턴당 1회)

// 군사 건물 — 4단계 체계 (하급/중급/상급/최상급)
// 수치:
//   하급       3턴/1명 생산, unitCapacity=5
//   중급/상급  3턴/2명 생산 (두 종류 유닛 1명씩 동시), unitCapacity=10
//   최상급    3턴/1명 생산, unitCapacity=5

// 하급 병영 — 근접(RockWarrior) 전용. 석기~철기 자동 업그레이드
Barracks_SoldierStone → Barracks_SoldierBronze → Barracks_SoldierIron

// 중급 병영 — 힐러(Healer) + 궁수(Archer) 동시 생산. 청동기~철기 자동 업그레이드
Barracks_ArcheryRange_Medic → Barracks_ArcheryRange_Medic_Elite

// 상급 병영 — 기사(Knight) + 기마병(HorseWarrior) 동시 생산. 철기 전용 (단일 단계)
Barracks_Stable_Knight

// 최상급 병영 — 마법사(Wizard) 전용. 철기 + 연구포인트 200 달성 시 생산 시작
Barracks_Wizard

// 지원 건물
PotionBuilding                                   // 포션 가게 (청동기~) — 매 턴 랜덤 포션 카드 추가
TrapWorkshop                                     // 덫 공방   (청동기~) — 매 턴 랜덤 덫 카드 추가
```

※ 중급/상급 병영의 "3턴/2명"은 한 사이클에 두 종류 유닛 1명씩 동시 생산 (중급=힐러1+궁수1, 상급=기사1+기마병1).

### 건물 제한

| 건물 | 제한 |
|------|------|
| 농장 (Farm) | 영지당 최대 2개 (maxPerTerritory=2) |
| 연구소 (Lab) | 게임 전체에서 1개 (isUniqueGlobal=true, 3단계 자동 업그레이드 체인) |
| 영주성 (Mansion) | 영지당 1개. 영주성 재건 카드로만 설치 (maxPerTerritory=1) |

### 자동 업그레이드 체인

시대 전환 시 자동 업그레이드. 최초 설치 비용만 지불, 업그레이드 무료.

```csharp
BuildingData.isAutoUpgrade = true
BuildingData.upgradesTo = 다음 단계 BuildingData SO 참조  // null = 최종
```

### BuildingData 주요 필드

```csharp
string id;                  // 직렬화 키 (BuildingInstance 저장/복원용)
Era requiredEra;            // 설치 가능 최소 시대
bool isAutoUpgrade;
BuildingData upgradesTo;
int goldCost;
GameObject visualPrefab;    // 건물 시각 프리팹 (BuildingBehaviour 부착). null이면 빈 GO fallback
int productionInterval;     // 몇 턴마다 유닛 1명 생산 (기본 3)
int unitCapacity;           // 최대 수용 유닛 수. 0이면 생산 건물 아님. 군사 건물 포함 모든 생산 건물에 사용
int goldPerTurn;
int researchPerTurn;
int populationCapBonus;     // 민가 전용
int maxPerTerritory;        // 영지당 최대 설치 수 (-1 = 무제한). Farm: 2 / Mansion: 1
bool isUniqueGlobal;        // true면 게임 전체에 1개만 허용 (Lab 전용)
```

### 건물 기준점 계산 (StarCraft 방식)

```
1×1: 클릭 지점이 건물 중심
2×2: 클릭 지점이 좌하단 모서리
3×3: 클릭 지점이 정중앙
4×4: 2×2와 동일하게 좌하단 모서리
```

### 배치 가능 조건

1. tileDataMap에 등록된 위치 (City 또는 Farmland 타일)
2. 해당 위치에 건물 없음
3. BuildingData.allowedTiles에 타일 타입 포함
   - 일반 건물: allowedTiles = [City]
   - Farm: allowedTiles = [Farmland]
   - Mansion: allowedTiles = [City] (중앙 고정 배치)
4. isMansionBuilt == true (Mansion 자신 제외)
5. maxPerTerritory 초과 여부 확인 (영지 내 buildingType 카운트)
6. isUniqueGlobal == true이면 전체 노드 스캔 후 동일 buildingType 없어야 배치 가능

※ River 타일 체크 별도 불필요 — River/Ground 타일맵이 없으므로 IsValidPosition으로 통합 처리

### 농장(Farm) 스프라이트 오토타일링

Farm 건물 배치/제거 시 자신과 인접 Farm의 스프라이트를 자동 갱신.
8방향 연결 여부에 따라 18종 스프라이트 중 자동 선택.

```
TileMapManager.farmSprites[18] — Inspector에서 스프라이트 연결
  0~2:  좌끝/우끝/가로중앙 (수평 줄)
  3~5:  상끝/하끝/세로중앙 (수직 줄)
  6~8:  우하 코너 (외부/내부 대각/T자)
  9~11: 좌하 코너 동일
  12~14: 우상 코너 동일
  15~17: 좌상 코너 동일
```

### 건물 배치 흐름

```
카드 선택
 → BuildingPlacementService.StartPlacing()
 → 마우스 따라다니는 프리뷰 (초록/빨강)
 → 좌클릭: 배치 시도
 → TileMapManager.PlaceBuilding()
 → BuildingInstance 생성 + GameObject 생성
 → NodeData.buildings에 추가 (노드 이탈 시 저장됨)
 → 재화 차감 및 효과 적용
```

---

## 전투 시스템

### 전투 흐름

```
월드맵에서 인접 적 노드 공격 선언
 → 영지 뷰 진입 (CombatArea 활성화)
 → 전투 진행 (카드 기반 독립 턴제)
 → 공격자 승리 (방어 유닛 전멸): SetNodeOwner() + SetPlayerUnitsPresent() 호출 → 월드맵 복귀
 → 방어자 승리 (공격 유닛 전멸 또는 식량 고갈): 공격 유닛 전부 소멸 → 월드맵 복귀
```

### 전투 지형 효과

| 지형 | 효과 |
|------|------|
| 평지 | 이동 1칸 소비 |
| 숲 / 바위 | 이동 불가 |
| 강 | 이동 2칸 소비 |

### 유닛 종류 (6종 + 경제 유닛)

| 유닛 | UnitType | 등장 시대 | 생산 건물 | 특성 |
|------|----------|-----------|-----------|------|
| 돌도끼병 | RockWarrior | 석기 | 하급 병영 (SoldierStone/Bronze/Iron) | 기본 근접 유닛 |
| 의무병 | Healer | 청동기 | 중급 병영 (ArcheryRange_Medic/_Elite) | 힐러. 아군 체력 회복 |
| 궁수 | Archer | 청동기 | 중급 병영 (ArcheryRange_Medic/_Elite) | 원거리 공격 유닛 |
| 기마병 | HorseWarrior | 철기 | 상급 병영 (Stable_Knight) | 고속 이동. 말 체력 소진 시 근접 전환 |
| 풀플레이트 아머 기사 | Knight | 철기 | 상급 병영 (Stable_Knight) | 고방어 근접 유닛 |
| 마법사 | Wizard | 철기 | 최상급 병영 (Wizard) | 연구포인트 200 도달 시 생산 시작. 3턴/1명, cap 5 |
| 농부 | Farmer | — | Farm 등 | 경제 유닛 |
| 상인 | Shoper | — | Market/Bank 등 | 경제 유닛 |

※ 성별 개념 없음.

### 부대 시스템

- 같은 종류의 유닛은 하나의 부대로 묶여 함께 행동
- 체력/공격력은 부대 인원 수에 비례 (예: 돌도끼병 기본 100 × 5명 = 부대 체력 500)
- 드로우 수 = 보유 유닛 종류 수 + 1

### 식량 소모 (전투 중)

- 매 턴: 유닛 수 × n만큼 식량 소모
- 식량 고갈 시: 전투 패배

---

## 인구 시스템 (미구현)

유닛 생산 최대 한도 개념. 재화 아님.

| 항목 | 내용 |
|------|------|
| 기본 인구 한도 | 10명 |
| 한도 증가 | 민가 건물 건설 |
| 민가 1채당 증가 | +5명 / +7명 / +10명 (시대별 자동 업그레이드) |
| 최대 인구 한도 | 150명 |

### 인구 성장 사이클

| 이벤트 | 기준 |
|------|------|
| 아이 출생 | 건물 건설 시점 (개별 객체 생성) |
| 성인 전환 | 출생 후 3턴 (해당 건물/슬롯에 자동 배치) |
| 노인 전환 | 출생 후 15턴 (생산 활동 없음) |

### 역할 (UnitRole)

```csharp
Idle,     // 미배치
Farmer,   // 농민
Laborer,  // 노역자
Soldier,  // 병사
```

---

## AI 시스템 (미구현)

규칙 기반(Rule-based). 기본 AI 1체 + 가중치 차등으로 3체 운용.

### AI 행동 우선순위

| 우선순위 | 조건 | 행동 |
|------|------|------|
| 1 | 재화 임계값 이상 | 트리 방향에 맞는 카드 사용 및 건물 건설 |
| 2 | 인접 노드에 적 존재 | 해당 적 노드 공격 선언 |
| 3 | 공격받음 | 방어 유닛 배치로 방어 강화 |
| 4 | 연구포인트 임박 | 연구 관련 카드 우선 사용 |
| 5 | 기본 | 재화 생산 카드 사용, 인접 빈 노드 점령 |

AI끼리의 전투 승패는 직접 시뮬레이션 없이 재화 수치, 유닛 수, 건물 수 가중치로 판단.

---

## 유닛 데이터 구조 (미구현)

```csharp
[CreateAssetMenu]
public class UnitData : ScriptableObject
{
    public string unitName;
    public int baseAttack;
    public int baseDefense;
    public int maxHP;
    public int cost;
    public Era requiredEra;
}

public class UnitInstance
{
    public UnitData data;
    public int currentHP;
    public int ownerCivID;
    public bool isDeployed;
}
```

---

## 이벤트 시스템 (미구현)

```csharp
[CreateAssetMenu]
public class EventData : ScriptableObject
{
    public string title;
    public string description;
    public EventChoice[] choices;
}
```

**발생 조건**: 5턴마다 1회 보장 + 매 턴 15% 확률 추가 발생

---

## 코드 작성 규칙

- 언어: C# (Unity 6)
- 변하지 않는 데이터는 ScriptableObject로 분리
- 런타임 상태는 Class (참조 타입) 사용
- Tilemap은 View 전용. 상태 변경은 NodeDataManager를 통해서만
- AI 영지는 타일맵 없이 수치로만 관리
- 주석은 간결하게 (팀원이 이해할 수 있을 정도만)
- 네이밍은 영어 카멜케이스 / 파스칼케이스 유지
- namespace 사용 안함 (팀 프로젝트)
- Unity 6 API 사용 (FindFirstObjectByType 등)

---

## 구현 현황

### ✅ 구현 완료

**레거시 정리 완료**
- FogManager.cs / AbandonedTerritoryManager.cs 삭제
- Enums.cs — FogState 제거, Mansion 추가, 군사 건물 전체 반영
- Enums.cs — MageBarracks 제거, ArcheryRangeElite / MedicBarracksElite / KnightBarracks / TrapWorkshop 추가
- TileData.cs — fogState 필드 제거
- BuildingInstance.cs — wasEverSeen 필드 제거
- BuildingData.cs — id / maxPerTerritory / researchPerTurn / visualPrefab / productionInterval / unitCapacity / isUniqueGlobal 필드 추가
- BuildingInstance.cs — behaviour / savedState / isActive 필드 추가

**타일맵 구조 단순화**
- ProductionArea: Tilemap_City / Tilemap_Farmland / Tilemap_Buildings만 유지
- Ground / River / Gold / Territory / Fog 타일맵 제거
- TileMapManager Inspector 필드: cityTilemap / farmlandTilemap 두 개만
- NodeDataManager.NodeTerrainSource: cityTilemap / farmlandTilemap 두 개만

**건물 배치 시스템 (도언)**
- StarCraft 방식 배치 — TileMapManager, BuildingPlacementService
- Unity Tilemap 기반 배치 감지 / 반투명 프리뷰 (초록/빨강)
- 건물 크기별 기준점 계산 (1×1, 2×2, 3×3, 4×4)
- 재화 차감 및 효과 적용 / 입력·로직 분리
- 건물 자동 업그레이드 데이터 구조 (BuildingData 체인 / upgradesTo)
- Farm 8방향 스프라이트 오토타일링 (18종)
- CanPlace() — maxPerTerritory 영지 내 제한 + isUniqueGlobal 게임 전체 제한 검사 추가
- CreateBuildingVisual() — 프리팹 인스턴스화 후 SO 스프라이트 자동 적용 (프리팹은 구조, 스프라이트는 SO 관리)

**월드맵 + 영지 전환 시스템 (도언) ✅ 씬 연결 완료**
- WorldMapView Canvas — NodeButton_101~110 배치 완료
- TerritoryView (Grid) — ProductionArea/CombatArea 구조 완료
- FadeCanvas/FadePanel (CanvasGroup) 연결 완료
- WorldMapManager Inspector 연결 완료 (worldMapView/territoryView/fadePanel/allNodes)
- NodeDataManager nodeTerrains 연결 완료 (nodeID 101~110)
- NodeButton nodeID 연결 완료
- 노드 진입/이탈 페이드 전환 동작 확인
- CitySpawnManager 재설계 — NodeData 기반으로 city bounds 계산 (라이브 타일맵 스캔 제거)
- NodeData.hasPlayerUnits — 유닛 주둔 시 ownerCivID 무관 진입 허용
- WorldMapManager.SetNodeOwner() — 소유권 변경 시 isUniqueGlobal 건물 active 상태 자동 재조정
- WorldMapManager.RebalanceUniqueGlobalBuildings() — 플레이어 노드 스캔, 동일 buildingType 1개만 active 유지
- WorldMapManager.HasUniqueGlobalBuilding() — TileMapManager용 게임 전체 건물 존재 여부 공개 조회

**건물 Behaviour 시스템 (도언) ✅ 코드 완료 / 에디터 연결 대기 — 4단계 병영 재편 작업 필요**
- BuildingBehaviour / UnitProducerBehaviour 추상 클래스 계층 구조 구현
- 기존 Behaviour (Enums 재편 후 리네임/재구조화 필요):
  - BarracksBehaviour — 하급 병영(SoldierStone/Bronze/Iron) 공유, 근접 유닛만 생산
  - ArcherBarracksBehaviour + HealerBarracksBehaviour — 중급 병영(ArcheryRange_Medic/_Elite) 하나로 통합 필요 (힐러 1 + 궁수 1 동시 생산)
  - CavalryBarracksBehaviour + KnightBarracksBehaviour — 상급 병영(Stable_Knight) 하나로 통합 필요 (기사 1 + 기마병 1 동시 생산)
  - GiantBarracksBehaviour → WizardBarracksBehaviour 로 리네임 + 생산 유닛 변경 (자이언트 → 마법사, 3턴/1명, cap 5). 연구 포인트 200 게이트 그대로 유지
  - FarmBehaviour / MarketBehaviour / LabBehaviour / MansionBehaviour
  - PotionShopBehaviour / TrapWorkshopBehaviour
- BuildingRuntimeState — 노드 이탈/재진입 시 tick/activeCount/waiting 직렬화
- BuildingInstance.isActive — isUniqueGlobal 건물 중복 보유 시 활성/비활성 제어
- TileMapManager — visualPrefab 프리팹 분기 + Behaviour 캐싱/OnPlaced/LoadState 연결
- NodeDataManager.ExitNode — SaveState() 스냅샷 저장
- GameManager.EndTurn — 현재 노드 OnTurnEnd() + 오프스크린 노드 tick 가산
- MansionBehaviour.OnPlaced() → WorldMapManager.OnMansionRebuilt() 자동 호출 (카드 시스템 별도 호출 불필요)
- BuildingRuntimeState — 노드 이탈/재진입 시 tick/activeCount/waiting 직렬화
- BuildingInstance.isActive — isUniqueGlobal 건물 중복 보유 시 활성/비활성 제어
- TileMapManager — visualPrefab 프리팹 분기 + Behaviour 캐싱/OnPlaced/LoadState 연결
- NodeDataManager.ExitNode — SaveState() 스냅샷 저장
- GameManager.EndTurn — 현재 노드 OnTurnEnd() + 오프스크린 노드 tick 가산
- MansionBehaviour.OnPlaced() → WorldMapManager.OnMansionRebuilt() 자동 호출 (카드 시스템 별도 호출 불필요)

---

### ❌ 미구현 / 미연결

**도언 담당 — 에디터 작업**
- 건물 프리팹 제작 (SpriteRenderer + Behaviour 스크립트 부착. 스프라이트는 SO에서 관리)
- BuildingData SO 생성 및 연결 (새 4단계 병영 체계 기준):
  - 하급 병영: Barracks_SoldierStone / _SoldierBronze / _SoldierIron (체인)
  - 중급 병영: Barracks_ArcheryRange_Medic / _Elite (체인)
  - 상급 병영: Barracks_Stable_Knight (단일)
  - 최상급 병영: Barracks_Wizard (단일, 연구 200 게이트)
  - 체인 SO: Lab×3 / Mansion×3 / PotionBuilding×2 / TrapWorkshop×2 (시대별 스프라이트 + researchPerTurn 등 수치 입력)
  - 기존 SO: upgradesTo 체인 링크 + visualPrefab / unitCapacity / productionInterval 입력
  - Lab / Mansion SO: isUniqueGlobal = true 체크

**도언 담당 — 코드**
- 전투 승리 시 WorldMapManager.SetPlayerUnitsPresent() 연동
- 노드 피탈 시 건물 처리 (파괴 vs 유지 — 회의 후 구현)
- CombatArea 타일맵 NodeDataManager 복사 로직 (전투 시스템 구현 시)
- AI 노드 확장 로직

**타 팀원 담당 (연동 필요)**
- 인구 시스템 (건물 건설 시 출생 → 3턴 후 성인 자동 배치)
- 유닛 데이터 구조 (UnitData SO / UnitInstance)
- 전투 시스템 (카드 기반 독립 턴, 부대 이동/공격)
- 이벤트 시스템 (5턴마다 1회 보장 + 15% 확률)
- 턴 흐름 전체 GameManager 연결
- 카드 시스템 연동 후 PotionShopBehaviour / TrapWorkshopBehaviour TODO 완성
- 카드 시스템 연동 후 영주성 재건 카드 → TileMapManager.PlaceBuilding(center, mansionSO) 호출

---

## 미확정 항목

| 항목 | 상태 |
|------|------|
| 노드 수 및 배치 | ✅ 10개 확정, nodeID 101~110 |
| 빈 노드 점령 방식 | ✅ 인접 플레이어 노드에서 유닛 파견 → 영주성 재건 시 점령 완료 (재화 소모 점령 방식 폐기) |
| 인구 한도 민가당 증가량 (시대별) | ✅ +5/+7/+10 |
| 시대 전환 연구 포인트 | ✅ 청동기 100 / 철기 200 |
| 카드 3택 가중치 수치 | 미확정 |
| 자동 업그레이드 대상 건물 종류 (군사 외) | ✅ Lab×3단계 / Mansion×3단계 / PotionBuilding×2단계 / TrapWorkshop×2단계 확정. Market / House / Farm 미확정 |
| 전투 중 식량 소모량 (유닛 수 × n) | 미확정 |
| goldPerTurn / researchPerTurn 수치 | 미확정 |
| 카드 목록 전체 (기본 카드 8장 포함) | 미확정 |
| 노드 피탈 시 건물 처리 방식 | ✅ 전투 승리 시 군사 건물 / Lab / PotionBuilding / TrapWorkshop 은 완전 파괴, Mansion/House/Market/Farm 만 잔해로 유지. 승리 턴에 "전체 재건 카드" 자동으로 플레이어 핸드에 추가됨 → 사용 시 영주성+잔해 전부 복구. 상세: `UNIT_MOVEMENT_COMBAT_SPEC.md` |
