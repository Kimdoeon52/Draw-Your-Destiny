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
├── 월드맵 뷰 (UI Canvas 기반)
│   ├── 배경: 손그림 스타일 지도 이미지
│   ├── 노드: 버튼/이미지 컴포넌트 (30개 이하)
│   ├── 노드 연결선: UI Line 또는 이미지로 인접 관계 표시
│   └── HUD: 재화, 턴 수, 카드 UI 등
│
└── 영지 뷰 (Tilemap 기반) ← 노드 클릭 시 페이드 전환
    ├── ProductionArea (건물/생산 공간 20×20)
    │   ├── Tilemap_Ground
    │   ├── Tilemap_Farmland
    │   ├── Tilemap_River
    │   ├── Tilemap_City
    │   └── Tilemap_Buildings
    └── CombatArea (전투 공간 20×20)
        ├── Tilemap_Ground
        ├── Tilemap_Forest    // 이동 불가
        ├── Tilemap_River     // 이동 2칸 소비
        └── Tilemap_Units
```

- 월드맵은 UI Canvas 기반. 타일맵 사용 안함.
- 영지 뷰는 씬 상주 NodePrefab 인스턴스 1개를 재사용 (풀링).
- 평소에는 ProductionArea 활성화, 전투 선언 시 CombatArea로 전환.
- 안개 전쟁 없음. 모든 노드 항상 공개.
- 미니맵 없음.

### 턴 흐름

```
① 영지 선택 + 카드 드로우 (5장)
② 내 영지 행동 — 건물 건설, 재화 생산 (중립/경제 카드 사용)
③ 적 영지 공격 — 인접 적 노드 선택 시 전투 돌입
   └── 승리: 해당 노드 점령 / 패배: 병력 전부 소모
④ 빈 노드 점령 — 재화 소모 후 즉시 점령
⑤ 카드 3택 — 문명카드+전투카드 세트 3개 제시, 1세트 선택 또는 패스
⑥ ①~⑤ 반복
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

### NodeData

```csharp
public class NodeData
{
    public int nodeID;
    public List<int> adjacentNodeIDs;       // 인접 노드 ID (공격 가능 여부 판단)
    public int ownerCivID;                  // -1 = 빈 노드, 0~3 = 문명 ID
    public bool isMansionBuilt;             // 영주성 재건 여부
    public List<BuildingInstance> buildings;
}
```

civID: 0=플레이어(파랑), 1=AI1(빨강), 2=AI2(초록), 3=AI3(노랑)

### 노드 종류

| 종류 | 설명 |
|------|------|
| 아군 노드 | 플레이어 소유. 영지 진입 및 건물 배치 가능 |
| 적 노드 | AI 소유. 인접 아군 노드에서 공격 선언 가능 |
| 빈 노드 | 미점령. 재화 n 소모 후 즉시 점령. 점령 후 isMansionBuilt = false |

### 노드 진입/이탈 흐름

```
월드맵에서 아군 노드 클릭
 → 페이드 아웃
 → 씬 상주 NodePrefab 인스턴스 재사용 (Instantiate 없음)
 → 타일맵 클리어 → NodePrefab_XX 지형 데이터 복사
 → NodeData에서 buildings 복원 (BuildingData.id 기반 재생성)
 → isMansionBuilt == false → 배치 UI 전체 잠금
 → 페이드 인 → 영지 뷰 활성화

영지 뷰에서 나가기
 → NodeData에 현재 buildings 저장
 → 타일맵 클리어
 → 페이드 → 월드맵 뷰 복귀
```

### 영주성 재건 카드 흐름

```
빈 노드 점령
 → ownerCivID = 플레이어, isMansionBuilt = false
 → 노드 진입 가능하나 배치 UI 전체 잠금

영지 뷰 진입 후 영주성 재건 카드 사용
 → 영주성 GameObject 중앙 배치
 → isMansionBuilt = true
 → 배치 UI 잠금 해제
 → 이후 건물/농장 배치 가능
```

### NodeDataManager 역할

- 씬 상주 NodePrefab 인스턴스 1개 보유 (GC 방지 풀링)
- 노드 진입 시: 타일맵 클리어 → 해당 프리팹 지형 복사 → buildings 복원
- 노드 이탈 시: 현재 buildings → NodeData 저장
- Tilemap은 View 전용. 상태 변경은 반드시 NodeDataManager를 통해서만.

---

## 영지 프리팹 구조

노드마다 프리팹 1개. 지형은 Unity 에디터에서 직접 타일 찍어 고정.

