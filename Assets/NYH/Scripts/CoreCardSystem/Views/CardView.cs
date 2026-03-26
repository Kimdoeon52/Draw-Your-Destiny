namespace NYH.CoreCardSystem
{
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;
    using DG.Tweening;
    using UnityEngine.EventSystems;

    /// <summary>
    /// 화면에 보이는 카드 한 장을 제어하는 클래스입니다.
    /// 마우스 드래그, 클릭, 기울기 효과 및 '카드 내기' 명령의 시작점 역할을 합니다.
    /// </summary>
    public class CardView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI Text Objects")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text costText; // [수정] manaText -> costText로 명칭 통일

        [Header("UI Image Objects")]
        [SerializeField] private Image cardArtImage;
        [SerializeField] private Image cardBackgroundImage;

        [Header("Settings")]
        [SerializeField] private LayerMask dropLayer;
        [SerializeField] private float dragSpeed = 0.15f;
        [SerializeField] private float tiltStrength = 5f;

        public Card Card { get; private set; }
        public static bool AnyCardPickedUp = false;
        public bool IsHoverPreview { get; set; } = false; // Hover 미리보기용 오브젝트인지 여부

        private Vector3 currentVelocity;
        private bool isDragging = false;
        private bool isPickedUp = false;
        private bool isTargetingMode = false; // [추가] 타겟팅 모드 여부
        private Vector3 pointerDownMousePos;
        private float clickThreshold = 20f;
        private float targetingThresholdY; // [추가] 타겟팅 모드 진입 높이
        private Vector3 targetingCenterPos; // [추가] 타겟팅 시 카드가 위치할 곳

        private Camera mainCamera;
        private HandView cachedHandView;

        private void Awake()
        {
            mainCamera = Camera.main;
            cachedHandView = FindFirstObjectByType<HandView>();
            
            // [추가] 화면 크기에 따른 기준점 설정
            targetingThresholdY = Screen.height * 0.35f;
            targetingCenterPos = new Vector3(Screen.width * 0.5f, Screen.height * 0.2f, 0f);
        }

        private void Update()
        {
            if (isPickedUp || isDragging)
            {
                HandleFollowingMouse();
                if (Input.GetMouseButtonDown(1)) ReturnToHand();
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

        /// <summary>
        /// [Hover 시스템 연결] 마우스를 카드 위에 올렸을 때 실행됩니다.
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            // 이미 다른 카드를 집어 들었거나 드래그 중이라면 무시
            if (isPickedUp || isDragging || AnyCardPickedUp) return;

            // [버그 수정] IsHoverPreview가 true(선택 창 등의 UI)인 경우 SetAsLastSibling을 호출하지 않습니다.
            // LayoutGroup이 적용된 곳에서 순서를 바꾸면 카드의 물리적 위치가 바뀌기 때문입니다.
            if (!IsHoverPreview)
            {
                transform.SetAsLastSibling();
            }

            // [핵심] CardViewHoverSystem에 현재 카드 정보를 전달하여 크게 보여주게 합니다.
            if (CardViewHoverSystem.Instance != null)
            {
                CardViewHoverSystem.Instance.Show(Card, transform.position);
            }
        }

        /// <summary>
        /// [Hover 시스템 연결] 마우스가 카드 밖으로 나갔을 때 실행됩니다.
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (this == null) return;
            
            // 드래그 중이거나 다른 카드를 집어든 상태라면 무시 (단, IsHoverPreview는 여기서 체크하지 않음)
            if (isPickedUp || isDragging || AnyCardPickedUp) return;

            // [핵심] 크게 보여주던 미리보기를 숨깁니다.
            if (CardViewHoverSystem.Instance != null)
            {
                CardViewHoverSystem.Instance.Hide();
            }

            // [수정] 일반 카드(손패)인 경우에만 손패 위치를 다시 정렬합니다.
            // IsHoverPreview가 true인 카드(선택창 등)는 손패 정렬을 건드리지 않습니다.
            if (!IsHoverPreview && cachedHandView != null)
            {
                StartCoroutine(cachedHandView.UpdateCardPositions(0.15f));
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // [추가] 미리보기/UI용 카드인 경우 모든 클릭/드래그 상호작용을 차단합니다.
            if (IsHoverPreview) return;

            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (ActionSystem.Instance.IsPerforming) return;

            if (isPickedUp) { TryPlayCard(); return; }

            isDragging = true;
            AnyCardPickedUp = true;
            pointerDownMousePos = Input.mousePosition;
            transform.DOKill();
            transform.SetAsLastSibling();

            // 카드를 집어 들면 Hover 미리보기를 즉시 숨깁니다.
            if (CardViewHoverSystem.Instance != null) CardViewHoverSystem.Instance.Hide();
            if (cachedHandView != null) cachedHandView.RemoveCard(Card);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // [추가] 미리보기/UI용 카드인 경우 상호작용 차단
            if (IsHoverPreview) return;

            if (eventData.button != PointerEventData.InputButton.Left || !isDragging) return;
            isDragging = false;
            if (Vector3.Distance(Input.mousePosition, pointerDownMousePos) > clickThreshold) TryPlayCard();
            else isPickedUp = true;
        }

        //카드 실행 
        private void TryPlayCard()
        {
            if(GameManager.Instance.playerGold < Card.Cost) //게임 매니저에서 플레이어 골드를 받아와서 골드가 부족하면 플레이 되지 않게 함
            {
                Debug.Log("플레이어의 골드가 부족하여 카드가 사용 되지 않았습니다.");
                ReturnToHand();
                return;
            }

            // [타겟팅 모드 처리]
            if (isTargetingMode)
            {
                var placementService = FindFirstObjectByType<BuildingPlacementService>();
                if (placementService != null)
                {
                    Vector3Int tilePos = placementService.GetMouseTilePos();
                    if (placementService.TryPlaceBuilding(tilePos))
                    {
                        // 설치 성공
                        isPickedUp = false;
                        isDragging = false;
                        AnyCardPickedUp = false;
                        isTargetingMode = false;

                        // [추가] 설치 성공 시에도 턴 종료 버튼 다시 활성화
                        // if (GameManager.Instance != null) GameManager.Instance.SetTurnEndButtonInteractable(true);

                        ActionSystem.Instance.Perform(new PlayCardGA(Card));
                        return;
                    }
                }
                
                // 설치 실패 시 혹은 서비스 없을 시 복귀
                ReturnToHand();
                return;
            }

            // [일반 모드 처리 - 위로 던지기]
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
            else ReturnToHand();
        }

        private void HandleFollowingMouse()
        {
            Vector3 mousePos = Input.mousePosition;

            // [추가] 배치 효과(PlacementEffect) 확인
            PlacementEffect placementEffect = null;
            if (Card?.Effects != null)
            {
                foreach (var effect in Card.Effects)
                {
                    if (effect is PlacementEffect pe) { placementEffect = pe; break; }
                }
            }

            // [타겟팅 모드 전환 로직]
            if (placementEffect != null && isDragging)
            if (placementEffect != null && (isDragging || isPickedUp))
            {
                if (mousePos.y > targetingThresholdY)
                {
                    if (!isTargetingMode) EnterTargetingMode(placementEffect);
                    UpdateTargeting(mousePos);
                    return;
                }
                else if (isTargetingMode)
                {
                    ExitTargetingMode();
                }
            }

            // 일반 드래그 로직
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

            // [추가] 카드 배치 중에는 턴 종료 버튼을 막음
            // if (GameManager.Instance != null) GameManager.Instance.SetTurnEndButtonInteractable(false);

            var placementService = FindFirstObjectByType<BuildingPlacementService>();
            if (placementService != null && effect is InstallBuildingEffect ibe)
            {
                placementService.StartPlacing(ibe.buildingData);
            }
        }

        private void ExitTargetingMode()
        {
            isTargetingMode = false;
            transform.DOKill();
            transform.DOScale(1.0f, 0.2f);
        transform.DORotate(Vector3.zero, 0.2f);

            // [주석 처리] 배치 모드 종료 시 턴 종료 버튼 다시 활성화
            // if (GameManager.Instance != null) GameManager.Instance.SetTurnEndButtonInteractable(true);

            var placementService = FindFirstObjectByType<BuildingPlacementService>();
            if (placementService != null)
            {
                placementService.CancelPlacing();
            }
        }

        private void UpdateTargeting(Vector3 mousePos)
        {
            // 카드는 중앙 고정 위치 유지
            transform.position = Vector3.SmoothDamp(transform.position, targetingCenterPos, ref currentVelocity, dragSpeed);
            
            // 건물 프리뷰 업데이트
            var placementService = FindFirstObjectByType<BuildingPlacementService>();
            if (placementService != null)
            {
                Vector3Int tilePos = placementService.GetMouseTilePos();
                placementService.UpdatePreview(tilePos);
            }
        }


        //카드를 다시 손패로 되돌리는 함수 (나중에 손패 위치 기억했다가 그 위치로 되돌아가게 만들어달라는 요청이 있음 ) 
        private void ReturnToHand() 
        {
            if (isTargetingMode) ExitTargetingMode();
            isPickedUp = false;
            isDragging = false;
            AnyCardPickedUp = false;

            // [추가] 손패로 돌아올 때 혹시 커져있거나 기울어져 있는 상태를 확실히 초기화
            transform.DOKill();
            transform.DOScale(Vector3.one, 0.2f);
            transform.DORotate(Vector3.zero, 0.2f);

            if (cachedHandView != null) StartCoroutine(cachedHandView.AddCard(this));
        }

    }
}
