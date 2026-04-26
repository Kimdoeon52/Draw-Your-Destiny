namespace NYH.BattleCardSystem
{
    using System;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    /// <summary>
    /// 교체 UI 인스턴스를 찾거나 없으면 만들어 주는 부트스트랩 helper입니다.
    /// BattleDeckReplacementUI 본체가 화면 흐름 제어에 집중하도록
    /// 씬 탐색과 런타임 생성 책임을 분리합니다.
    /// </summary>
    internal static class BattleDeckReplacementBootstrapper
    {
        /// <summary>
        /// 기존 UI를 찾아 반환하거나, 없으면 새 루트까지 생성해 반환합니다.
        /// </summary>
        public static BattleDeckReplacementUI GetOrCreate(BattleDeckReplacementUI currentInstance)
        {
            if (currentInstance != null)
            {
                return currentInstance;
            }

            BattleDeckReplacementUI[] existingUis = UnityEngine.Object.FindObjectsByType<BattleDeckReplacementUI>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            if (existingUis.Length > 0 && existingUis[0] != null)
            {
                return existingUis[0];
            }

            Transform[] transforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Transform candidate in transforms)
            {
                if (candidate == null || !string.Equals(candidate.name, "BattleDeckReplacementUI", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                BattleDeckReplacementUI existingComponent = candidate.GetComponent<BattleDeckReplacementUI>();
                return existingComponent != null
                    ? existingComponent
                    : candidate.gameObject.AddComponent<BattleDeckReplacementUI>();
            }

            Canvas parentCanvas = FindPreferredParentCanvas();
            if (parentCanvas == null)
            {
                GameObject canvasObject = new(
                    "BattleDeckReplacementCanvas",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));
                parentCanvas = canvasObject.GetComponent<Canvas>();
                parentCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                UnityEngine.Object.DontDestroyOnLoad(canvasObject);
            }

            EnsureEventSystemExists();

            GameObject root = new("BattleDeckReplacementUI", typeof(RectTransform));
            root.transform.SetParent(parentCanvas.transform, false);
            return root.AddComponent<BattleDeckReplacementUI>();
        }

        /// <summary>
        /// 런타임 생성 경로에서 EventSystem이 없으면 함께 만들어 줍니다.
        /// </summary>
        private static void EnsureEventSystemExists()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObject = new("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            UnityEngine.Object.DontDestroyOnLoad(eventSystemObject);
        }

        /// <summary>
        /// 런타임 생성 UI를 붙일 부모 Canvas를 찾습니다.
        /// 활성화된 루트 ScreenSpaceOverlay Canvas를 우선 사용합니다.
        /// </summary>
        private static Canvas FindPreferredParentCanvas()
        {
            Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Canvas canvas in canvases)
            {
                if (canvas == null || !canvas.isActiveAndEnabled)
                {
                    continue;
                }

                if (!canvas.isRootCanvas || !canvas.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    return canvas;
                }
            }

            return null;
        }
    }
}
