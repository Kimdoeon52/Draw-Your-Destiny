namespace NYH.BattleCardSystem
{
    using UnityEngine;
    using UnityEngine.EventSystems;

    /// <summary>
    /// 교체 후보 카드 한 장에 붙는 클릭 전달 전용 컴포넌트입니다.
    /// 이 스크립트는 "카드를 눌렀다"는 사실만 메인 UI에 알려주고,
    /// 실제 선택 표시 변경이나 확인 버튼 활성화는 하지 않습니다.
    ///
    /// 책임 분리:
    /// - 이 클래스: 클릭 좌표/버튼 종류 확인, 어떤 카드/슬롯이 눌렸는지 전달
    /// - BattleDeckReplacementUI: 선택 상태 저장, 선택 표시, 확인/취소 흐름 관리
    /// </summary>
    public sealed class BattleDeckReplacementCandidateClickHandler : MonoBehaviour, IPointerClickHandler
    {
        private BattleDeckReplacementUI owner;
        private BattleCardData candidate;
        private int candidateIndex = -1;

        /// <summary>
        /// 클릭 시 메인 UI로 되돌려 보낼 대상 정보를 저장합니다.
        /// 같은 BattleCardData를 공유하는 카드가 여러 장 있을 수 있으므로
        /// 카드 데이터뿐 아니라 슬롯 인덱스도 함께 기억합니다.
        /// </summary>
        public void Setup(BattleDeckReplacementUI owner, BattleCardData candidate, int candidateIndex)
        {
            this.owner = owner;
            this.candidate = candidate;
            this.candidateIndex = candidateIndex;
        }

        /// <summary>
        /// 좌클릭만 선택 입력으로 처리합니다.
        /// 여기서는 창을 닫지 않고, "이 슬롯을 선택했다"는 정보만 UI에 전달합니다.
        /// 실제 교체는 확인 버튼을 눌렀을 때만 진행됩니다.
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
