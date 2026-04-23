namespace NYH.CoreCardSystem
{
    using System.Collections;

    /*
     * CardModifierPerformer
     *
     * - 카드 타입 배율, 카드 타입 비활성 같은 modifier 처리
     *
     * 역할:
     * - 카드 타입 배율 변경, 카드 타입 사용 금지 같은 modifier 계열 액션을 처리합니다.
     * - 실제 modifier 데이터 저장은 CardModifierSystem이 맡고,
     *   이 performer는 GameAction을 받아 CardModifierSystem에 전달하는 역할을 합니다.
     *
     * 여기에 넣는 것:
     * - SetCardTypeMultiplierGA 처리
     * - DisableCardTypeGA 처리
     * - modifier 적용 후 카드 UI 갱신 트리거
     *
     * 여기에 넣지 않는 것:
     * - 카드 드로우/버리기/소멸
     * - 자원 변경
     * - 카드 선택 UI
     * - 건물 설치
     *
     * 사용하는 법:
     * - CardSystem.Perform()에서 modifier 계열 action인지 확인한 뒤 이 performer로 위임합니다.
     * - 이후 modifier 종류가 늘어나면 이 파일에 분기를 추가하면 됩니다.
     */
    public class CardModifierPerformer
    {
        private readonly System.Action refreshCardViews;

        public CardModifierPerformer(System.Action refreshCardViews)
        {
            this.refreshCardViews = refreshCardViews;
        }

        public bool CanHandle(GameAction action)
        {
            return action is SetCardTypeMultiplierGA
                || action is DisableCardTypeGA
                || action is LockDrawGA
                || action is SelectOneAndCopyGA;
        }

        public IEnumerator Perform(GameAction action)
        {
            if (action is SetCardTypeMultiplierGA setCardTypeMultiplierGA)
            {
                CardModifierSystem.SetTypeMultiplier(
                    setCardTypeMultiplierGA.TargetType,
                    setCardTypeMultiplierGA.Multiplier,
                    setCardTypeMultiplierGA.DurationTurns);
                refreshCardViews?.Invoke();
                yield return null;
            }
            else if (action is DisableCardTypeGA disableCardTypeGA)
            {
                CardModifierSystem.BlockType(
                    disableCardTypeGA.TargetType,
                    disableCardTypeGA.DurationTurns);
                refreshCardViews?.Invoke();
                yield return null;
            }
            else if (action is LockDrawGA lockDrawGA)
            {
                CardModifierSystem.DrawLock();
            }
            else if (action is SelectOneAndCopyGA selectOneAndCopyGA)
            {
                CardSystem.Instance.ShowDeckDistinct();
            }
        }
    }
}
