namespace NYH.CoreCardSystem
{
    using System.Collections.Generic;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Read-only list viewer for card piles such as draw pile and discard pile.
    /// Selection logic lives elsewhere on purpose.
    /// </summary>
    public class CardListUI : MonoBehaviour
    {
        public static CardListUI Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Transform container;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Button closeButton;

        /// <summary>
        /// Registers the singleton instance and hides the panel by default.
        /// </summary>
        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (panel != null)
            {
                panel.SetActive(false);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }
        }

        /// <summary>
        /// Opens the list and renders every supplied card in read-only mode.
        /// </summary>
        public void Show(List<Card> cards, string title)
        {
            if (panel == null || container == null)
            {
                return;
            }

            if (titleText != null)
            {
                titleText.text = title;
            }

            panel.SetActive(true);

            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }

            if (cards == null)
            {
                return;
            }

            foreach (Card card in cards)
            {
                if (CardViewCreator.Instance == null)
                {
                    continue;
                }

                CardView cardView = CardViewCreator.Instance.CreateCardView(card, container.position, Quaternion.identity);
                if (cardView == null)
                {
                    continue;
                }

                cardView.transform.SetParent(container, false);
                cardView.IsHoverPreview = true;
                cardView.UseBuiltInInteractions = false;
                cardView.transform.localScale = Vector3.one;
            }
        }

        /// <summary>
        /// Same as Show, but filters out duplicate CardIDs first.
        /// </summary>
        public void ShowDistinct(List<Card> cards, string title)
        {
            List<Card> distinctCards = new();
            HashSet<int> seenCardIds = new();

            if (cards != null)
            {
                foreach (Card card in cards)
                {
                    if (card == null || seenCardIds.Contains(card.CardID))
                    {
                        continue;
                    }

                    seenCardIds.Add(card.CardID);
                    distinctCards.Add(card);
                }
            }

            Show(distinctCards, title);
        }

        /// <summary>
        /// Hides the list and clears any active hover preview.
        /// </summary>
        public void Close()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }

            CardViewHoverSystem.Instance?.Hide();
        }
    }
}
