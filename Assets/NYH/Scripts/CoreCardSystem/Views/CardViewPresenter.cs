namespace NYH.CoreCardSystem
{
    using TMPro;
    using UnityEngine.UI;

    /// <summary>
    /// CardView의 텍스트와 이미지만 갱신합니다.
    /// 드래그, 클릭, 카드 사용, 배치 타겟팅 같은 입력 흐름은 담당하지 않습니다.
    /// </summary>
    internal sealed class CardViewPresenter
    {
        private readonly TMP_Text titleText;
        private readonly TMP_Text descriptionText;
        private readonly TMP_Text costText;
        private readonly TMP_Text cardTypeText;
        private readonly TMP_Text cardUseTypeText;
        private readonly TMP_Text moveRangeText;
        private readonly Image cardArtImage;
        private readonly Image gridImage;

        public CardViewPresenter(
            TMP_Text titleText,
            TMP_Text descriptionText,
            TMP_Text costText,
            TMP_Text cardTypeText,
            TMP_Text cardUseTypeText,
            TMP_Text moveRangeText,
            Image cardArtImage,
            Image gridImage)
        {
            this.titleText = titleText;
            this.descriptionText = descriptionText;
            this.costText = costText;
            this.cardTypeText = cardTypeText;
            this.cardUseTypeText = cardUseTypeText;
            this.moveRangeText = moveRangeText;
            this.cardArtImage = cardArtImage;
            this.gridImage = gridImage;
        }

        public void Apply(Card card)
        {
            if (card == null)
            {
                return;
            }

            if (titleText != null)
            {
                titleText.text = card.Title;
            }

            if (descriptionText != null)
            {
                descriptionText.text = card.Description;
            }

            if (costText != null)
            {
                costText.text = card.Cost.ToString();
            }

            if (cardArtImage != null && card.Image != null)
            {
                cardArtImage.sprite = card.Image;
            }

            ApplyPresentationData(card.PresentationData);
        }

        private void ApplyPresentationData(CardPresentationData presentationData)
        {
            CardPresentationData resolvedData = presentationData ?? new CardPresentationData();
            bool isBattleCard = resolvedData.VisualKind == CardVisualKind.Battle;

            SetOptionalText(cardTypeText, resolvedData.CardTypeText, !isBattleCard);
            SetOptionalText(cardUseTypeText, resolvedData.CardUseTypeText, !isBattleCard);
            SetOptionalText(moveRangeText, resolvedData.MoveRangeText, isBattleCard);
            SetOptionalImage(gridImage, resolvedData.GridImage, isBattleCard);
        }

        private static void SetOptionalText(TMP_Text target, string value, bool shouldShow)
        {
            if (target == null)
            {
                return;
            }

            target.text = string.IsNullOrWhiteSpace(value) ? "-" : value;
            target.gameObject.SetActive(shouldShow);
        }

        private static void SetOptionalImage(Image target, UnityEngine.Sprite sprite, bool shouldShow)
        {
            if (target == null)
            {
                return;
            }

            target.sprite = sprite;
            target.gameObject.SetActive(shouldShow && sprite != null);
        }
    }
}
