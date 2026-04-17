namespace NYH.BattleCardSystem
{
    using SerializeReferenceEditor;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Serialization;
    using NYH.CoreCardSystem;

    public enum BattleCardType
    {
        Attack,
        Move,
        Skill,
        Potion,
    }

    public enum BattleCardKeyword
    {
        None,
        Stun,
        Slow,
        Disarm,
        Push,
        Pull,
        MoveSpeedUp,
        AttackPowerUp,
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
        [field: SerializeField] public int CardID { get; private set; }
        [field: SerializeField] public string CardName { get; private set; }
        [field: SerializeField] public BattleCardType CardType { get; private set; }
        [field: SerializeField] public Sprite Image { get; private set; }
        [field: SerializeField] public Sprite GridImage { get; private set; }

        [Header("전투 코스트 / 덱 규칙")]
        [field: FormerlySerializedAs("<FoodCost>k__BackingField")]
        [field: SerializeField] public int ActionPointCost { get; private set; }
        [field: SerializeField] public int DisplayMoveRange { get; private set; }
        [field: SerializeField] public bool IgnoresDeckLimit { get; private set; }
        [Header("사용 시 소멸 여부")]
        [field: SerializeField] public bool IsConsumable { get; private set; } = false;

        [Header("타게팅 방식")]
        // Auto는 카드 이펙트를 보고 기존 카드도 자동으로 분류하도록 두고,
        // 필요할 때만 자산에서 강제로 모드를 지정합니다.
        [field: SerializeField] public BattleCardTargetingMode TargetingMode { get; private set; } = BattleCardTargetingMode.Auto;

        [Header("키워드")]
        [field: SerializeField] public List<BattleCardKeyword> Keywords { get; private set; } = new();

        [Header("추가 이펙트")]
        [field: SerializeReference, SR] public List<Effect> Effects { get; private set; }

        [Header("카드 설명")]
        [SerializeField]
        [TextArea(3, 5)]
        private string description;

        public string Description => description;

        public override int SharedCardID => CardID;
        public override string SharedCardName => CardName;
        public override Sprite SharedImage => Image;
        public override string SharedDescription => Description;
        public override int SharedBaseCost => ActionPointCost;
        public override IReadOnlyList<Effect> SharedEffects => Effects;
    }
}
