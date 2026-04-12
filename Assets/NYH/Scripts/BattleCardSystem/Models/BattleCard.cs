namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using System.Text;
    using UnityEngine;
    using NYH.CoreCardSystem;

    public class BattleCard : IRuntimeCard
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
        public AttackPatternData CustomAttackPattern => Data.CustomAttackPattern;

        public int RuntimeCardID => CardID;
        public string RuntimeTitle => Title;
        public string RuntimeDescription => Description;
        public Sprite RuntimeImage => Image;
        public int RuntimeCost => CurrentCost;
        public IReadOnlyList<Effect> RuntimeEffects => Data != null ? Data.Effects : null;

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

            if (Data.Effects != null)
            {
                foreach (var effect in Data.Effects)
                {
                    if (effect == null)
                    {
                        continue;
                    }

                    var tokens = effect.GetDescriptionTokens((Card)null);
                    if (tokens == null)
                    {
                        continue;
                    }

                    foreach (var pair in tokens)
                    {
                        builder.Replace($"{{{pair.Key}}}", pair.Value);
                    }
                }
            }

            return builder.ToString();
        }
    }
}
