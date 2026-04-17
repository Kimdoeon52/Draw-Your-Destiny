namespace NYH.BattleCardSystem
{
    using System.Reflection;
    using NYH.CoreCardSystem;
    using UnityEngine;

    /*
     * BattleCardViewAdapter
     *
     * 역할:
     * - 전투 카드를 기존 CoreCardSystem의 CardView/CardViewHoverSystem에서 재사용할 수 있게
     *   BattleCardData 또는 BattleCard를 임시 Card로 변환합니다.
     *
     * 사용하는 법:
     * - UI에서 전투 카드를 기존 카드 프리팹으로 보여주고 싶을 때 CreatePreviewCard()를 호출합니다.
     * - 실제 전투 로직을 문명 카드 시스템으로 실행하는 용도가 아니라, 표시/호버/설명 토큰 재사용용입니다.
     */
    public static class BattleCardViewAdapter
    {
        private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static Card CreatePreviewCard(BattleCardData battleCardData)
        {
            if (battleCardData == null)
            {
                return null;
            }

            return CreatePreviewCardInternal(
                battleCardData.CardID,
                battleCardData.CardName,
                ResolvePreviewCardType(battleCardData.CardType),
                battleCardData.Image,
                battleCardData.ActionPointCost,
                battleCardData.Description,
                battleCardData.Effects,
                CardPresentationTextFormatter.CreateForBattle(battleCardData));
        }

        public static Card CreatePreviewCard(BattleCard battleCard)
        {
            if (battleCard == null)
            {
                return null;
            }

            return CreatePreviewCardInternal(
                battleCard.CardID,
                battleCard.Title,
                ResolvePreviewCardType(battleCard.CardType),
                battleCard.Image,
                battleCard.CurrentCost,
                battleCard.Data != null ? battleCard.Data.Description : string.Empty,
                battleCard.Data != null ? battleCard.Data.Effects : null,
                CardPresentationTextFormatter.CreateForBattle(battleCard.Data));
        }

        private static Card CreatePreviewCardInternal(
            int cardId,
            string cardName,
            CardType cardType,
            Sprite image,
            int cost,
            string description,
            System.Collections.Generic.List<Effect> effects,
            CardPresentationData presentationData)
        {
            CardData previewData = ScriptableObject.CreateInstance<CardData>();
            previewData.hideFlags = HideFlags.HideAndDontSave;

            SetField(previewData, "<cardID>k__BackingField", cardId);
            SetField(previewData, "cardName", cardName);
            SetField(previewData, "<cardType>k__BackingField", cardType);
            SetField(previewData, "<Image>k__BackingField", image);
            SetField(previewData, "<Cost>k__BackingField", cost);
            SetField(previewData, "description", description);
            SetField(previewData, "<Effects>k__BackingField", effects ?? new System.Collections.Generic.List<Effect>());

            Card previewCard = new Card(previewData)
            {
                Cost = cost,
            };

            previewCard.PresentationData = presentationData ?? new CardPresentationData { VisualKind = CardVisualKind.Battle };

            return previewCard;
        }

        private static CardType ResolvePreviewCardType(BattleCardType battleCardType)
        {
            switch (battleCardType)
            {
                case BattleCardType.Attack:
                    return CardType.Fight;
                case BattleCardType.Move:
                    return CardType.Normal;
                case BattleCardType.Skill:
                    return CardType.Normal;
                case BattleCardType.Potion:
                    return CardType.Common;
                default:
                    return CardType.Common;
            }
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo fieldInfo = target.GetType().GetField(fieldName, FieldFlags);
            if (fieldInfo != null)
            {
                fieldInfo.SetValue(target, value);
            }
        }
    }
}
