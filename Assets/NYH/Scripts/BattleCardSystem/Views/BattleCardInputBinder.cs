namespace NYH.BattleCardSystem
{
    using DG.Tweening;
    using NYH.CoreCardSystem;
    using UnityEngine;
    using UnityEngine.EventSystems;

    /*
     * BattleCardInputBinder
     *
     * 역할:
     * - 공용 CardView를 전투 씬에서 재사용할 때 문명 카드 로직 대신 전투 전용 입력을 처리합니다.
     * - 호버는 CardView에 맡기고, 클릭/드래그는 이 바인더에서 직접 처리합니다.
     */
    [RequireComponent(typeof(CardView))]
    public class BattleCardInputBinder : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private const float DragThreshold = 20f;

        private CardView cardView;
        private BattleCard boundBattleCard;
        private BattleUIController owner;
        private HandView handView;
        private RectTransform rectTransform;
        private Vector2 pointerDownPosition;
        private bool isDragging;

        private void Awake()
        {
            cardView = GetComponent<CardView>();
            handView = FindFirstObjectByType<HandView>();
            rectTransform = transform as RectTransform;
        }

        public void Bind(BattleCard battleCard, BattleUIController battleUIController)
        {
            boundBattleCard = battleCard;
            owner = battleUIController;

            if (cardView != null)
            {
                cardView.UseBuiltInInteractions = false;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || rectTransform == null)
            {
                return;
            }

            pointerDownPosition = eventData.position;
            isDragging = false;
            CardView.AnyCardPickedUp = true;

            rectTransform.DOKill();
            rectTransform.SetAsLastSibling();
            CardViewHoverSystem.Instance?.Hide();
            if (cardView != null && handView != null)
            {
                handView.RemoveCard(cardView.Card);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || rectTransform == null)
            {
                return;
            }

            if (!isDragging && Vector2.Distance(pointerDownPosition, eventData.position) >= DragThreshold)
            {
                isDragging = true;
            }

            if (!isDragging)
            {
                return;
            }

            rectTransform.position = eventData.position;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || rectTransform == null)
            {
                return;
            }

            bool wasDragged = isDragging;
            bool wasPlayed = owner != null && owner.HandleBattleCardReleased(boundBattleCard, eventData.position, wasDragged);

            isDragging = false;
            CardView.AnyCardPickedUp = false;
            if (!wasPlayed)
            {
                ReturnToHandLayout();
            }
            else
            {
                CardViewHoverSystem.Instance?.Hide();
            }
        }

        private void ReturnToHandLayout()
        {
            if (handView == null)
            {
                return;
            }

            CardViewHoverSystem.Instance?.Hide();
            if (cardView != null)
            {
                cardView.transform.localScale = Vector3.one;
                cardView.transform.rotation = Quaternion.identity;
                StartCoroutine(handView.AddCard(cardView));
                return;
            }

            StartCoroutine(handView.UpdateCardPositions(0.15f));
        }
    }
}
