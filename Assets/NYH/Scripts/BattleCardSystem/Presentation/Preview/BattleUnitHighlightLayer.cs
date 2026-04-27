namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    // 유닛 SpriteRenderer 색상 하이라이트와 복원을 담당합니다.
    internal sealed class BattleUnitHighlightLayer
    {
        private readonly Dictionary<SpriteRenderer, Color> highlightedUnitColors = new();

        // 전달받은 유닛들의 SpriteRenderer 원래 색을 저장하고 하이라이트 색으로 바꿉니다.
        public void Show(IEnumerable<BattleUnit> units, Color highlightColor)
        {
            Clear();
            if (units == null)
            {
                return;
            }

            foreach (BattleUnit unit in units)
            {
                HighlightUnit(unit, highlightColor);
            }
        }

        // 저장해 둔 원래 색상으로 모든 하이라이트 유닛을 되돌립니다.
        public void Clear()
        {
            foreach (var pair in highlightedUnitColors)
            {
                if (pair.Key != null)
                {
                    pair.Key.color = pair.Value;
                }
            }

            highlightedUnitColors.Clear();
        }

        // 현재 하이라이트를 해제하고 씬의 모든 BattleUnit 렌더러 색상을 흰색으로 초기화합니다.
        public void ResetAllUnitColorsImmediate()
        {
            Clear();

            BattleUnit[] allUnits = Object.FindObjectsByType<BattleUnit>(FindObjectsSortMode.None);
            for (int i = 0; i < allUnits.Length; i++)
            {
                BattleUnit unit = allUnits[i];
                if (unit == null)
                {
                    continue;
                }

                ResetUnitColor(unit);
            }
        }

        // 단일 유닛의 모든 자식 SpriteRenderer를 하이라이트 색상으로 변경합니다.
        private void HighlightUnit(BattleUnit unit, Color highlightColor)
        {
            if (unit == null)
            {
                return;
            }

            SpriteRenderer[] renderers = unit.GetComponentsInChildren<SpriteRenderer>();
            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer == null || highlightedUnitColors.ContainsKey(renderer))
                {
                    continue;
                }

                highlightedUnitColors.Add(renderer, renderer.color);
                renderer.color = highlightColor;
            }
        }

        // 단일 유닛의 모든 자식 SpriteRenderer 색상을 흰색으로 되돌립니다.
        private static void ResetUnitColor(BattleUnit unit)
        {
            SpriteRenderer[] renderers = unit.GetComponentsInChildren<SpriteRenderer>(true);
            for (int j = 0; j < renderers.Length; j++)
            {
                SpriteRenderer renderer = renderers[j];
                if (renderer == null)
                {
                    continue;
                }

                renderer.color = Color.white;
            }
        }
    }
}
