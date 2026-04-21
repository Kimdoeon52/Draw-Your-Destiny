namespace NYH.BattleCardSystem
{
    using System;
    using System.Collections;
    using NYH.CoreCardSystem;
    using UnityEngine;

    /// <summary>
    /// 카드 사용 완료 후 카드뷰 버림 애니메이션, 승패 체크, 전투 UI 갱신을 이어줍니다.
    /// 카드 사용 가능 여부, 실제 효과 실행, 타겟팅 상태 관리는 담당하지 않습니다.
    /// </summary>
    internal sealed class BattlePlayedCardResolutionController
    {
        public IEnumerator Resolve(
            BattleManager battleManager,
            BattleGridPreviewSystem gridPreviewSystem,
            CardView playedCardView,
            Transform discardPilePoint,
            Action refreshHand,
            Action refreshHud)
        {
            CardViewHoverSystem.Instance?.Hide();

            if (playedCardView != null)
            {
                if (discardPilePoint != null)
                {
                    yield return CardViewAnimationUtility.AnimateDiscard(playedCardView, discardPilePoint);
                }
                else
                {
                    UnityEngine.Object.Destroy(playedCardView.gameObject);
                }
            }

            battleManager?.CheckBattleEnd();
            gridPreviewSystem?.ResetAllUnitColorsImmediate();
            refreshHud?.Invoke();
            refreshHand?.Invoke();
        }
    }
}
