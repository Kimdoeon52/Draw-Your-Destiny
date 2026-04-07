using NYH.CoreCardSystem;
using UnityEngine;

namespace NYH.CoreCardSystem
{
    [System.Serializable]
    public abstract class PlacementEffect : Effect
    {
        // 설치 모드에서 마우스 끝에 보여줄 프리뷰용 스프라이트 (건물 이미지)
        public abstract Sprite GetPreviewSprite();
        
        // 설치가 확정되었을 때 실행할 액션을 반환
        public override abstract GameAction GetGameAction(int effectIndex = 0, Card sourceCard = null);
    }
}
