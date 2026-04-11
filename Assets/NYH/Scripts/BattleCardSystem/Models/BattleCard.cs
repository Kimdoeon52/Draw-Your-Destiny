namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using System.Text;
    using UnityEngine;

    public class BattleCard
    {
        public BattleCardData Data { get; }
        public int CurrentCost { get; set; }

        public int CardID => Data.CardID;
        public string Title => Data.CardName;
        public string Description => BuildDescription();
        public Sprite Image => Data.Image;
        public BattleCardType CardType => Data.CardType;
        public bool IgnoresDeckLimit => Data.IgnoresDeckLimit || Data.CardType == BattleCardType.Potion;
        public bool IsConsumable => Data.IsConsumable;
        public IReadOnlyList<BattleCardKeyword> Keywords => Data.Keywords;
        public int MoveAmount => Data.MoveAmount;
        public int AttackDamage => Data.AttackDamage;
        public int AttackRange => Data.AttackRange;
        public int AttackTargetCount => Data.AttackTargetCount;
        public bool HitsAllTargetsInRange => Data.HitsAllTargetsInRange;
        public BattleAttackPattern AttackPattern => Data.AttackPattern;

        public BattleCard(BattleCardData data)
        {
            Data = data;
            CurrentCost = data != null ? data.ActionPointCost : 0;
        }

        private string BuildDescription()
        {
            if (Data == null || string.IsNullOrEmpty(Data.Description))
            {
                return string.Empty;
            }

            StringBuilder builder = new(Data.Description);
            builder.Replace("{cost}", CurrentCost.ToString());
            return builder.ToString();
        }
    }
}
