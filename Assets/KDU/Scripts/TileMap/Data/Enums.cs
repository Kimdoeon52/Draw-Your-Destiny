// 타일 지형 타입
public enum TileType
{
    Plain,      // 평지 (건설 가능)
    River,      // 강 (건설 불가, 이동 불가)
    Farmland,   // 농경지 (농장 전용)
    Resource,   // 자원 타일 (금광 등)
    City        // 도시 타일 (일반 건물 배치 불가)
}

// 건물 타입
public enum BuildingType
{
    None,

    // 기반 건물
    House,      // 민가 (인구 한도 증가)
    Market,     // 시장 (금 생산)
    Lab,        // 연구소 (연구 포인트 생산)
    Farm,       // 농장 (농경지 전용)

    // 군사 건물 — 시대별 자동 업그레이드 체인
    TribePracticeGround,    // 부족 훈련지 (석기)
    TrainingCamp,           // 훈련소 (청동기)
    Barracks,               // 병영 (철기)

    // 확장 건물
    Outpost,    // 소규모 영지 (8x8 영토 + 영구 시야)
    Wall        // 성벽 (시대 전환 시 스프라이트 자동 교체)
}

// 시대
public enum Era
{
    Stone,      // 석기시대 (약 1~15턴)
    Bronze,     // 청동기시대 (약 16~30턴)
    Iron        // 철기시대 (약 31턴~)
}

// 안개 전쟁 상태
public enum FogState
{
    Hidden,     // 완전 암흑
    Explored,   // 지형 + 마지막으로 본 건물만 공개
    Visible     // 완전 공개
}

// 시민 역할
// 여성은 모든 생산 역할에서 보너스 / 남성 전용은 Soldier뿐
public enum UnitRole
{
    Idle,       // 미배치
    Farmer,     // 농민 (여성 보너스)
    Laborer,    // 노역자 (여성 보너스)
    Soldier,    // 병사 (남성 전용, 전역 카드 전까지 고정)
    Textile     // 직물/교역 (여성 보너스)
}
