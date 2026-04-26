namespace NYH.BattleCardSystem
{
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 씬 프리팹에 교체 UI가 준비되지 않았을 때만 사용하는 runtime fallback 레이아웃 빌더입니다.
    /// 평소에는 인스펙터로 연결한 UI를 쓰고, 이 코드는 안전망 역할만 합니다.
    /// </summary>
    internal static class BattleDeckReplacementRuntimeLayoutBuilder
    {
        /// <summary>
        /// 필수 참조가 모두 비어 있을 때만 임시 교체 UI를 생성합니다.
        /// 이미 연결된 프리팹 UI가 있으면 아무 작업도 하지 않습니다.
        /// </summary>
        public static void BuildIfNeeded(
            BattleDeckReplacementViewContext context,
            UnityEngine.Events.UnityAction onConfirm,
            UnityEngine.Events.UnityAction onCancel)
        {
            if (BattleDeckReplacementViewBindingUtility.HasMinimumBindings(context))
            {
                return;
            }

            if (context.RootRect == null)
            {
                Debug.LogWarning("[BattleDeckReplacementUI] Runtime UI를 만들려면 루트 RectTransform이 필요합니다. 가능하면 프리팹 직접 연결을 권장합니다.");
                return;
            }

            BattleDeckReplacementViewElements.StretchRect(context.RootRect);

            context.OverlayPanel = BattleDeckReplacementViewElements.CreatePanel(
                "OverlayPanel",
                context.RootTransform,
                BattleDeckReplacementUiMetrics.OverlayColor);
            BattleDeckReplacementViewElements.StretchRect(context.OverlayPanel.GetComponent<RectTransform>());

            GameObject window = BattleDeckReplacementViewElements.CreatePanel(
                "Window",
                context.OverlayPanel.transform,
                BattleDeckReplacementUiMetrics.WindowColor);
            RectTransform windowRect = window.GetComponent<RectTransform>();
            windowRect.anchorMin = new Vector2(0.5f, 0.5f);
            windowRect.anchorMax = new Vector2(0.5f, 0.5f);
            windowRect.pivot = new Vector2(0.5f, 0.5f);
            windowRect.sizeDelta = new Vector2(
                BattleDeckReplacementUiMetrics.WindowWidth,
                BattleDeckReplacementUiMetrics.WindowHeight);
            windowRect.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup windowLayout = window.AddComponent<VerticalLayoutGroup>();
            windowLayout.padding = new RectOffset(28, 28, 24, 28);
            windowLayout.spacing = 20f;
            windowLayout.childAlignment = TextAnchor.UpperCenter;
            windowLayout.childControlWidth = true;
            windowLayout.childControlHeight = false;
            windowLayout.childForceExpandWidth = true;
            windowLayout.childForceExpandHeight = false;

            context.TitleText = BattleDeckReplacementViewElements.CreateText(
                "TitleText",
                window.transform,
                42f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);
            context.TitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;

            context.SubtitleText = BattleDeckReplacementViewElements.CreateText(
                "SubtitleText",
                window.transform,
                24f,
                FontStyles.Normal,
                TextAlignmentOptions.Center);
            context.SubtitleText.color = new Color(0.88f, 0.9f, 0.94f, 0.94f);
            context.SubtitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;

            GameObject contentRow = new("ContentRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            contentRow.transform.SetParent(window.transform, false);
            contentRow.GetComponent<LayoutElement>().preferredHeight = BattleDeckReplacementUiMetrics.ContentRowHeight;

            HorizontalLayoutGroup contentLayout = contentRow.GetComponent<HorizontalLayoutGroup>();
            contentLayout.spacing = 22f;
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = true;

            context.RewardPreviewRoot = CreatePreviewColumn(contentRow.transform, "RewardColumn", "새로 얻는 카드", out _);
            context.CandidateContentRoot = CreateCandidateColumn(contentRow.transform);
            context.SelectedPreviewRoot = CreatePreviewColumn(contentRow.transform, "SelectedColumn", "교체 미리보기", out context.SelectedHeaderText);

            GameObject buttonRow = BattleDeckReplacementViewElements.CreatePanel(
                "ButtonRow",
                window.transform,
                BattleDeckReplacementUiMetrics.SectionColor);
            buttonRow.GetComponent<Image>().raycastTarget = false;
            buttonRow.AddComponent<LayoutElement>().preferredHeight = BattleDeckReplacementUiMetrics.FooterHeight;

            HorizontalLayoutGroup buttonRowLayout = buttonRow.AddComponent<HorizontalLayoutGroup>();
            buttonRowLayout.padding = new RectOffset(20, 20, 24, 24);
            buttonRowLayout.spacing = 18f;
            buttonRowLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonRowLayout.childControlWidth = false;
            buttonRowLayout.childControlHeight = false;
            buttonRowLayout.childForceExpandWidth = false;
            buttonRowLayout.childForceExpandHeight = false;

            context.ConfirmButton = BattleDeckReplacementViewElements.CreateButton("ConfirmButton", buttonRow.transform, "선택 후 확인", onConfirm);
            context.CancelButton = BattleDeckReplacementViewElements.CreateButton("CancelButton", buttonRow.transform, "취소", onCancel);
            context.OverlayPanel.SetActive(false);
        }

        /// <summary>
        /// 보상 카드/선택 카드 미리보기를 담는 고정 열 하나를 생성합니다.
        /// </summary>
        private static RectTransform CreatePreviewColumn(Transform parent, string name, string header, out TMP_Text headerText)
        {
            GameObject column = BattleDeckReplacementViewElements.CreatePanel(name, parent, BattleDeckReplacementUiMetrics.SectionColor);
            column.AddComponent<LayoutElement>().preferredWidth = 340f;

            VerticalLayoutGroup columnLayout = column.AddComponent<VerticalLayoutGroup>();
            columnLayout.padding = new RectOffset(16, 16, 16, 16);
            columnLayout.spacing = 12f;
            columnLayout.childAlignment = TextAnchor.UpperCenter;
            columnLayout.childControlWidth = true;
            columnLayout.childControlHeight = false;
            columnLayout.childForceExpandWidth = true;
            columnLayout.childForceExpandHeight = false;

            headerText = BattleDeckReplacementViewElements.CreateText(
                $"{name}Header",
                column.transform,
                28f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);
            headerText.text = header;

            GameObject previewRoot = BattleDeckReplacementViewElements.CreatePanel(
                $"{name}PreviewRoot",
                column.transform,
                BattleDeckReplacementUiMetrics.ContentColor);
            previewRoot.AddComponent<LayoutElement>().preferredHeight = 520f;

            RectTransform previewRect = previewRoot.GetComponent<RectTransform>();
            previewRect.sizeDelta = new Vector2(
                BattleDeckReplacementUiMetrics.PreviewCardWidth + 40f,
                BattleDeckReplacementUiMetrics.PreviewCardHeight + 100f);
            return previewRect;
        }

        /// <summary>
        /// 교체 후보 실제 CardView들을 가로 스크롤로 담을 컨텐츠 열을 생성합니다.
        /// </summary>
        private static RectTransform CreateCandidateColumn(Transform parent)
        {
            GameObject column = BattleDeckReplacementViewElements.CreatePanel(
                "CandidateColumn",
                parent,
                BattleDeckReplacementUiMetrics.SectionColor);
            column.AddComponent<LayoutElement>().flexibleWidth = 1f;

            VerticalLayoutGroup columnLayout = column.AddComponent<VerticalLayoutGroup>();
            columnLayout.padding = new RectOffset(16, 16, 16, 16);
            columnLayout.spacing = 12f;
            columnLayout.childAlignment = TextAnchor.UpperCenter;
            columnLayout.childControlWidth = true;
            columnLayout.childControlHeight = false;
            columnLayout.childForceExpandWidth = true;
            columnLayout.childForceExpandHeight = false;

            TMP_Text headerText = BattleDeckReplacementViewElements.CreateText(
                "CandidateHeader",
                column.transform,
                28f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);
            headerText.text = "교체 후보";

            GameObject scrollRoot = BattleDeckReplacementViewElements.CreatePanel(
                "CandidateScrollRoot",
                column.transform,
                BattleDeckReplacementUiMetrics.ContentColor);
            scrollRoot.AddComponent<LayoutElement>().preferredHeight = 500f;

            RectTransform scrollRect = scrollRoot.GetComponent<RectTransform>();
            scrollRect.sizeDelta = new Vector2(820f, 500f);

            ScrollRect scrollRectComponent = scrollRoot.AddComponent<ScrollRect>();
            scrollRectComponent.horizontal = true;
            scrollRectComponent.vertical = false;

            GameObject viewport = new("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollRoot.transform, false);
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;
            BattleDeckReplacementViewElements.StretchRect(viewport.GetComponent<RectTransform>());

            GameObject content = new("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);

            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 0.5f);
            contentRect.anchorMax = new Vector2(0f, 0.5f);
            contentRect.pivot = new Vector2(0f, 0.5f);
            contentRect.anchoredPosition = Vector2.zero;

            HorizontalLayoutGroup contentLayout = content.GetComponent<HorizontalLayoutGroup>();
            contentLayout.spacing = BattleDeckReplacementUiMetrics.CandidateSpacing;
            contentLayout.padding = new RectOffset(8, 8, 8, 8);
            contentLayout.childAlignment = TextAnchor.UpperLeft;
            contentLayout.childControlWidth = false;
            contentLayout.childControlHeight = false;
            contentLayout.childForceExpandWidth = false;
            contentLayout.childForceExpandHeight = false;

            ContentSizeFitter contentFitter = content.GetComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRectComponent.viewport = viewport.GetComponent<RectTransform>();
            scrollRectComponent.content = contentRect;
            return contentRect;
        }
    }
}
