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
        [SerializeField] private TMP_Text cardTypeText;
        [SerializeField] private TMP_Text cardUseTypeText;
        [SerializeField] private TMP_Text moveRangeText;

        [Header("UI Image Objects")]
        [SerializeField] private Image cardArtImage;
        [SerializeField] private Image gridImage;
        [SerializeField] private Image cardBackgroundImage;

        [Header("Settings")]
        [SerializeField] private LayerMask dropLayer;
        [SerializeField] private float dragSpeed = 0.15f;
        [SerializeField] private float tiltStrength = 5f;

        public Card Card { get; private set; }
        public static bool AnyCardPickedUp = false;
        public bool IsHoverPreview { get; set; } = false;
        public bool UseBuiltInInteractions { get; set; } = true;
        public bool AllowHoverPreview { get; set; } = true;

        private Vector3 currentVelocity;
        private bool isDragging;
        private bool isPickedUp;
        private bool isTargetingMode;
        public bool IsTargetingMode => isTargetingMode;
        private Vector3 pointerDownMousePos;
        private readonly float clickThreshold = 20f;
        private float targetingThresholdY;
        private Vector3 targetingCenterPos;
        private int originalHandIndex = -1;

        private Camera mainCamera;
        private HandView cachedHandView;
        private ICardViewPlayHandler customPlayHandler;
        private CardViewPresenter presenter;
        private TMP_Text mulliganMarkText;

        private void Awake()
        {
            mainCamera = Camera.main;
            cachedHandView = FindFirstObjectByType<HandView>();
            customPlayHandler = GetComponents<MonoBehaviour>().OfType<ICardViewPlayHandler>().FirstOrDefault();
            presenter = new CardViewPresenter(
                titleText,
                descriptionText,
                costText,
                cardTypeText,
                cardUseTypeText,
                moveRangeText,
                cardArtImage,
                gridImage);

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
            presenter ??= new CardViewPresenter(
                titleText,
                descriptionText,
                costText,
                cardTypeText,
                cardUseTypeText,
                moveRangeText,
                cardArtImage,
                gridImage);
            presenter.Apply(card);
        }

        public void SetMulliganMarked(bool isMarked)
        {
            TMP_Text mark = EnsureMulliganMark();
            if (mark != null)
            {
                mark.gameObject.SetActive(isMarked);
            }
        }

        public void RefreshPlayHandlerBinding()
        {
            customPlayHandler = GetComponents<MonoBehaviour>().OfType<ICardViewPlayHandler>().FirstOrDefault();
        }

        public void ClearPlayHandlerBinding()
        {
            customPlayHandler = null;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!AllowHoverPreview)
            {
                return;
            }

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
            if (!AllowHoverPreview)
            {
                return;
            }

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
            cachedHandView = ResolveHandView();
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

            bool wasDragged = Vector3.Distance(Input.mousePosition, pointerDownMousePos) > clickThreshold;
            if (wasDragged)
            {
                isDragging = false;
                TryPlayCard();
                return;
            }

            if (customPlayHandler != null)
            {
                isDragging = false;
                TryPlayCard();
                return;
            }

            isDragging = false;
            isPickedUp = true;
        }

        public void BeginExternalSelection()
        {
            CardViewHoverSystem.Instance?.Hide();
            UseBuiltInInteractions = false;
            transform.DOKill();
            transform.SetAsLastSibling();
            transform.DOMove(targetingCenterPos, 0.25f).SetEase(Ease.OutBack);
            transform.DOScale(1.15f, 0.25f);
            transform.DORotate(Vector3.zero, 0.2f);

            isDragging = false;
            isPickedUp = false;
            isTargetingMode = false;
            AnyCardPickedUp = false;
        }

        public void CancelExternalSelection()
        {
            UseBuiltInInteractions = true;
            ReturnToHand();
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
                    ReturnToHand();
                    return;
                }

                if (CardSystem.Instance != null)
                {
                    Vector3Int tilePos = placementService.GetCurrentPreviewTilePos();
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
            transform.DOKill();
            transform.DOMove(targetingCenterPos, 0.3f).SetEase(Ease.OutBack);
            transform.DOScale(1.2f, 0.3f);
            transform.DORotate(Vector3.zero, 0.3f);

            var placementService = FindFirstObjectByType<BuildingPlacementService>();
            if (placementService != null && effect is InstallBuildingEffect installEffect && installEffect.buildingData != null)
            {
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

            cachedHandView = ResolveHandView();
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

        private HandView ResolveHandView()
        {
            if (cachedHandView != null)
            {
                return cachedHandView;
            }

            HandView parentHandView = GetComponentInParent<HandView>();
            if (parentHandView != null)
            {
                return parentHandView;
            }

            return FindFirstObjectByType<HandView>();
        }

        private TMP_Text EnsureMulliganMark()
        {
            if (mulliganMarkText != null)
            {
                return mulliganMarkText;
            }

            Transform existing = transform.Find("MulliganMark");
            if (existing != null)
            {
                mulliganMarkText = existing.GetComponent<TMP_Text>();
                return mulliganMarkText;
            }

            GameObject markObject = new("MulliganMark", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            markObject.transform.SetParent(transform, false);

            RectTransform rectTransform = markObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            mulliganMarkText = markObject.GetComponent<TextMeshProUGUI>();
            mulliganMarkText.text = "X";
            mulliganMarkText.alignment = TextAlignmentOptions.Center;
            mulliganMarkText.fontSize = 180f;
            mulliganMarkText.color = new Color(0.88f, 0.12f, 0.12f, 0.85f);
            mulliganMarkText.raycastTarget = false;
            mulliganMarkText.gameObject.SetActive(false);
            return mulliganMarkText;
        }

    }
}
