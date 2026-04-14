namespace NYH.CoreCardSystem
{
    /*
     * CardActionRegistrar
     * - CardSystem이 처리하는 GameAction의 ActionSystem performer 등록을 전담하는 클래스
     * - ActionSystem.AttachPerformer<T>() 등록 전담
     *
     * 역할:
     * - CardSystem이 처리해야 하는 GameAction performer 등록 책임만 담당합니다.
     * - _CardSystem에서 AttachPerformer 나열 코드를 제거해, 메인 시스템을 더 얇게 만듭니다.
     *
     * 여기에 넣는 것:
     * - ActionSystem.AttachPerformer<T>() 등록 코드
     * - CardSystem.Perform()으로 라우팅할 액션 목록
     *
     * 여기에 넣지 않는 것:
     * - 실제 액션 처리 로직
     * - 카드 뷰 갱신
     * - 카드 더미 상태 변경
     *
     * 사용하는 법:
     * - _CardSystem.Awake()에서 RegisterAll(this) 한 번만 호출합니다.
     * - 새 GameAction을 CardSystem에서 처리하게 할 때는 이 파일에 AttachPerformer를 추가합니다.
     */
    public static class CardActionRegistrar
    {
        public static void RegisterAll(CardSystem cardSystem)
        {
            ActionSystem.AttachPerformer<DrawCardsGA>(action => cardSystem.Perform(action));
            ActionSystem.AttachPerformer<PlayCardGA>(action => cardSystem.Perform(action));
            ActionSystem.AttachPerformer<DiscardAllCardsGA>(action => cardSystem.Perform(action));
            ActionSystem.AttachPerformer<DiscardRandomGA>(action => cardSystem.Perform(action));
            ActionSystem.AttachPerformer<ExtinctionCardGA>(action => cardSystem.Perform(action));
            ActionSystem.AttachPerformer<GoldCardGA>(action => cardSystem.Perform(action));
            ActionSystem.AttachPerformer<PlayCostGA>(action => cardSystem.Perform(action));
            ActionSystem.AttachPerformer<ChooseOneGA>(action => cardSystem.Perform(action));
            ActionSystem.AttachPerformer<ResearchpointsGA>(action => cardSystem.Perform(action));
            ActionSystem.AttachPerformer<IncreasePopulationGA>(action => cardSystem.Perform(action));
            ActionSystem.AttachPerformer<CostPlusGA>(action => cardSystem.Perform(action));
            ActionSystem.AttachPerformer<CountCardByTypeGA>(action => cardSystem.Perform(action));
            ActionSystem.AttachPerformer<GenerateHumanGA>(action => cardSystem.Perform(action));
            ActionSystem.AttachPerformer<IncreaseFoodGA>(action => cardSystem.Perform(action));
            ActionSystem.AttachPerformer<ConvertGoldToFoodGA>(action => cardSystem.Perform(action));
            ActionSystem.AttachPerformer<ReturnToHandGA>(action => cardSystem.Perform(action));
            ActionSystem.AttachPerformer<ZeroCostHandGA>(action => cardSystem.Perform(action));
            ActionSystem.AttachPerformer<PlayBuildingGA>(action => cardSystem.Perform(action));
            ActionSystem.AttachPerformer<SetCardTypeMultiplierGA>(action => cardSystem.Perform(action));
            ActionSystem.AttachPerformer<DisableCardTypeGA>(action => cardSystem.Perform(action));
            ActionSystem.AttachPerformer<AddCardGA>(action => cardSystem.Perform(action));
            ActionSystem.AttachPerformer<LockDrawGA>(action => cardSystem.Perform(action));
        }
    }
}
