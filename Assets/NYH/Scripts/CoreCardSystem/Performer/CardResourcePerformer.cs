namespace NYH.CoreCardSystem
{
    using System.Collections;
    using Unity.VisualScripting;
    using UnityEngine;

    /*
     * CardResourcePerformer
     *
     * - 골드, 식량, 연구, 인구, 코스트 변경 등 수치/자원 처리
     *
     * 역할:
     * - 카드 효과 중 자원/수치 변경 계열 액션을 실행합니다.
     * - 주로 GameManager와 직접 연결되는 액션을 모아 둔 performer입니다.
     *
     * 여기에 넣는 것:
     * - GoldCardGA, PlayCostGA, ResearchpointsGA, IncreasePopulationGA
     * - IncreaseFoodGA, ConvertGoldToFoodGA
     * - GenerateHumanGA
     * - CostPlusGA, ZeroCostHandGA, CountCardByTypeGA처럼 카드 수치/상태를 바꾸는 로직
     * - ContinueBehaviourGA처럼 턴 지속 효과 등록 같은 "자원/상태 처리" 성격의 액션
     *
     * 여기에 넣지 않는 것:
     * - 카드 버리기/소멸 애니메이션
     * - 카드 선택 UI
     * - 건물 설치
     *
     * 사용하는 법:
     * - CardSystem.Perform()에서 action이 이 클래스가 처리할 종류인지 확인한 뒤
     *   Perform(action)을 위임합니다.
     * - 새 자원형 능력을 만들면 보통 이 파일에 분기를 추가하면 됩니다.
     */
    
    /// <summary>
    /// GameManager와 직접 연결되는 자원/수치 변경 액션들전용 Performer
    /// </summary>
    public class CardResourcePerformer
    {
        private readonly CardPileState pileState;
        private readonly System.Action refreshCardViews;
        private readonly System.Action<int> setCardTypeCount;

        public CardResourcePerformer(
            CardPileState pileState,
            System.Action refreshCardViews,
            System.Action<int> setCardTypeCount)
        {
            this.pileState = pileState;
            this.refreshCardViews = refreshCardViews;
            this.setCardTypeCount = setCardTypeCount;
        }

        public bool CanHandle(GameAction action)
        {
            return action is GoldCardGA
                || action is PlayCostGA
                || action is ResearchpointsGA
                || action is IncreasePopulationGA
                || action is ContinueBehaviourGA
                || action is CostPlusGA
                || action is CountCardByTypeGA
                || action is GenerateHumanGA
                || action is IncreaseFoodGA
                || action is ConvertGoldToFoodGA
                || action is ZeroCostHandGA
                || action is AddResearchByCurResearchGA
                || action is interestGA;
        }

        public IEnumerator Perform(GameAction action)
        {
            if (action is GoldCardGA goldCardGA)
            {
                GameManager.Instance.AddGold(goldCardGA.Amount);
                yield return null;
            }
            else if (action is PlayCostGA playCostGA)
            {
                GameManager.Instance.SpendGold(playCostGA.Amount);
                yield return null;
            }
            else if (action is ResearchpointsGA researchpointsGA)
            {
                GameManager.Instance.AddResearch(researchpointsGA.Amount);
                yield return null;
            }
            else if (action is IncreasePopulationGA increasePopulationGA)
            {
                GameManager.Instance.IncreasePopulationCap(increasePopulationGA.Amount);
                yield return null;
            }
            else if (action is ContinueBehaviourGA continueGA)
            {
                OngoingEffectSystem.Instance.Register(
                    continueGA.SourceCard,
                    continueGA.StartEffectIndex,
                    continueGA.TurnAmount);
                yield return null;
            }
            else if (action is CostPlusGA costPlusGA)
            {
                costPlusGA.SourceCard.Cost += costPlusGA.Cost;
                refreshCardViews?.Invoke();
                yield return null;
            }
            else if (action is CountCardByTypeGA countCardByTypeGA)
            {
                setCardTypeCount?.Invoke(pileState.CountHandByType(countCardByTypeGA._CardType));
                yield return null;
            }
            else if (action is GenerateHumanGA generateHumanGA)
            {
                GameManager.Instance.GenerateHumans(generateHumanGA.Amount, generateHumanGA._UnitInfo);
                yield return null;
            }
            else if (action is IncreaseFoodGA increaseFoodGA)
            {
                GameManager.Instance.AddFood(increaseFoodGA.Amount);
                yield return null;
            }
            else if (action is ConvertGoldToFoodGA convertGoldToFoodGA)
            {
                GameManager.Instance.ConvertGoldToFood(convertGoldToFoodGA.Percent);
                yield return null;
            }
            else if (action is ZeroCostHandGA zeroCostHandGA)
            {
                int modifiedCount = 0;
                foreach (var card in pileState.Hand)
                {
                    if (zeroCostHandGA.CardAmount > 0 && modifiedCount >= zeroCostHandGA.CardAmount)
                    {
                        break;
                    }

                    card.Cost = zeroCostHandGA.CostAmount;
                    modifiedCount++;
                }

                refreshCardViews?.Invoke();
                yield return null;
            }
            else if (action is AddResearchByCurResearchGA addResearchByCurResearchGA)
            {
                if(ResourceManager.Instance.Research >= 50)
                    GameManager.Instance.AddResearch(addResearchByCurResearchGA.Amount * 2);
                else
                    GameManager.Instance.AddResearch(addResearchByCurResearchGA.Amount);
            }
            else if (action is interestGA interestAction)
            {
                GameManager.Instance.AddGold(-interestAction.MinusGold);
                GameManager.Instance.RegisterPendingGold(
                    GameManager.Instance.currentTurn + interestAction.TurnAmount,
                    interestAction.PlusGold);
                yield return null;
            }
        }
    }
}