```
NodePrefab_XX
├── ProductionArea          // 건물/생산 공간 (20×20)
│   ├── Tilemap_Ground      // 평지
│   ├── Tilemap_Farmland    // 농경지 (Farm 전용)
│   ├── Tilemap_River       // 강 (이동/건설 불가)
│   ├── Tilemap_City        // 도시 (일반 건물 배치 가능)
│   └── Tilemap_Buildings   // 런타임 건물 생성 레이어
└── CombatArea              // 전투 공간 (20×20)
    ├── Tilemap_Ground
    ├── Tilemap_Forest      // 이동 불가
    ├── Tilemap_River       // 이동 2칸 소비
    └── Tilemap_Units       // 런타임 유닛 배치 레이어
```

### 타일 종류 (TileType)

| 타입 | 건설 가능 | 특성 |
|------|------|------|
| Plain | X | 일반 지형. 건물 배치 불가 |
| River | X | 이동 불가, 건설 불가 |
| Farmland | △ | Farm(농장) 전용 |
| City | O | 일반 건물 배치 가능 |
| Forest | X | 전투 공간 전용. 이동 불가 |

---

## 건물 시스템

### BuildingType

```csharp
// 기반 건물
Mansion,  // 영주성 — 영주성 재건 카드로만 설치. 영지 잠금 해제 조건
House,    // 민가 — 인구 한도 증가
Market,   // 상점 — 매 턴 금 획득
Lab,      // 연구소 — 매 턴 연구 포인트 획득. 게임당 1개 제한
Farm,     // 농장 — 매 턴 식량 획득. 영지당 최대 2개 제한 (2×2 크기)

// 군사 건물 — 시대별 자동 업그레이드 체인
TribePracticeGround → TrainingCamp → Barracks  // 돌도끼병
ArcheryRange                                    // 궁수 (청동기~)
MedicBarracks                                   // 의무병 (청동기~)
StableBarracks                                  // 기마병 (철기)
GiantBarracks                                   // 자이언트 (철기)
MageBarracks                                    // 마법사 (철기, 연구포인트 200 필요)
PotionBuilding                                  // 포션 건물 (청동기~)
```

### 건물 제한

| 건물 | 제한 |
|------|------|
| 농장 (Farm) | 영지당 최대 2개 |
| 연구소 (Lab) | 게임 전체에서 1개 |
| 영주성 (Mansion) | 영지당 1개. 영주성 재건 카드로만 설치 |

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
int soldierCapacity;        // 군사 건물 전용
int goldPerTurn;
int researchPerTurn;
int populationCapBonus;     // 민가 전용
int maxPerTerritory;        // 영지당 최대 설치 수 (-1 = 무제한)
```

### 건물 기준점 계산 (StarCraft 방식)

```
1×1: 클릭 지점이 건물 중심
2×2: 클릭 지점이 좌하단 모서리
3×3: 클릭 지점이 정중앙
4×4: 2×2와 동일하게 좌하단 모서리
```

### 배치 가능 조건

1. 맵 범위 안
2. Tilemap_River에 타일 없음
3. 해당 위치에 건물 없음
4. BuildingData.allowedTiles에 타일 타입 포함
   - 일반 건물: allowedTiles = [City]
   - Farm: allowedTiles = [Farmland]
   - Mansion: allowedTiles = [City] (중앙 고정 배치)
5. isMansionBuilt == true (Mansion 자신 제외)
6. maxPerTerritory 초과 여부 확인

### 건물 배치 흐름

```
카드 선택
 → BuildingPlacementService.StartPlacing()
 → 마우스 따라다니는 프리뷰 (초록/빨강)
 → 좌클릭: 배치 시도
 → TileMapManager.PlaceBuilding()
 → BuildingInstance 생성
 → GameObject 생성 (Tilemap_Buildings 컨테이너)
 → NodeData.buildings에 추가
 → 재화 차감 및 효과 적용
```

---

## 전투 시스템

### 전투 흐름

```
월드맵에서 인접 적 노드 공격 선언
 → 영지 뷰 진입 (CombatArea 활성화)
 → 전투 진행 (카드 기반 독립 턴제)
 → 공격자 승리 (방어 유닛 전멸): ownerCivID 변경 → 월드맵 복귀
 → 방어자 승리 (공격 유닛 전멸 또는 식량 고갈): 공격 유닛 전부 소멸 → 월드맵 복귀
