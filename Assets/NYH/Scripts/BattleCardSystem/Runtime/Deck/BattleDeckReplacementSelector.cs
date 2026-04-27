namespace NYH.BattleCardSystem
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// "덱이 가득 찼을 때 어떤 카드를 교체할지"를 묻는 코루틴 래퍼입니다.
    ///
    /// 역할:
    /// - BattleDeckReplacementUI를 열기
    /// - 플레이어가 확인 또는 취소할 때까지 기다리기
    /// - 결과를 콜백으로 호출자에게 되돌려 주기
    ///
    /// 실제 카드 교체는 여기서 하지 않습니다.
    /// 선택 결과만 반환하고, ReplaceCard 호출은 상위 흐름이 담당합니다.
    /// </summary>
    internal sealed class BattleDeckReplacementSelector
    {
        /// <summary>
        /// 교체 UI를 띄우고, 플레이어가 고른 교체 대상 카드를 콜백으로 전달합니다.
        /// 취소한 경우 null을 전달합니다.
        ///
        /// 흐름:
        /// 1. UI 확보
        /// 2. 후보 목록 검사
        /// 3. UI Show
        /// 4. 확인/취소 콜백이 올 때까지 WaitUntil
        /// 5. 결과 반환
        /// </summary>
        public IEnumerator SelectReplacement(
            BattleCardData rewardCard,
            IReadOnlyList<BattleCardData> candidates,
            Action<BattleCardData> onSelected)
        {
            BattleDeckReplacementUI replacementUi = BattleDeckReplacementUI.GetOrCreate();
            if (replacementUi == null)
            {
                Debug.LogWarning("[BattleDeckReplacementSelector] BattleDeckReplacementUI가 없어 교체 UI를 열 수 없습니다.");
                onSelected?.Invoke(null);
                yield break;
            }

            if (candidates == null || candidates.Count == 0)
            {
                Debug.LogWarning("[BattleDeckReplacementSelector] 교체 후보 카드가 없습니다.");
                onSelected?.Invoke(null);
                yield break;
            }

            BattleCardData selectedData = null;
            bool isFinished = false;

            replacementUi.Show(
                rewardCard,
                candidates,
                confirmed =>
                {
                    selectedData = confirmed;
                    isFinished = true;
                },
                () =>
                {
                    selectedData = null;
                    isFinished = true;
                });

            yield return new WaitUntil(() => isFinished);
            onSelected?.Invoke(selectedData);
        }
    }
}
