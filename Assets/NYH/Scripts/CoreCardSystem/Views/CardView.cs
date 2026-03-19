namespace NYH.CoreCardSystem
{
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.EventSystems;

    public class CardView : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI Text Objects")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text manaText;

        [Header("UI Image Objects")]
        [SerializeField] private Image cardArtImage;
        [SerializeField] private Image cardBackgroundImage;

        [Header("Other Settings")]
        [SerializeField] private GameObject wrapper;
        [SerializeField] private LayerMask dropLayer;

        public Card Card { get; private set; }
        private Vector3 dragStartPosition;
        private Quaternion dragStartRotation;
        private bool isDragging = false;
        private Camera mainCamera;
        private HandView cachedHandView;

        private void Awake()
        {
            mainCamera = Camera.main;
            cachedHandView = FindFirstObjectByType<HandView>();
        }

        public void Setup(Card card)
        {
            if (card == null) return;
            Card = card;
            if (titleText != null) titleText.text = card.Title;
            if (descriptionText != null) descriptionText.text = card.Description;
            if (manaText != null) manaText.text = card.Mana.ToString();
            if (cardArtImage != null && card.Image != null) cardArtImage.sprite = card.Image;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (isDragging) return;
            transform.SetAsLastSibling();
            if (CardViewHoverSystem.Instance != null) CardViewHoverSystem.Instance.Show(Card, transform.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (isDragging) return;
            if (CardViewHoverSystem.Instance != null) CardViewHoverSystem.Instance.Hide();
            if (cachedHandView != null) StartCoroutine(cachedHandView.UpdateCardPositions(0.15f));
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isDragging = true;
            dragStartPosition = transform.position;
            dragStartRotation = transform.rotation;
            
            transform.rotation = Quaternion.identity; 
            transform.SetAsLastSibling();

            if (cachedHandView != null) cachedHandView.RemoveCard(Card);
            if (CardViewHoverSystem.Instance != null) CardViewHoverSystem.Instance.Hide();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging) return;
            transform.position = eventData.position;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!isDragging) return;
            isDragging = false;

            Vector3 mousePos = eventData.position;
            mousePos.z = -mainCamera.transform.position.z; 
            Vector2 worldPoint = mainCamera.ScreenToWorldPoint(mousePos);

            Collider2D hit = Physics2D.OverlapPoint(worldPoint, dropLayer);
            
            if (hit != null)
            {
                ActionSystem.Instance.Perform(new PlayCardGA(Card));
            }
            else
            {
                if (cachedHandView != null) StartCoroutine(cachedHandView.AddCard(this));
                else
                {
                    transform.position = dragStartPosition;
                    transform.rotation = dragStartRotation;
                }
            }
        }
    }
}
