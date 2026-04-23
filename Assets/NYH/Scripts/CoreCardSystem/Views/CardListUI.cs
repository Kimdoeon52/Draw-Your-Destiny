namespace NYH.CoreCardSystem
{
    using System.Collections.Generic;
    using System.Linq;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 덱이나 무덤처럼 많은 양의 카드를 스크롤하며 보여주는 UI 클래스입니다.
    /// </summary>
    public class CardListUI : MonoBehaviour
    {
        public static CardListUI Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Transform container; // ScrollView의 Content (GridLayoutGroup이 있어야 함)
        [SerializeField] private TMP_Text titleText;  // "덱 확인" 또는 "무덤 확인" 표시용
        [SerializeField] private Button closeButton;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            
            if (panel != null) panel.SetActive(false);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        /// <summary>
        /// 카드를 화면에 나열합니다.
        /// </summary>
        /// <param name="cards">보여줄 카드 리스트</param>
        /// <param name="title">UI 상단에 표시될 제목</param>
        public void Show(List<Card> cards, string title)
        {
            if (panel == null || container == null) return;

            if (titleText != null) titleText.text = title;
            panel.SetActive(true);

            // 1. 기존 카드 제거
            foreach (Transform child in container) Destroy(child.gameObject);

            // 2. 카드 생성 및 배치
            foreach (var card in cards)
            {
                // CardViewCreator를 사용하여 카드 생성
                CardView cardView = CardViewCreator.Instance.CreateCardView(card, container.position, Quaternion.identity);
                cardView.transform.SetParent(container, false);

                // [중요] '보기 전용' 설정: 클릭/드래그가 안 되도록 함
                cardView.IsHoverPreview = true;
                
                // UI 레이아웃에 맞게 스케일 조정 (필요 시)
                cardView.transform.localScale = Vector3.one;
            }
        }

        public void ShowDistinct(List<Card> cards, string title)
        {
            // 중복 제거된 카드 리스트 생성
            List<Card> distinctCards = new List<Card>();
            HashSet<int> seenCardIds = new HashSet<int>();
            foreach (var card in cards)
            {
                if (!seenCardIds.Contains(card.CardID))
                {
                    seenCardIds.Add(card.CardID);
                    distinctCards.Add(card);
                }
            }
            Show(distinctCards, title);
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
            
            // 호버 미리보기가 켜져 있을 수 있으므로 숨김 처리
            if (CardViewHoverSystem.Instance != null) CardViewHoverSystem.Instance.Hide();
        }
    }
}
