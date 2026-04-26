namespace NYH.BattleCardSystem
{
    using UnityEngine;
    using UnityEngine.EventSystems;

    /// <summary>
    /// 교체 후보 카드에서 발생한 클릭을 메인 UI 컨트롤러로 전달하는 전용 핸들러입니다.
    /// 카드 자체는 일반 CardView를 유지하고, 선택 상태만 별도로 관리하기 위해 사용합니다.
    /// </summary>
    public sealed class BattleDeckReplacementCandidateClickHandler : MonoBehaviour, IPointerClickHandler
    {
        private BattleDeckReplacementUI owner;
        private BattleCardData candidate;
        private int candidateIndex = -1;

        /// <summary>
        /// 이 클릭 핸들러가 전달할 대상 UI와 카드 데이터를 설정합니다.
        /// </summary>
        public void Setup(BattleDeckReplacementUI owner, BattleCardData candidate, int candidateIndex)
        {
            this.owner = owner;
            this.candidate = candidate;
            this.candidateIndex = candidateIndex;
        }

        /// <summary>
        /// 좌클릭만 교체 후보 선택으로 처리합니다.
        /// 실제 교체는 확인 버튼에서만 일어나고, 여기서는 선택 상태만 전달합니다.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            owner?.HandleCandidateCardClicked(candidate, candidateIndex);
        }
    }
}