```

### 전투 지형 효과

| 지형 | 효과 |
|------|------|
| 평지 | 이동 1칸 소비 |
| 숲 / 바위 | 이동 불가 |
| 강 | 이동 2칸 소비 |

### 유닛 종류 (6종)

| 유닛 | 등장 시대 | 특성 |
|------|------|------|
| 돌도끼병 | 석기 | 기본 근접 유닛 |
| 의무병 | 청동기 | 힐러. 아군 체력 회복 |
| 궁수 | 청동기 | 원거리 공격 유닛 |
| 기마병 | 철기 | 고속 이동. 말 체력 소진 시 근접 유닛으로 전환 |
| 자이언트 | 철기 | 대형 고체력 근접 유닛 |
| 마법사 | 철기 | 광역 공격. 연구포인트 200 달성 후 건물 설치 가능 |

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

**건물 배치 시스템 (도언)**
- 건물 배치 시스템 (StarCraft 방식) — TileMapManager, BuildingPlacementService
- Unity Tilemap 기반 배치 감지 / 반투명 프리뷰 (초록/빨강)
- 건물 크기별 기준점 계산 (1×1, 2×2, 3×3, 4×4)
- 재화 차감 및 효과 적용 / 성능 최적화 (CanPlace 캐싱) / 입력·로직 분리
- 프리뷰 위치 버그 수정 (RoundToInt 오차 제거 — 짝수 크기 건물 반 타일 밀림 해결)
- 건물 자동 업그레이드 데이터 구조 (BuildingData 체인 / upgradesTo)

---

### ❌ 미구현

**도언 담당**
- 세계지도형 노드 맵 UI (WorldMapManager, NodeButton)
- NodeDataManager (노드 진입/이탈 저장·복원, 풀링)
- 노드별 영지 프리팹 제작 및 지형 편집
- 영주성 재건 카드 동작
- AI 노드 확장 로직

**타 팀원 담당 (연동 필요)**
- 인구 시스템 (건물 건설 시 출생 → 3턴 후 성인 자동 배치)
- 유닛 데이터 구조 (UnitData SO / UnitInstance)
- 전투 시스템 (카드 기반 독립 턴, 부대 이동/공격)
- 이벤트 시스템 (5턴마다 1회 보장 + 15% 확률)
- 턴 흐름 전체 GameManager 연결

---

## 미확정 항목

| 항목 | 상태 |
|------|------|
| 노드 수 및 배치 | 30개 이하 예정, 미확정 |
| 빈 노드 점령 재화 비용 | 미확정 |
| 인구 한도 민가당 증가량 (시대별) | ✅ +5/+7/+10 |
| 시대 전환 연구 포인트 | ✅ 청동기 100 / 철기 200 |
| 카드 3택 가중치 수치 | 미확정 |
| 자동 업그레이드 대상 건물 종류 (군사 외) | 미확정 |
| 전투 중 식량 소모량 (유닛 수 × n) | 미확정 |
| goldPerTurn / researchPerTurn 수치 | 미확정 |
| 카드 목록 전체 (기본 카드 8장 포함) | 미확정 |

## ⚠️ 레거시 코드 정리 (v0.6 전환 작업)

기획 v0.6으로 전환되면서 아래 시스템이 제거되었다.
Claude Code는 이 파일을 처음 읽을 때 아래 목록을 기준으로
불필요한 코드를 정리해야 한다.

### 삭제 대상 파일

| 파일 | 이유 |
|------|------|
| FogManager.cs | 안개 전쟁 시스템 제거됨 |
| AbandonedTerritoryManager.cs | 버려진 영지 시스템 제거됨 |
| FOG_OF_WAR.md | 삭제된 시스템 문서 |

### 리팩토링 대상 파일

| 파일 | 작업 내용 |
|------|------|
| TileMapManager.cs | ClaimTerritory / ExpandTerritory / TransferTerritory / InstallWallPerimeter / ExpandOutpostArea / FogManager 호출부 전부 제거 |
| BuildingPlacementService.cs | FogManager.OnBuildingPlaced() 호출부 제거 |
| Enums.cs | FogState, UnitRole(Textile 제거) 정리. Wall / Outpost BuildingType 제거 |
| BuildingInstance.cs | wasEverSeen 필드 제거 |

### 유지 파일

| 파일 | 비고 |
|------|------|
| TileMapManager.cs | 위 리팩토링 후 유지 |
| BuildingPlacementService.cs | 위 리팩토링 후 유지 |
| BuildingPlacementController.cs | 그대로 유지 |
| BuildingPreview.cs | 그대로 유지 |
| BuildingData.cs | id / maxPerTerritory 필드 추가 필요 |
| BuildingInstance.cs | wasEverSeen 제거 후 유지 |
| Singleton.cs | 그대로 유지 |

### 신규 생성 대상

| 파일 | 내용 |
|------|------|
| WorldMap/NodeData.cs | 노드 런타임 상태 클래스 |
| WorldMap/WorldMapManager.cs | 노드 맵 전체 관리 싱글톤 |
| WorldMap/NodeButton.cs | 노드 UI 버튼 동작 |
| TileMap/Management/NodeDataManager.cs | 노드 진입/이탈 저장·복원, 풀링 |