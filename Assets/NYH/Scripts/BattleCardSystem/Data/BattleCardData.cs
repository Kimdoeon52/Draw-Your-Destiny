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
        None,
        Adjacent4,
        Line,
        Area,
    }

    [CreateAssetMenu(menuName = "Data/Battle Card")]
    public class BattleCardData : CardDataBase
    {
        [Header("기본 정보")]
        [field: SerializeField] public int CardID { get; private set; }
        [field: SerializeField] public string CardName { get; private set; }
        [field: SerializeField] public BattleCardType CardType { get; private set; }
        [field: SerializeField] public Sprite Image { get; private set; }

        [Header("전투 코스트 / 덱 규칙")]
        [field: FormerlySerializedAs("<FoodCost>k__BackingField")]
        [field: SerializeField] public int ActionPointCost { get; private set; }
        [field: SerializeField] public bool IgnoresDeckLimit { get; private set; }
        [field: SerializeField] public bool IsConsumable { get; private set; } = true;

        [Header("키워드")]
        [field: SerializeField] public List<BattleCardKeyword> Keywords { get; private set; } = new();

        [Header("이동 카드 설정")]
        [field: SerializeField] public int MoveAmount { get; private set; }

        [Header("공격 카드 설정")]
        [field: SerializeField] public int AttackDamage { get; private set; }
        [field: SerializeField] public int AttackRange { get; private set; } = 1;
        [field: SerializeField] public int AttackTargetCount { get; private set; } = 1;
        [field: SerializeField] public bool HitsAllTargetsInRange { get; private set; }
        [field: SerializeField] public BattleAttackPattern AttackPattern { get; private set; } = BattleAttackPattern.None;

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
