// ============================================================
// Enums.cs — 프로젝트 전역 열거형 모음
// 새 값을 추가할 때 이 파일 하나만 수정하면 된다.
// ============================================================

// ── 타일 지형 타입 ────────────────────────────────────────
// TileMapManager.GetTileType()이 우선순위(River > Resource > Farmland > City > Plain)
// 순서로 판별한다. 한 타일에 여러 레이어가 겹쳐도 가장 높은 우선순위 타입이 반환됨.
public enum TileType
{
    Plain,      // 평지 — 건물 배치 불가
    River,      // 강   — 이동/건설 모두 불가
    Farmland,   // 농경지 — Farm(농장)만 배치 가능
    Resource,   // 금광  — 특수 건물만 가능 (추후 구현)
    City        // 영지/도시 — 일반 건물 배치 가능
}

// ── 건물 타입 ────────────────────────────────────────────
// BuildingData ScriptableObject와 1:1 매핑됨.
// 코드에서 타입 분기가 필요할 때 buildingType 필드로 판별.
public enum BuildingType
{
    None,

    // 기반 건물 — City 타일 위에 배치
    Mansion,    // 영주성 — 영주성 재건 카드로만 설치. 영지 잠금 해제 조건
    House,      // 민가  — 인구 한도(populationCap) 증가
    Market,     // 시장  — 턴마다 금(gold) 생산
    Lab,        // 연구소 — 턴마다 연구 포인트(research) 생산
    Farm,       // 농장  — Farmland 전용, 식량 생산
    Bank,       // 은행  — 인접 자기 노드로 자원 운송 (턴당 1회)

    // 군사 건물 — 근접 체인 (석기→청동기→철기 자동 업그레이드)
    Barracks_SoldierStone,    // 부족 훈련지 (석기)   isAutoUpgrade=true, upgradesTo=Barracks_SoldierBronze
    Barracks_SoldierBronze,           // 훈련소     (청동기)  isAutoUpgrade=true, upgradesTo=Barracks_SoldierIron
    Barracks_SoldierIron,               // 병영       (철기)   최종 단계

    // 군사 건물 — 궁수 체인 (청동기→철기 자동 업그레이드)
    Barracks_ArcheryRange,           // 사격장         (청동기) isAutoUpgrade=true, upgradesTo=ArcheryRangeElite
    Barracks_ArcheryRangeElite,      // 정예 사격장     (철기)  최종 단계

    // 군사 건물 — 힐러 체인 (청동기→철기 자동 업그레이드)
    Barracks_Medic,          // 의무병 막사     (청동기) isAutoUpgrade=true, upgradesTo=Barracks_MedicElite
    Barracks_MedicElite,     // 정예 의무병 막사 (철기)  최종 단계

    // 군사 건물 — 단일 (철기부터)
    Barracks_Stable,         // 기마병 막사   (철기)
    Barracks_Knight,         // 기사단 막사   (철기) — 풀플레이트 아머 기사
    Barracks_Giant,          // 자이언트 막사 (철기, 연구포인트 200 도달 시부터 유닛 생산 시작)

    // 지원 건물
    PotionBuilding,         // 포션 가게  (청동기~) — 매 턴 덱에 랜덤 포션 카드 추가
    TrapWorkshop            // 덫 공방    (청동기~) — 매 턴 덱에 랜덤 덫 카드 추가
}

// ── 시대 ─────────────────────────────────────────────────
// 시대가 오르면 해당 requiredEra 건물 해금, 자동 업그레이드 체인 실행
public enum Era
{
    Stone,      // 석기시대 (1~15턴 예상)
    Bronze,     // 청동기시대 — 연구 포인트 100 달성 시 전환
    Iron        // 철기시대  — 연구 포인트 200 달성 시 전환
}

// ── 시민 역할 ─────────────────────────────────────────────
// 인구 개별 관리 방식에서 시민 한 명의 현재 역할을 나타냄 (미구현)
public enum UnitRole
{
    Idle,       // 미배치 — 역할 없음
    Farmer,     // 농민
    Laborer,    // 노역자
    Soldier     // 병사 — 전역 카드 전까지 역할 변경 불가
}
