namespace NYH.CoreCardSystem
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using DG.Tweening;
    using UnityEngine;
    using UnityEngine.Splines;

    public class HandView : MonoBehaviour
    {
        [SerializeField] private SplineContainer splineContainer;
        private readonly List<CardView> cards = new();

        public IReadOnlyList<CardView> Cards => cards;

        public IEnumerator AddCard(CardView cardView)
        {
            cardView.transform.SetParent(this.transform, false);

            if (!cards.Contains(cardView))
            {
                cards.Add(cardView);
            }

            yield return UpdateCardPositions(0.15f);
        }

        public void AddCardImmediate(CardView cardView)
        {
            cardView.transform.SetParent(this.transform, false);

            if (!cards.Contains(cardView))
            {
                cards.Add(cardView);
            }
        }

        public IEnumerator InsertCard(CardView cardView, int index)
        {
            cardView.transform.SetParent(this.transform, false);

            if (cards.Contains(cardView))
            {
                cards.Remove(cardView);
            }

            index = Mathf.Clamp(index, 0, cards.Count);
            cards.Insert(index, cardView);

            yield return UpdateCardPositions(0.15f);
        }

        public CardView RemoveCard(Card card)
        {
            CardView cardView = GetCardView(card);
            if (cardView == null)
            {
                return null;
            }

            cards.Remove(cardView);
            return cardView;
        }

        public int GetCardIndex(CardView cardView)
        {
            return cards.IndexOf(cardView);
        }

        public void RebuildCardListFromChildren()
        {
            cards.Clear();

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                CardView cardView = child.GetComponent<CardView>();
                if (cardView == null || cardView.IsHoverPreview)
                {
                    continue;
                }

                cards.Add(cardView);
            }

        }

        private CardView GetCardView(Card card)
        {
            return cards.Where(cv => cv != null && cv.Card == card).FirstOrDefault();
        }

        public IEnumerator UpdateCardPositions(float duration)
        {
            if (cards.Count == 0)
            {
                yield break;
            }

            cards.RemoveAll(cv => cv == null);

            if (!TryGetSpline(out Spline spline))
            {
                yield return LayoutCardsInStraightLine(duration);
                yield break;
            }

            float cardSpacing = 1f / 10f;
            float firstCardPosition = 0.5f - (cards.Count - 1) * cardSpacing / 2f;

            for (int i = 0; i < cards.Count; i++)
            {
                float p = firstCardPosition + i * cardSpacing;

                Vector3 splinePosition = (Vector3)spline.EvaluatePosition(p);
                Vector3 forward = (Vector3)spline.EvaluateTangent(p);
                Vector3 up = (Vector3)spline.EvaluateUpVector(p);
                Quaternion rotation = Quaternion.LookRotation(-up, Vector3.Cross(-up, forward).normalized);

                cards[i].transform.DOKill();
                cards[i].transform.SetSiblingIndex(i);
                cards[i].transform.DOLocalMove(splinePosition, duration);
                cards[i].transform.DORotate(rotation.eulerAngles, duration);
                cards[i].transform.DOScale(Vector3.one, duration);
            }

            yield return new WaitForSeconds(duration);
        }

        private bool TryGetSpline(out Spline spline)
        {
            spline = splineContainer != null ? splineContainer.Spline : null;
            return spline != null && spline.Count > 0;
        }

        private IEnumerator LayoutCardsInStraightLine(float duration)
        {
            float spacing = 180f;
            float startX = -((cards.Count - 1) * spacing) * 0.5f;

            for (int i = 0; i < cards.Count; i++)
            {
                Vector3 targetPosition = new(startX + (i * spacing), 0f, 0f);

                cards[i].transform.DOKill();
                cards[i].transform.SetSiblingIndex(i);
                cards[i].transform.DOLocalMove(targetPosition, duration);
                cards[i].transform.DORotate(Vector3.zero, duration);
                cards[i].transform.DOScale(Vector3.one, duration);
            }

            Debug.LogWarning("[HandView] splineContainer가 비어 있거나 Knot가 없어 직선 배치 대체를 사용합니다.");
            yield return new WaitForSeconds(duration);
        }

        public IEnumerator LayoutCardsInCenter(float duration, float centerY = 220f, float spacing = 190f)
        {
            cards.RemoveAll(cv => cv == null);
            if (cards.Count == 0)
            {
                yield break;
            }

            float startX = -((cards.Count - 1) * spacing) * 0.5f;
            for (int i = 0; i < cards.Count; i++)
            {
                Vector3 targetPosition = new(startX + (i * spacing), centerY, 0f);

                cards[i].transform.DOKill();
                cards[i].transform.SetSiblingIndex(i);
                cards[i].transform.DOLocalMove(targetPosition, duration);
                cards[i].transform.DORotate(Vector3.zero, duration);
                cards[i].transform.DOScale(Vector3.one, duration);
            }

            yield return new WaitForSeconds(duration);
        }

        public void ClearAllCardsImmediate()
        {
            cards.RemoveAll(cv => cv == null);

            for (int i = cards.Count - 1; i >= 0; i--)
            {
                if (cards[i] != null)
                {
                    Destroy(cards[i].gameObject);
                }
            }

            cards.Clear();
        }
    }
}
