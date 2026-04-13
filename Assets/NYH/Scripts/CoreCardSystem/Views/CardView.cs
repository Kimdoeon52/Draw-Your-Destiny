namespace NYH.CoreCardSystem
{
    using System.Linq;
    using DG.Tweening;
    using TMPro;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    /// <summary>
    /// 화면에 보이는 카드를 띄우는 view 스크립트.
    /// 마우스를 올려두면 카드가 커지며 여러 기능이 있음.
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
        public bool UseBuiltInInteractions { get; set; } = true;

        private Vector3 currentVelocity;
        private bool isDragging;
        private bool isPickedUp;
        private bool isTargetingMode;
        public bool IsTargetingMode => isTargetingMode;
        private Vector3 pointerDownMousePos;
        private readonly float clickThreshold = 20f;
        private float targetingThresholdY;
        private Vector3 targetingCenterPos;
        private bool hasLoggedTargetingPreviewUpdate;
        private int originalHandIndex = -1;

        private Camera mainCamera;
        private HandView cachedHandView;
        private ICardViewPlayHandler customPlayHandler;

        private void Awake()
        {
            mainCamera = Camera.main;
            cachedHandView = FindFirstObjectByType<HandView>();
            customPlayHandler = GetComponents<MonoBehaviour>().OfType<ICardViewPlayHandler>().FirstOrDefault();

            targetingThresholdY = Screen.height * 0.35f;
            targetingCenterPos = new Vector3(Screen.width * 0.5f, Screen.height * 0.2f, 0f);
        }

        private void Update()
        {
            if (!UseBuiltInInteractions)
            {
                return;
            }

            if (isPickedUp || isDragging)
            {
                HandleFollowingMouse();
                if (Input.GetMouseButtonDown(1))
                {
                    ReturnToHand();
                }

                if (isPickedUp && isTargetingMode && Input.GetMouseButtonDown(0))
                {
                    TryPlayCard();
                }
            }
        }

        public void Setup(Card card)
        {
            if (card == null)
            {
                return;
            }

            Card = card;
            if (titleText != null) titleText.text = card.Title;
            if (descriptionText != null) descriptionText.text = card.Description;
            if (costText != null) costText.text = card.Cost.ToString();
            if (cardArtImage != null && card.Image != null) cardArtImage.sprite = card.Image;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (isPickedUp || isDragging || AnyCardPickedUp)
            {
                return;
            }

            if (!IsHoverPreview)
            {
                transform.SetAsLastSibling();
            }

            CardViewHoverSystem.Instance?.Show(Card, transform.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (this == null)
            {
                return;
            }

            if (isPickedUp || isDragging || AnyCardPickedUp)
            {
                return;
            }

            CardViewHoverSystem.Instance?.Hide();

            if (!IsHoverPreview && cachedHandView != null)
            {
                StartCoroutine(cachedHandView.UpdateCardPositions(0.15f));
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (IsHoverPreview) return;
            if (!UseBuiltInInteractions) return;
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (ActionSystem.Instance != null && ActionSystem.Instance.IsPerforming) return;

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
            if (cachedHandView != null)
            {
                originalHandIndex = cachedHandView.GetCardIndex(this);
                if (originalHandIndex < 0)
                {
                    cachedHandView.RebuildCardListFromChildren();
                    originalHandIndex = cachedHandView.GetCardIndex(this);
                }
            }
            cachedHandView?.RemoveCard(Card);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (IsHoverPreview) return;
            if (!UseBuiltInInteractions) return;
            if (eventData.button != PointerEventData.InputButton.Left || !isDragging) return;

            isDragging = false;
            if (Vector3.Distance(Input.mousePosition, pointerDownMousePos) > clickThreshold) TryPlayCard();
            else isPickedUp = true;
        }

        private void TryPlayCard()
        {
            if (customPlayHandler == null)
            {
                customPlayHandler = GetComponents<MonoBehaviour>().OfType<ICardViewPlayHandler>().FirstOrDefault();
            }

            bool wasDragged = Vector3.Distance(Input.mousePosition, pointerDownMousePos) > clickThreshold || isDragging || isPickedUp;
            if (customPlayHandler != null)
            {
                bool wasPlayed = customPlayHandler.TryPlayCard(this, Input.mousePosition, wasDragged);
                if (wasPlayed)
                {
                    isPickedUp = false;
                    isDragging = false;
                    isTargetingMode = false;
                    AnyCardPickedUp = false;
                    CardViewHoverSystem.Instance?.Hide();
                }
                else
                {
                    ReturnToHand();
                }

                return;
            }

            if (CardModifierSystem.IsTypeBlocked(Card._CardType))
            {
                Debug.Log($"{Card._CardType} 타입 카드는 지금 사용할 수 없습니다.");
                ReturnToHand();
                return;
            }

            if (ResourceManager.Instance.Gold < Card.Cost)
            {
                Debug.Log("골드가 부족하여 카드를 낼 수 없습니다.");
                ReturnToHand();
                return;
            }

            bool isBuildingCard = false;
            if (Card?.Effects != null)
            {
                foreach (var effect in Card.Effects)
                {
                    if (effect is InstallBuildingEffect)
                    {
                        isBuildingCard = true;
                        break;
                    }
                }
            }

            if (isBuildingCard && !isTargetingMode)
            {
                Debug.Log($"[CardView] {Card?.Title} 건물 카드는 가운데 배치 모드에서만 사용할 수 있습니다.");
                ReturnToHand();
                return;
            }

            if (isTargetingMode)
            {
                var placementService = FindFirstObjectByType<BuildingPlacementService>();

                if (placementService == null || !placementService.IsPlacing)
                {
                    return;
                }

                if (CardSystem.Instance != null)
                {
                    Vector3Int tilePos = placementService.GetCurrentPreviewTilePos();
                    Debug.Log($"[CardView] 카드 배치 시도: {Card?.Title} -> {tilePos}");
                    if (CardSystem.Instance.TryQueuePlacementCard(Card, tilePos, IsTargetingMode))
                    {
                        placementService.CancelPlacing();
                        isPickedUp = false;
                        isDragging = false;
                        isTargetingMode = false;
                        AnyCardPickedUp = false;
                        return;
                    }
                }

                Debug.Log("[CardView] 건물 위치에 이미 카드가 있습니다.");
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
                Debug.Log($"[CardView] 타게팅 모드 진입: {Card?.Title}, 건물={installEffect.buildingData.buildingName}");
                placementService.StartPlacing(installEffect.buildingData);
            }
            else
            {
                Debug.LogWarning($"[CardView] 타겟팅 모드 진입: service={(placementService != null)}, effect={effect?.GetType().Name}");
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
            //호버 확대 카드 숨김
            CardViewHoverSystem.Instance?.Hide();

            if (isTargetingMode) ExitTargetingMode();
            isPickedUp = false;
            isDragging = false;
            AnyCardPickedUp = false;

            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;

            if (cachedHandView != null)
            {
                if (originalHandIndex >= 0)
                {
                    StartCoroutine(cachedHandView.InsertCard(this, originalHandIndex));
                }
                else
                {
                    StartCoroutine(cachedHandView.AddCard(this));
                }

                originalHandIndex = -1;
            }
        }
    }
}
