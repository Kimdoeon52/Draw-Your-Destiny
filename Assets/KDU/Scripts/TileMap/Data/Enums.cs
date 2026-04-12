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

    // 군사 건물 — 시대 전환 시 자동 업그레이드 체인
    // BuildingData.isAutoUpgrade = true, upgradesTo = 다음 단계 SO
    TribePracticeGround,    // 부족 훈련지 (석기시대)
    TrainingCamp,           // 훈련소   (청동기시대로 자동 업그레이드)
    Barracks,               // 병영    (철기시대로 자동 업그레이드, 최종 단계)
    ArcheryRange,           // 사격장  (청동기~)
    MedicBarracks,          // 의무병 막사 (청동기~)
    StableBarracks,         // 기마병 막사 (철기)
    GiantBarracks,          // 자이언트 막사 (철기)
    MageBarracks,           // 마법사 막사 (철기, 연구포인트 200 필요)
    PotionBuilding          // 포션 건물 (청동기~)
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
