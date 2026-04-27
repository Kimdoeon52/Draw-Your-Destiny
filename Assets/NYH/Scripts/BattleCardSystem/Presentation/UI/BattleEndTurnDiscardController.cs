namespace NYH.BattleCardSystem
{
    using System;
    using System.Collections;
    using NYH.CoreCardSystem;
    using UnityEngine;

    /// <summary>
    /// 플레이어 턴 종료 시 손패 버림 애니메이션과 실제 턴 종료 호출을 담당합니다.
    /// HUD 버튼 상태 계산, 카드 타겟팅, 손패 재구성은 담당하지 않습니다.
    /// </summary>
    internal sealed class BattleEndTurnDiscardController
    {
        public bool IsResolving { get; private set; }

        public IEnumerator DiscardHandThenEndTurn(
            BattleManager battleManager,
            HandView handView,
            Transform discardPilePoint,
            MonoBehaviour coroutineOwner,
            Action refreshHud)
        {
            if (battleManager == null || battleManager.CurrentPhase != BattlePhase.PlayerTurn)
            {
                yield break;
            }

            if (discardPilePoint == null || handView == null || handView.Cards.Count == 0)
            {
                battleManager.EndPlayerTurn();
                yield break;
            }

            IsResolving = true;
            refreshHud?.Invoke();

            CardView[] cardsToDiscard = new CardView[handView.Cards.Count];
            for (int i = 0; i < handView.Cards.Count; i++)
            {
                cardsToDiscard[i] = handView.Cards[i];
            }

            foreach (CardView cardView in cardsToDiscard)
            {
                if (cardView == null)
                {
                    continue;
                }

                handView.RemoveCard(cardView.Card);
                if (coroutineOwner != null)
                {
                    coroutineOwner.StartCoroutine(CardViewAnimationUtility.AnimateDiscard(cardView, discardPilePoint));
                }
                else
                {
                    yield return CardViewAnimationUtility.AnimateDiscard(cardView, discardPilePoint);
                }

                yield return new WaitForSeconds(0.05f);
            }

            IsResolving = false;
            battleManager.EndPlayerTurn();
            refreshHud?.Invoke();
        }
    }
}
