namespace NYH.CoreCardSystem
{
    using UnityEngine;

    public class CardViewCreator : Singleton<CardViewCreator>
    {
        [SerializeField] private CardView civilizationCardViewPrefab;
        [SerializeField] private CardView battleCardViewPrefab;
        [SerializeField] private CardView cardViewPrefab;

        public CardView CreateCardView(Card card, Vector3 position, Quaternion rotation)
        {
            CardView prefab = ResolvePrefab(card);
            if (prefab == null)
            {
                string title = card != null ? card.Title : "null";
                string visualKind = card?.PresentationData != null
                    ? card.PresentationData.VisualKind.ToString()
                    : "Unknown";
                Debug.LogWarning($"[CardViewCreator] 사용할 카드 프리팹이 없습니다. title={title}, visualKind={visualKind}");
                return null;
            }

            CardView cardView = Instantiate(prefab, transform);
            cardView.transform.localScale = Vector3.one;
            cardView.transform.position = position;
            cardView.transform.rotation = rotation;
            cardView.Setup(card);
            return cardView;
        }

        private CardView ResolvePrefab(Card card)
        {
            CardVisualKind visualKind = card?.PresentationData != null
                ? card.PresentationData.VisualKind
                : CardVisualKind.Civilization;

            return visualKind switch
            {
                CardVisualKind.Battle => battleCardViewPrefab != null ? battleCardViewPrefab : cardViewPrefab,
                _ => civilizationCardViewPrefab != null ? civilizationCardViewPrefab : cardViewPrefab,
            };
        }
    }
}

