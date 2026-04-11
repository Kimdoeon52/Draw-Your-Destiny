namespace NYH.BattleCardSystem
{
    using NYH.CoreCardSystem;
    using UnityEngine;

    /*
     * BattleCardPlayHandler
     *
     * 역할:
     * - 공용 CardView의 입력/호버/드래그는 그대로 사용하고,
     *   전투 카드일 때 실제 사용 로직만 BattleUIController로 위임합니다.
     */
    [RequireComponent(typeof(CardView))]
    public class BattleCardPlayHandler : MonoBehaviour, ICardViewPlayHandler
    {
        private BattleCard boundBattleCard;
        private BattleUIController owner;

        public void Bind(BattleCard battleCard, BattleUIController battleUIController)
        {
            boundBattleCard = battleCard;
            owner = battleUIController;
        }

        public bool TryPlayCard(CardView cardView, Vector2 screenPosition, bool wasDragged)
        {
            if (owner == null || boundBattleCard == null)
            {
                return false;
            }

            return owner.HandleBattleCardReleased(boundBattleCard, screenPosition, wasDragged);
        }
    }
}
