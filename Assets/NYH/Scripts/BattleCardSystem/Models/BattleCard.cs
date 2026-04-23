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
        public BattleCardTargetingMode TargetingMode => Data != null ? Data.TargetingMode : BattleCardTargetingMode.Auto;
        public bool IgnoresDeckLimit => Data.IgnoresDeckLimit || Data.CardType == BattleCardType.Potion;
        public bool IsConsumable => Data.IsConsumable;
        public IReadOnlyList<UnitType> AllowedUserUnitTypes => Data != null ? Data.AllowedUserUnitTypes : null;
        public IReadOnlyList<BattleCardKeyword> Keywords => Data.Keywords;

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

            return FormatBattleDescription(
                NormalizeDescriptionText(builder.ToString()),
                Data.Keywords);
        }

        internal static string FormatBattleDescription(
            string description,
            IReadOnlyList<BattleCardKeyword> keywords)
        {
            string formattedDescription = BattleCardKeywordTextFormatter.ApplyKeywordColors(description, keywords);
            string keywordLine = BattleCardKeywordTextFormatter.FormatKeywordList(keywords);
            return string.IsNullOrEmpty(keywordLine)
                ? formattedDescription
                : $"{keywordLine}\n{formattedDescription}";
        }

        private static string NormalizeDescriptionText(string description)
        {
            if (string.IsNullOrEmpty(description))
            {
                return string.Empty;
            }

            return description.Replace("?꾩옱?띾룄", "현재속도");
        }
    }

    public static class BattleCardTargetingUtility
    {
        public static BattleCardTargetingMode ResolveTargetingMode(BattleCard card)
        {
            if (card == null)
            {
                return BattleCardTargetingMode.DirectEffect;
            }

            if (card.TargetingMode != BattleCardTargetingMode.Auto)
            {
                return card.TargetingMode;
            }

            // 기존 카드 자산을 전부 손대지 않아도 되도록
            // 이동/공격 관련 이펙트를 보고 하이브리드 모드를 자동 추론합니다.
            // 순서가 중요한 카드(예: 공격 후 이동)는 자산에서 수동 지정이 필요합니다.
            bool hasMove = card.CardType == BattleCardType.Move || HasEffect<BattleMoveEffect>(card);
            bool hasAttack = card.CardType == BattleCardType.Attack
                || BattleEffectResolver.GetAttackEffect(card) != null
                || HasEffect<BattleDamageEffect>(card)
                || HasEffect<BattleHealEffect>(card)
                || HasEffect<BattleStatusEffect>(card)
                || HasEffect<BattleStatModifierEffect>(card);

            if (hasMove && hasAttack)
            {
                return BattleCardTargetingMode.MoveThenAttack;
            }

            if (hasMove)
            {
                return BattleCardTargetingMode.MoveOnly;
            }

            if (hasAttack)
            {
                return BattleCardTargetingMode.AttackOnly;
            }

            return BattleCardTargetingMode.DirectEffect;
        }

        private static bool HasEffect<TEffect>(BattleCard card)
            where TEffect : BattleEffect
        {
            if (card?.RuntimeEffects == null)
            {
                return false;
            }

            foreach (var effect in card.RuntimeEffects)
            {
                if (effect is TEffect)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
