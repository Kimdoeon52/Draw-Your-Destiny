namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using System.Reflection;
    using NYH.CoreCardSystem;
    using UnityEngine;

    /// <summary>
    /// 전투 카드 시스템의 BattleCard / BattleCardData를
    /// 기존 CoreCardSystem의 CardView에서 표시할 수 있는 Preview Card로 바꿔 주는 어댑터입니다.
    ///
    /// 왜 필요한가:
    /// - 전투 카드는 BattleCardData/BattleCard를 사용합니다.
    /// - 기존 카드 UI는 Card / CardData를 기준으로 동작합니다.
    /// - 둘을 직접 섞으면 UI 쪽을 크게 바꿔야 하므로,
    ///   "표시 전용 임시 Card"를 만들어 기존 UI를 그대로 재사용합니다.
    ///
    /// 이 클래스가 담당하는 것:
    /// - 카드명, 비용, 설명, 이미지, 효과 목록을 CardData 형태로 복사
    /// - 전투 카드 타입을 Preview용 CoreCardSystem CardType으로 변환
    /// - CardView가 필요로 하는 PresentationData를 battle 시각 규칙으로 구성
    ///
    /// 이 클래스가 담당하지 않는 것:
    /// - 실제 전투 카드 로직 실행
    /// - 덱/손패/무덤 변경
    /// - 카드 사용 가능 여부 판단
    /// </summary>
    public static class BattleCardViewAdapter
    {
        private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        /// <summary>
        /// BattleCardData를 바로 넘겼을 때도 최종적으로는 런타임 BattleCard를 만든 뒤
        /// 그 BattleCard를 Preview Card로 바꾸게 합니다.
        /// 이렇게 해야 덱 보기 Show, 보상 미리보기, 교체 UI가 모두 같은 표시 경로를 타게 됩니다.
        /// </summary>
        public static Card CreatePreviewCard(BattleCardData battleCardData)
        {
            if (battleCardData == null)
            {
                return null;
            }

            return CreatePreviewCard(new BattleCard(battleCardData));
        }

        /// <summary>
        /// 런타임 BattleCard를 CoreCardSystem용 Preview Card로 변환합니다.
        /// 카드 설명은 BattleCard가 이미 현재 비용/키워드 기준으로 가공한 값을 사용합니다.
        /// </summary>
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
                battleCard.Description,
                battleCard.Data != null ? battleCard.Data.Effects : null,
                CardPresentationTextFormatter.CreateForBattle(battleCard.Data));
        }

        /// <summary>
        /// CardView가 읽을 임시 CardData / Card 객체를 생성합니다.
        /// ScriptableObject.CreateInstance로 메모리상 임시 자산을 만들고,
        /// 필요한 필드를 reflection으로 채워 Preview Card를 구성합니다.
        /// </summary>
        private static Card CreatePreviewCardInternal(
            int cardId,
            string cardName,
            CardType cardType,
            Sprite image,
            int cost,
            string description,
            List<Effect> effects,
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
            SetField(previewData, "<Effects>k__BackingField", effects ?? new List<Effect>());

            Card previewCard = new Card(previewData)
            {
                Cost = cost,
            };

            previewCard.PresentationData = presentationData ?? new CardPresentationData
            {
                VisualKind = CardVisualKind.Battle,
            };

            return previewCard;
        }

        /// <summary>
        /// 전투 카드 타입을 Preview용 CoreCardSystem CardType으로 옮깁니다.
        /// 이 값은 주로 기존 카드 UI가 타입별 표시를 결정할 때 사용됩니다.
        /// </summary>
        private static CardType ResolvePreviewCardType(BattleCardType battleCardType)
        {
            switch (battleCardType)
            {
                case BattleCardType.Attack:
                    return CardType.Fight;
                case BattleCardType.Move:
                case BattleCardType.Skill:
                    return CardType.Normal;
                case BattleCardType.Potion:
                default:
                    return CardType.Common;
            }
        }

        /// <summary>
        /// CardData의 비공개 필드를 reflection으로 채웁니다.
        /// Preview Card는 표시 전용 임시 데이터이므로, 실제 자산을 수정하지 않습니다.
        /// </summary>
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
