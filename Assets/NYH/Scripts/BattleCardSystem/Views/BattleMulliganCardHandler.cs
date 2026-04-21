namespace NYH.BattleCardSystem
{
    using System;
    using NYH.CoreCardSystem;
    using UnityEngine;
    using UnityEngine.EventSystems;

    /// <summary>
    /// 멀리건 중인 카드 클릭만 BattleMulliganController로 전달합니다.
    /// 선택 상태나 카드 사용 규칙은 담당하지 않습니다.
    /// </summary>
    [RequireComponent(typeof(CardView))]
    public sealed class BattleMulliganCardHandler : MonoBehaviour, IPointerClickHandler
    {
        private BattleCard battleCard;
        private Action<BattleCard> onClicked;

        public void Bind(BattleCard battleCard, Action<BattleCard> onClicked)
        {
            this.battleCard = battleCard;
            this.onClicked = onClicked;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || battleCard == null)
            {
                return;
            }

            onClicked?.Invoke(battleCard);
        }
    }
}
