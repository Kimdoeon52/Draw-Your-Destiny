# Bank 건물 구현 프롬프트

아래 프롬프트를 Claude Code (Sonnet)에 그대로 붙여넣어 사용하세요.

---

## 프롬프트

```
이 프로젝트는 Unity 6 C# 턴제 전략 + 덱빌딩 게임이야. CLAUDE.md를 먼저 읽고 프로젝트 구조를 파악해.

은행(Bank) 건물을 구현해야 해. 설계 명세서는 Assets/KDU/Md/BANK_BUILDING_SPEC.md에 있으니 반드시 먼저 읽어.

구현 전에 반드시 아래 파일들을 읽고 기존 코드 패턴을 파악해:
- Assets/KDU/Scripts/TileMap/Data/Enums.cs (BuildingType enum)
- Assets/KDU/Scripts/TileMap/Data/BuildingData.cs (SO 구조)
- Assets/KDU/Scripts/TileMap/Data/BuildingInstance.cs (런타임 인스턴스)
- Assets/KDU/Scripts/TileMap/Data/BuildingRuntimeState.cs (상태 저장)
- Assets/KDU/Scripts/TileMap/Behaviour/BuildingBehaviour.cs (추상 부모)
- Assets/KDU/Scripts/TileMap/Behaviour/MarketBehaviour.cs (비슷한 기반 건물 참고)
- Assets/KDU/Scripts/TileMap/Behaviour/LabBehaviour.cs (비생산 건물 참고)
- Assets/KDU/Scripts/WorldMap/WorldMapManager.cs (CurrentNodeID, GetNode 등)
- Assets/KDU/Scripts/WorldMap/NodeData.cs (노드 데이터)

작업 순서:

1. Enums.cs에 BuildingType.Bank 추가 (Farm 아래, 기반 건물 섹션)

2. NodeData.cs에 노드별 자원 필드 추가 (gold, food, research — int, 기본값 0). 주석 스타일은 기존 NodeData 필드와 동일하게.

3. WorldMapManager.cs에서 currentNodeID를 외부에서 읽을 수 있는지 확인. 안 되면 public int CurrentNodeID => currentNodeID; 프로퍼티 추가.

4. BankBehaviour.cs 생성 (Assets/KDU/Scripts/TileMap/Behaviour/BankBehaviour.cs)
   - BuildingBehaviour 직접 상속 (UnitProducerBehaviour 아님)
   - usedThisTurn: bool — 턴당 1회 제한
   - OnPlaced(): usedThisTurn = false
   - OnTurnEnd(): usedThisTurn = false (턴 리셋)
   - TransferResources(int targetNodeID, int goldAmount, int foodAmount, int researchAmount): bool
     - 현재 노드에서 대상 노드로 자원 이동
     - 검증: usedThisTurn 체크, 인접 노드 체크, 플레이어 소유 체크, 보유량 초과 불가
     - 성공 시 usedThisTurn = true
   - GetTransferableNodes(): List<NodeData> — 인접 플레이어 소유 노드 목록 반환
   - SaveState() / LoadState() — usedThisTurn은 턴 리셋이라 저장 불필요, 기본 BuildingRuntimeState 반환
   - 명세서의 코드 예시를 참고하되 기존 프로젝트 코드 스타일에 맞춰서 작성해.

5. BankTransferUI.cs 생성 (Assets/KDU/Scripts/UI/BankTransferUI.cs)
   - 싱글톤 패턴 (기존 프로젝트의 Singleton<T> 사용하지 않고 간단히 static Instance)
   - Open(BankBehaviour bank): 운송 패널 열기. 인접 노드 버튼 동적 생성.
   - OnNodeSelected(int nodeID): 노드 선택 시 슬라이더 활성화 + 최대값을 현재 노드 보유량으로 설정
   - OnTransferClicked(): TransferResources 호출 후 패널 닫기
   - 자원 UI 갱신은 TODO 주석으로 남겨두기 (다른 팀원 담당)
   - SerializeField로 UI 요소 참조 (panel, nodeListParent, nodeButtonPrefab, goldSlider, foodSlider, researchSlider, transferButton, closeButton)

주의사항:
- namespace 사용하지 마
- 주석은 한국어로, 간결하게 (기존 코드 스타일 따라가)
- 기존 파일 수정 시 최소한의 변경만 해 (관련 없는 코드 건드리지 마)
- Unity 6 API 사용 (FindFirstObjectByType 등)
- 파일 헤더 주석은 기존 BuildingBehaviour.cs 스타일로 작성
- UI 프리팹은 에디터 작업이라 코드만 작성하면 돼
- BuildingData SO 생성도 에디터 작업이라 코드에서 할 것 없음
- 구현 전에 컨펌 받지 말고 바로 구현해
```

---

## 구현 후 에디터 작업 (수동)

코드 구현이 끝나면 Unity Editor에서 수동으로 해야 할 작업:

1. **BuildingData SO 3개 생성** (Create > KDU > Building)
   - Building_Bank_Stone: id="bank_stone", buildingType=Bank, requiredEra=Stone, isAutoUpgrade=true, upgradesTo=Building_Bank_Bronze, goldCost=50, width=2, height=2, allowedTiles=[City], maxPerTerritory=1
   - Building_Bank_Bronze: id="bank_bronze", buildingType=Bank, requiredEra=Bronze, isAutoUpgrade=true, upgradesTo=Building_Bank_Iron, goldCost=50, width=2, height=2
   - Building_Bank_Iron: id="bank_iron", buildingType=Bank, requiredEra=Iron, isAutoUpgrade=false, upgradesTo=null, goldCost=50, width=2, height=2

2. **은행 프리팹 제작**
   - 빈 GameObject → SpriteRenderer + BankBehaviour + BoxCollider2D 부착
   - 3개 SO의 visualPrefab 필드에 이 프리팹 연결

3. **BankTransferUI 프리팹 제작**
   - Canvas 하위에 Panel 생성
   - 노드 리스트 영역 + 슬라이더 3개 (금/식량/연구) + 전송 버튼 + 닫기 버튼
   - BankTransferUI 스크립트 부착 후 SerializeField 연결

4. **시대별 스프라이트 준비**
   - 각 SO의 sprite 필드에 시대별 은행 스프라이트 연결
