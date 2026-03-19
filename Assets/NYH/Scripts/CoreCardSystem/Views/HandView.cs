namespace NYH.CoreCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Splines;
    using System.Collections;
    using DG.Tweening;
    using System.Linq;

    public class HandView : MonoBehaviour
    {
        [SerializeField] private SplineContainer splineContainer;
        private readonly List<CardView> cards = new();

        public IEnumerator AddCard(CardView cardView)
        {
            // 부모를 확실하게 자신으로 설정
            cardView.transform.SetParent(this.transform);

            if (!cards.Contains(cardView))
            {
                cards.Add(cardView);
            }
            
            yield return UpdateCardPositions(0.15f);
        }

        public CardView RemoveCard(Card card)
        {
            CardView cardView = GetCardView(card);
            if (cardView == null) return null;
            
            cards.Remove(cardView);
            StartCoroutine(UpdateCardPositions(0.15f));
            
            return cardView;
        }

        private CardView GetCardView(Card card)
        {
            return cards.Where(cv => cv != null && cv.Card == card).FirstOrDefault();
        }

        public IEnumerator UpdateCardPositions(float duration)
        {
            if (cards.Count == 0) yield break;
            
            cards.RemoveAll(cv => cv == null);

            float cardSpacing = 1f / 10f;
            float firstCardPosition = 0.5f - (cards.Count - 1) * cardSpacing / 2f;
            
            if (splineContainer == null) yield break;
            Spline spline = splineContainer.Spline;

            for (int i = 0; i < cards.Count; i++)
            {
                float p = firstCardPosition + i * cardSpacing;
                
                // ✅ 로컬 좌표로 계산
                Vector3 splinePosition = (Vector3)spline.EvaluatePosition(p);
                Vector3 forward = (Vector3)spline.EvaluateTangent(p);
                Vector3 up = (Vector3)spline.EvaluateUpVector(p);
                Quaternion rotation = Quaternion.LookRotation(-up, Vector3.Cross(-up, forward).normalized);
                
                cards[i].transform.DOKill();
                
                // ✅ DOLocalMove를 써야 UI 좌표가 정확하게 맞습니다.
                cards[i].transform.DOLocalMove(splinePosition, duration);
                cards[i].transform.DORotate(rotation.eulerAngles, duration);
            }
            yield return new WaitForSeconds(duration);
        }
    }
}
