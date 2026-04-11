namespace NYH.CoreCardSystem
{
    using System.Collections;
    using DG.Tweening;
    using UnityEngine;

    public static class CardViewAnimationUtility
    {
        public static IEnumerator AnimateDiscard(
            CardView cardView,
            Transform discardPilePoint,
            float moveDuration = 0.2f,
            float scaleDuration = 0.2f,
            bool destroyAtEnd = true)
        {
            if (cardView == null || discardPilePoint == null)
            {
                yield break;
            }

            cardView.transform.DOKill();
            cardView.transform.DOScale(Vector3.zero, scaleDuration);
            yield return cardView.transform.DOMove(discardPilePoint.position, moveDuration).WaitForCompletion();

            if (destroyAtEnd && cardView != null)
            {
                Object.Destroy(cardView.gameObject);
            }
        }
    }
}
