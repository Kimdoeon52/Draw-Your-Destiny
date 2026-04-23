namespace NYH.BattleCardSystem
{
    using SerializeReferenceEditor;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Serialization;
    using NYH.CoreCardSystem;

    public enum BattleCardType
    {
        Attack, // 공격
        Move,  // 이동
        Skill, // 특수 행동 (공격/이동 외 효과, 예: 버프, 디버프, 힐 등)
        Potion, // 포션 - 경제 트리를 갔을 때만 얻을 수 있는 포션 
        Trap,   // 함정 - 전투 트리를 갔을 때만 얻을 수 있는 함정 카드
    }

    public enum BattleCardKeyword
    {
        None,  // 타입 없음
        Ranged, //원거리
        Melle, //근거리 
        Stun,  //기절
        Slow,  //둔화
        Disarm, //무장해제(n 턴간 공격력 0으로)
        Push,   //밀기
        Pull,   //당기기
        MoveSpeedUp, //이동속도 증가
        AttackPowerUp, //공격력 증가
        AreaAttack, //범위 공격
        NonPiercing, //비관통
    }

    public enum BattleAttackPattern
    {
        [InspectorName("없음/기본 단일")]
        None,

        [InspectorName("상하좌우 인접 4칸")]
        Adjacent4,

        [InspectorName("직선")]
        Line,

        [InspectorName("범위")]
        Area,
    }

    public enum BattleAttackPatternOriginMode
    {
        [InspectorName("원거리 패턴 - 선택한 칸 기준")]
        RangedPattern,

        [InspectorName("근거리 패턴 - 유닛 앞 기준")]
        MeleePattern,
    }

    public enum BattleUnitTargetFilter
    {
        [InspectorName("적만")]
        EnemiesOnly,

        [InspectorName("아군만")]
        AlliesOnly,

        [InspectorName("모든 유닛")]
        AllUnits,
    }

    public enum BattleCardTargetingMode
    {
        [InspectorName("자동")]
        Auto,

        [InspectorName("직접 효과")]
        DirectEffect,

        [InspectorName("이동 전용")]
        MoveOnly,

        [InspectorName("공격 전용")]
        AttackOnly,

        [InspectorName("이동 후 공격")]
        MoveThenAttack,

        [InspectorName("공격 후 이동")]
        AttackThenMove,
    }

    [CreateAssetMenu(menuName = "Data/Battle Card")]
    public class BattleCardData : CardDataBase
    {
        [Header("기본 정보")]
        [Tooltip("카드의 ID 중복되지 않게 지정")]
        [field: SerializeField] public int CardID { get; private set; }
        [Tooltip("카드의 이름")]
        [field: SerializeField] public string CardName { get; private set; }
        [Tooltip("카드의 타입")]
        [field: SerializeField] public BattleCardType CardType { get; private set; }
        [Tooltip("전투 카드 가장 큰 이미지")]
        [field: SerializeField] public Sprite Image { get; private set; }
        [Tooltip("전투 카드의 공격 범위를 보여줄 이미지")]
        [field: SerializeField] public Sprite GridImage { get; private set; }

        [Header("전투 코스트 / 덱 규칙")]
        [field: FormerlySerializedAs("<FoodCost>k__BackingField")]
        [Tooltip("전투 코스트 사용량")]
        [field: SerializeField] public int ActionPointCost { get; private set; }
        [Tooltip("디스플레이 이동 범위")]
        [field: SerializeField] public int DisplayMoveRange { get; private set; }
        [Tooltip("덱 제한에 걸리는 카드인지 아닌지")]
        [field: SerializeField] public bool IgnoresDeckLimit { get; private set; }
        [Header("사용 시 소멸 여부")]
        [field: SerializeField] public bool IsConsumable { get; private set; } = false;

        [Header("타게팅 방식")]
        // Auto는 카드 이펙트를 보고 기존 카드도 자동으로 분류하도록 두고,
        // 필요할 때만 자산에서 강제로 모드를 지정합니다.
        [field: SerializeField] public BattleCardTargetingMode TargetingMode { get; private set; } = BattleCardTargetingMode.Auto;

        [Header("사용 가능 병종 제한")]
        [Tooltip("비워두면 모든 아군 전투 유닛이 사용할 수 있습니다.\n값을 넣으면 해당 UnitType 병종만 이 카드를 사용할 수 있습니다.")]
        [SerializeField] private List<UnitType> allowedUserUnitTypes = new();

        [Header("키워드")]
        [field: SerializeField] public List<BattleCardKeyword> Keywords { get; private set; } = new();

        [Header("추가 이펙트")]
        [field: SerializeReference, SR] public List<Effect> Effects { get; private set; }

        [Header("카드 설명")]
        [SerializeField]
        [TextArea(3, 5)]
        private string description;

        public string Description => description;
        public IReadOnlyList<UnitType> AllowedUserUnitTypes => allowedUserUnitTypes;

        public override int SharedCardID => CardID;
        public override string SharedCardName => CardName;
        public override Sprite SharedImage => Image;
        public override string SharedDescription => Description;
        public override int SharedBaseCost => ActionPointCost;
        public override IReadOnlyList<Effect> SharedEffects => Effects;
        
    }
}
