namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using UnityEngine;

    public class BattleUnitHitFlash : MonoBehaviour
    {
        [SerializeField] private Color flashColor = new(1f, 0.35f, 0.35f, 1f);
        [SerializeField] private float flashDuration = 0.12f;
        [SerializeField] private bool includeInactiveChildren = true;

        private readonly List<SpriteRenderer> renderers = new();
        private readonly Dictionary<SpriteRenderer, Color> originalColors = new();
        private int playToken;

        private void Awake()
        {
            CacheRenderers();
        }

        private void OnEnable()
        {
            CacheRenderers();
            RestoreOriginalColors();
        }

        private void OnDisable()
        {
            RestoreOriginalColors();
        }

        public void Play()
        {
            CacheRenderers();
            if (renderers.Count == 0)
            {
                return;
            }

            playToken++;
            PlayAsync(playToken).Forget();
        }

        private void CacheRenderers()
        {
            renderers.Clear();
            originalColors.Clear();

            SpriteRenderer[] foundRenderers = GetComponentsInChildren<SpriteRenderer>(includeInactiveChildren);
            for (int i = 0; i < foundRenderers.Length; i++)
            {
                SpriteRenderer renderer = foundRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                renderers.Add(renderer);
                originalColors[renderer] = renderer.color;
            }
        }

        private async UniTaskVoid PlayAsync(int token)
        {
            ApplyFlashColor();
            await UniTask.Delay(Mathf.RoundToInt(Mathf.Max(0.01f, flashDuration) * 1000f));

            if (this == null || token != playToken)
            {
                return;
            }

            RestoreOriginalColors();
        }

        private void ApplyFlashColor()
        {
            for (int i = 0; i < renderers.Count; i++)
            {
                SpriteRenderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                if (!originalColors.ContainsKey(renderer))
                {
                    originalColors[renderer] = renderer.color;
                }

                renderer.color = flashColor;
            }
        }

        private void RestoreOriginalColors()
        {
            foreach (KeyValuePair<SpriteRenderer, Color> pair in originalColors)
            {
                if (pair.Key == null)
                {
                    continue;
                }

                pair.Key.color = pair.Value;
            }
        }
    }
}
