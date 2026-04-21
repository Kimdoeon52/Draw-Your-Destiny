namespace NYH.BattleCardSystem
{
    using TMPro;
    using UnityEngine.UI;

    /// <summary>
    /// 전투 HUD 텍스트와 버튼 활성 상태만 갱신합니다.
    /// 턴 전환, 카드 사용, 타겟팅 입력 처리는 담당하지 않습니다.
    /// </summary>
    internal sealed class BattleHudPresenter
    {
        private readonly TMP_Text turnText;
        private readonly TMP_Text phaseText;
        private readonly TMP_Text actionPointsText;
        private readonly Button endTurnButton;
        private readonly Button mulliganConfirmButton;

        public BattleHudPresenter(
            TMP_Text turnText,
            TMP_Text phaseText,
            TMP_Text actionPointsText,
            Button endTurnButton,
            Button mulliganConfirmButton)
        {
            this.turnText = turnText;
            this.phaseText = phaseText;
            this.actionPointsText = actionPointsText;
            this.endTurnButton = endTurnButton;
            this.mulliganConfirmButton = mulliganConfirmButton;
        }

        public void Refresh(
            BattleManager battleManager,
            BattleCardSystem battleCardSystem,
            bool isResolvingEndTurnDiscard,
            bool isResolvingMulligan,
            bool hasMulliganSelection,
            bool isTargetingIdle,
            bool anyCardPickedUp)
        {
            if (battleManager != null)
            {
                if (turnText != null)
                {
                    turnText.text = $"Turn {battleManager.BattleTurn}";
                }

                if (phaseText != null)
                {
                    phaseText.text = battleManager.CurrentPhase.ToString();
                }
            }

            if (battleCardSystem != null && actionPointsText != null)
            {
                actionPointsText.text = $"AP {battleCardSystem.CurrentActionPoints}";
            }

            if (endTurnButton != null && battleManager != null)
            {
                endTurnButton.interactable = !isResolvingEndTurnDiscard
                    && !isResolvingMulligan
                    && isTargetingIdle
                    && !anyCardPickedUp
                    && !battleManager.IsBattleEnded
                    && (battleManager.CurrentPhase == BattlePhase.PlayerTurn || battleManager.CurrentPhase == BattlePhase.EnemyTurn);
            }

            if (mulliganConfirmButton != null && battleManager != null)
            {
                mulliganConfirmButton.gameObject.SetActive(battleManager.IsMulliganPhase);
                mulliganConfirmButton.interactable = !isResolvingMulligan && hasMulliganSelection;
            }
        }
    }
}
