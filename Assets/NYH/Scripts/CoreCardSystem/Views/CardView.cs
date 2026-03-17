namespace NYH.CoreCardSystem
{
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI; // UI Image 사용을 위해 추가
    using UnityEngine.EventSystems;

    public class CardView: MonoBehaviour
    {
        [Header("UI Text Objects")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text manaText;

        [Header("UI Image Objects")]
        [SerializeField] private Image cardArtImage; // 카드 일러스트 이미지
        [SerializeField] private Image cardBackgroundImage; // 카드 배경 이미지

        [Header("Other Settings")]
        [SerializeField] private GameObject wrapper;
        [SerializeField] private LayerMask dropLayer;

        public Card Card { get; private set; }
        private Vector3 dragStartPosition;
        private Quaternion dragStartRotation;
        private Vector3 offset;
        private bool isDragging = false;
        private Camera mainCamera;

        private void Awake()
        {
            mainCamera = Camera.main;
            // UI 클릭 인식을 위해 카메라에 Physics2DRaycaster가 없으면 추가
            if (mainCamera != null && mainCamera.GetComponent<Physics2DRaycaster>() == null)
            {
                mainCamera.gameObject.AddComponent<Physics2DRaycaster>();
            }
        }

        public void Setup(Card card)
        {
            if (card == null) return;
            Card = card;
            
            if (titleText != null) titleText.text = card.Title;
            if (descriptionText != null) descriptionText.text = card.Description;
            if (manaText != null) manaText.text = card.Mana.ToString();
            
            if (cardArtImage != null) cardArtImage.sprite = card.Image;
        }

        private void OnMouseDown()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            
            isDragging = true;
            dragStartPosition = transform.position;
            dragStartRotation = transform.rotation;

            Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            offset = transform.position - mousePos;
            
            transform.rotation = Quaternion.identity;
            transform.position = new Vector3(transform.position.x, transform.position.y, -1f);
        }

        private void OnMouseDrag()
        {
            if (!isDragging) return;

            Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            transform.position = mousePos + offset + Vector3.forward * -1f;
        }

        private void OnMouseUp()
        {
            if (!isDragging) return;
            isDragging = false;

            Collider2D hit = Physics2D.OverlapPoint(transform.position, dropLayer);
            
            if (hit != null)
            {
                PlayCardGA playCardGA = new(Card);
                ActionSystem.Instance.Perform(playCardGA);
            }
            else
            {
                transform.position = dragStartPosition;
                transform.rotation = dragStartRotation;
            }
        }
    }
}