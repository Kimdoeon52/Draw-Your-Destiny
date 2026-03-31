namespace NYH.CoreCardSystem
{
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;
    using DG.Tweening;
    using UnityEngine.EventSystems;

    /// <summary>
    /// ?��면에 보이?�� 카드 UI�? ?��?��?��?��?��.
    /// 마우?�� ?��?���?, ?���?, ?���? 미리보기, 카드 ?��?�� ?��?��?�� ?��?��?��?��?��.
    /// </summary>
    public class CardView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI Text Objects")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text costText;

        [Header("UI Image Objects")]
        [SerializeField] private Image cardArtImage;
        [SerializeField] private Image cardBackgroundImage;

        [Header("Settings")]
        [SerializeField] private LayerMask dropLayer;
        [SerializeField] private float dragSpeed = 0.15f;
        [SerializeField] private float tiltStrength = 5f;

        public Card Card { get; private set; }
        public static bool AnyCardPickedUp = false;
        public bool IsHoverPreview { get; set; } = false;

        private Vector3 currentVelocity;
        private bool isDragging = false;
        private bool isPickedUp = false;
        private bool isTargetingMode = false;
        private Vector3 pointerDownMousePos;
        private float clickThreshold = 20f;
        private float targetingThresholdY;
        private Vector3 targetingCenterPos;
        private bool hasLoggedTargetingPreviewUpdate = false;

        private Camera mainCamera;
        private HandView cachedHandView;

        private void Awake()
        {
            mainCamera = Camera.main;
            cachedHandView = FindFirstObjectByType<HandView>();

            targetingThresholdY = Screen.height * 0.35f;
            targetingCenterPos = new Vector3(Screen.width * 0.5f, Screen.height * 0.2f, 0f);
        }

        private void Update()
        {
            if (isPickedUp || isDragging)
            {
                HandleFollowingMouse();
                if (Input.GetMouseButtonDown(1))
                 ReturnToHand();
                if (isPickedUp && isTargetingMode && Input.GetMouseButtonDown(0))
                TryPlayCard();
            }
        }

        public void Setup(Card card)
        {
            if (card == null) return;

            Card = card;
            if (titleText != null) titleText.text = card.Title;
            if (descriptionText != null) descriptionText.text = card.Description;
            if (costText != null) costText.text = card.Cost.ToString();
            if (cardArtImage != null && card.Image != null) cardArtImage.sprite = card.Image;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (isPickedUp || isDragging || AnyCardPickedUp) return;

            if (!IsHoverPreview)
            {
                transform.SetAsLastSibling();
            }

            CardViewHoverSystem.Instance?.Show(Card, transform.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (this == null) return;
            if (isPickedUp || isDragging || AnyCardPickedUp) return;

            CardViewHoverSystem.Instance?.Hide();

            if (!IsHoverPreview && cachedHandView != null)
            {
                StartCoroutine(cachedHandView.UpdateCardPositions(0.15f));
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (IsHoverPreview) return;
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (ActionSystem.Instance.IsPerforming) return;

            if (isPickedUp && !isTargetingMode)
            {
                TryPlayCard();
                return;
            }

            isDragging = true;
            AnyCardPickedUp = true;
            pointerDownMousePos = Input.mousePosition;
            transform.DOKill();
            transform.SetAsLastSibling();

            CardViewHoverSystem.Instance?.Hide();
            cachedHandView?.RemoveCard(Card);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (IsHoverPreview) return;
            if (eventData.button != PointerEventData.InputButton.Left || !isDragging) return;

            isDragging = false;
            if (Vector3.Distance(Input.mousePosition, pointerDownMousePos) > clickThreshold) TryPlayCard();
            else isPickedUp = true;
        }

        private void TryPlayCard()
        {
            if (GameManager.Instance.playerGold < Card.Cost)
            {
                Debug.Log("?��?��?��?��?�� 골드�? �?족하?�� 카드�? ?��?��?���? ?��?��?��?��?��.");
                ReturnToHand();
                return;
            }

            if (isTargetingMode)
            {
                var placementService = FindFirstObjectByType<BuildingPlacementService>();

                if (placementService == null || !placementService.IsPlacing)
                     return;
                if (CardSystem.Instance != null)
                {
                    Vector3Int tilePos = placementService.GetCurrentPreviewTilePos();
                    Debug.Log($"[CardView] ?���? ?��?��: {Card?.Title} -> {tilePos}");
                    if (CardSystem.Instance.TryQueuePlacementCard(Card, tilePos))
                    {
                        placementService.CancelPlacing();
                        isPickedUp = false;
                        isDragging = false;
                        isTargetingMode = false;
                        AnyCardPickedUp = false;
                        return;
                    }
                }

                Debug.Log("[CardView] 건물 ?��치에 ?��?��?��?�� 카드�? ?��?���? ?��?��립니?��.");
                ReturnToHand();
                return;
            }

            Vector3 mousePos = Input.mousePosition;
            mousePos.z = -mainCamera.transform.position.z;
            Vector2 worldPoint = mainCamera.ScreenToWorldPoint(mousePos);
            Collider2D hit = Physics2D.OverlapPoint(worldPoint, dropLayer);

            if (hit != null)
            {
                isPickedUp = false;
                isDragging = false;
                AnyCardPickedUp = false;
                ActionSystem.Instance.Perform(new PlayCardGA(Card));
            }
            else
            {
                ReturnToHand();
            }
        }

        private void HandleFollowingMouse()
        {
            Vector3 mousePos = Input.mousePosition;

            PlacementEffect placementEffect = null;
            if (Card?.Effects != null)
            {
                foreach (var effect in Card.Effects)
                {
                    if (effect is PlacementEffect pe)
                    {
                        placementEffect = pe;
                        break;
                    }
                }
            }

            if (placementEffect != null && (isDragging || isPickedUp))
            {
                if (mousePos.y > targetingThresholdY)
                {
                    if (!isTargetingMode) EnterTargetingMode(placementEffect);
                    UpdateTargeting();
                    return;
                }
                else if (isTargetingMode)
                {
                    ExitTargetingMode();
                }
            }

            mousePos.z = -mainCamera.transform.position.z;
            transform.position = Vector3.SmoothDamp(transform.position, mousePos, ref currentVelocity, dragSpeed);
            float horizontalVelocity = Mathf.Abs(currentVelocity.x) > 100f ? currentVelocity.x : 0f;
            float targetRotZ = Mathf.Clamp(-horizontalVelocity * tiltStrength * 0.01f, -20f, 20f);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 0, targetRotZ), Time.deltaTime * 10f);
        }

        private void EnterTargetingMode(PlacementEffect effect)
        {
            isTargetingMode = true;
            hasLoggedTargetingPreviewUpdate = false;
            transform.DOKill();
            transform.DOMove(targetingCenterPos, 0.3f).SetEase(Ease.OutBack);
            transform.DOScale(1.2f, 0.3f);
            transform.DORotate(Vector3.zero, 0.3f);

            var placementService = FindFirstObjectByType<BuildingPlacementService>();
            if (placementService != null && effect is InstallBuildingEffect installEffect && installEffect.buildingData != null)
            {
                Debug.Log($"[CardView] ???게팅 모드 진입: {Card?.Title}, 건물={installEffect.buildingData.buildingName}");
                placementService.StartPlacing(installEffect.buildingData);
            }
            else
            {
                Debug.LogWarning($"[CardView] ???게팅 모드 진입 ?��?��: service={(placementService != null)}, effect={effect?.GetType().Name}");
            }
        }

        private void ExitTargetingMode()
        {
            isTargetingMode = false;
            hasLoggedTargetingPreviewUpdate = false;
            transform.DOKill();
            transform.DOScale(1.0f, 0.2f);
            transform.DORotate(Vector3.zero, 0.2f);

            var placementService = FindFirstObjectByType<BuildingPlacementService>();
            placementService?.CancelPlacing();
        }

        private void UpdateTargeting()
        {
            transform.position = Vector3.SmoothDamp(transform.position, targetingCenterPos, ref currentVelocity, dragSpeed);

            var placementService = FindFirstObjectByType<BuildingPlacementService>();
            if (placementService != null)
            {
                Vector3Int tilePos = placementService.GetMouseTilePos();
                if (!hasLoggedTargetingPreviewUpdate)
                {
                    
                    hasLoggedTargetingPreviewUpdate = true;
                }
                placementService.UpdatePreview(tilePos);
            }
        }

        private void ReturnToHand()
        {
            CardViewHoverSystem.Instance?.Hide();

            if (isTargetingMode) ExitTargetingMode();
            isPickedUp = false;
            isDragging = false;
            AnyCardPickedUp = false;

            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;

            if (cachedHandView != null) StartCoroutine(cachedHandView.AddCard(this));
        }
    }
}
