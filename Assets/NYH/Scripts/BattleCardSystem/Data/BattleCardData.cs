namespace NYH.BattleCardSystem
{
    using SerializeReferenceEditor;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Serialization;

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
    /*
     * BattleCardData
     *
     * 역할:
     * - 전투 카드 1장의 원본 데이터를 정의하는 ScriptableObject입니다.
     * - 카드 종류, 식량 코스트, 이동/공격 수치, 키워드, 설명을 보관합니다.
     *
     * 인스펙터에서 넣는 것:
     * - Card Type: Attack / Move / Skill / Potion
     * - Food Cost: 전투 중 사용하는 식량 코스트
     * - Move Amount: 이동 카드일 때 기본 이동량
     * - Attack Damage / Range / Pattern: 공격 카드일 때 기본 공격 정보
     * - Ignores Deck Limit: 포션처럼 30장 제한을 무시해야 할 때 사용
     *
     * 사용하는 법:
     * - 이 SO를 BattleCardCatalog에 등록합니다.
     * - 보상으로 얻거나 기본 전투 덱에 넣을 때 이 에셋을 사용합니다.
     */
    public class BattleCardData : ScriptableObject
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
    }
}
