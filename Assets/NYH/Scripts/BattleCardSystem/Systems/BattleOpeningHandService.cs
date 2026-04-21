namespace NYH.BattleCardSystem
{
    using UnityEngine;

    /// <summary>
    /// 전투 시작 손패와 턴 드로우 수 계산 규칙을 담당합니다.
    /// 실제 카드 더미에서 카드를 뽑거나 UI를 갱신하는 일은 담당하지 않습니다.
    /// </summary>
    internal sealed class BattleOpeningHandService
    {
        public int CalculateDrawCountByAliveUnitTypes(int aliveUnitTypeCount)
        {
            return Mathf.Max(1, aliveUnitTypeCount + 1);
        }
    }
}
