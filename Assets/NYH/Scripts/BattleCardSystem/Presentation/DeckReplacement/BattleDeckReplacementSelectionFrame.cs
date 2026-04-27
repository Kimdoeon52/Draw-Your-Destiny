namespace NYH.BattleCardSystem
{
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 후보 카드 위에 덧씌우는 선택 표시 전용 컴포넌트입니다.
    /// 실제 선택 판정은 하지 않고, 외부에서 on/off만 제어합니다.
    /// </summary>
    internal sealed class BattleDeckReplacementSelectionFrame : MonoBehaviour
    {
        private Image frameImage;
        private Outline frameOutline;

        /// <summary>
        /// 선택 프레임에 사용할 이미지와 외곽선 참조를 초기화합니다.
        /// </summary>
        public void Initialize(Image frameImage, Outline frameOutline)
        {
            this.frameImage = frameImage;
            this.frameOutline = frameOutline;
        }

        /// <summary>
        /// 선택 여부에 따라 시각 표시만 켜고 끕니다.
        /// </summary>
        public void SetSelected(bool isSelected)
        {
            if (frameImage != null)
            {
                frameImage.enabled = isSelected;
            }

            if (frameOutline != null)
            {
                frameOutline.enabled = isSelected;
            }
        }
    }
}
