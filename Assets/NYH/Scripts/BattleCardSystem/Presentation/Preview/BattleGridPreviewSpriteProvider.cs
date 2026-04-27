namespace NYH.BattleCardSystem
{
    using UnityEngine;

    // 모든 그리드 프리뷰 레이어가 공유하는 흰색 스프라이트를 제공합니다.
    internal static class BattleGridPreviewSpriteProvider
    {
        private static Sprite cachedPreviewSprite;

        // Texture2D.whiteTexture 기반 스프라이트를 지연 생성하고 재사용합니다.
        public static Sprite GetWhiteSprite()
        {
            if (cachedPreviewSprite != null)
            {
                return cachedPreviewSprite;
            }

            cachedPreviewSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0.5f, 0.5f),
                Texture2D.whiteTexture.width);

            return cachedPreviewSprite;
        }
    }
}
