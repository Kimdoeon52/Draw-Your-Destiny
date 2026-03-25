using NYH.CoreCardSystem;
using UnityEngine;
     public abstract class PlacementEffect : Effect
     {
         // 설치 모드에서 마우스 끝에 보여줄 프리뷰용 데이터 (건물이든 유닛이든)
         public abstract Sprite GetPreviewSprite();
    
         // 설치가 확정되었을 때 실행할 액션
         public override abstract GameAction GetGameAction();
    }
