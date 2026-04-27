namespace NYH.BattleCardSystem
{
    using NYH.CoreCardSystem;

    // ActionSystem에 전투 카드 GameAction 처리기를 등록합니다.
    internal static class BattleCardActionRegistrar
    {
        // 전투 카드 사용/공격/이동 액션을 BattleCardSystem.Perform으로 연결합니다.
        public static void RegisterAll(BattleCardSystem battleCardSystem)
        {
            ActionSystem.AttachPerformer<BattlePlayCardGA>(action => battleCardSystem.Perform(action));
            ActionSystem.AttachPerformer<BattleAttackGA>(action => battleCardSystem.Perform(action));
            ActionSystem.AttachPerformer<BattleMoveGA>(action => battleCardSystem.Perform(action));
        }
    }
}
