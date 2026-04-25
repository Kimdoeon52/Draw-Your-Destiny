namespace NYH.BattleCardSystem
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Opens the dedicated replacement UI and waits for the player to pick a card to replace.
    /// </summary>
    internal sealed class BattleDeckReplacementSelector
    {
        /// <summary>
        /// Shows the replacement UI and invokes the callback with the chosen replacement target.
        /// Null is returned when the player cancels.
        /// </summary>
        public IEnumerator SelectReplacement(
            BattleCardData rewardCard,
            IReadOnlyList<BattleCardData> candidates,
            Action<BattleCardData> onSelected)
        {
            BattleDeckReplacementUI replacementUi = BattleDeckReplacementUI.GetOrCreate();
            if (replacementUi == null)
            {
                Debug.LogWarning("[BattleDeckReplacementSelector] BattleDeckReplacementUI is missing.");
                onSelected?.Invoke(null);
                yield break;
            }

            if (candidates == null || candidates.Count == 0)
            {
                Debug.LogWarning("[BattleDeckReplacementSelector] No replacement candidates.");
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
