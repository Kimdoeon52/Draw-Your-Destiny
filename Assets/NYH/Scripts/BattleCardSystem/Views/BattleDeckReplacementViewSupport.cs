namespace NYH.BattleCardSystem
{
    using TMPro;
    using NYH.CoreCardSystem;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 교체 UI 컨트롤러와 helper들이 공유해서 쓰는 참조 묶음입니다.
    /// 인스펙터 필드를 컨텍스트 객체로 복사해 helper에 넘기고,
    /// helper가 보정한 값을 다시 컨트롤러 필드로 반영할 때 사용합니다.
    /// </summary>
    internal sealed class BattleDeckReplacementViewContext
    {
        public BattleDeckReplacementViewContext(Transform rootTransform, RectTransform rootRect)
        {
            RootTransform = rootTransform;
            RootRect = rootRect;
        }

        public Transform RootTransform { get; }
        public RectTransform RootRect { get; }

        public GameObject OverlayPanel;
        public RectTransform RewardPreviewRoot;
        public RectTransform SelectedPreviewRoot;
        public RectTransform CandidateContentRoot;
        public TMP_Text TitleText;
        public TMP_Text SubtitleText;
        public TMP_Text SelectedHeaderText;
        public Text LegacyTitleText;
        public Text LegacySubtitleText;
        public Text LegacySelectedHeaderText;
        public Button ConfirmButton;
        public Button CancelButton;
        public CardView CandidateCardViewPrefab;
    }

    /// <summary>
    /// 교체 화면과 helper들이 함께 쓰는 공통 크기/색상 상수입니다.
    /// 숫자와 색을 한곳에 모아 UI 조정 지점을 명확하게 유지합니다.
    /// </summary>
    internal static class BattleDeckReplacementUiMetrics
    {
        public const float CandidatePadding = 18f;
        public const float PreviewCardWidth = 244f;
        public const float PreviewCardHeight = 380f;
        public const float CandidateSpacing = 16f;
        public const float WindowWidth = 1600f;
        public const float WindowHeight = 940f;
        public const float ContentRowHeight = 560f;
        public const float FooterHeight = 112f;

        public static readonly Color OverlayColor = new(0f, 0f, 0f, 0.72f);
        public static readonly Color WindowColor = new(0.11f, 0.12f, 0.15f, 0.97f);
        public static readonly Color SectionColor = new(0.17f, 0.18f, 0.22f, 1f);
        public static readonly Color ContentColor = new(0.14f, 0.15f, 0.18f, 1f);
        public static readonly Color ButtonColor = new(0.25f, 0.28f, 0.34f, 1f);
        public static readonly Color CandidateDefaultColor = new(1f, 1f, 1f, 0.02f);
        public static readonly Color CandidateSelectedColor = new(0.96f, 0.82f, 0.29f, 0.14f);
        public static readonly Color CandidateOutlineColor = new(0.98f, 0.84f, 0.31f, 0.95f);
    }

    /// <summary>
    /// 교체 화면에서 공통으로 쓰는 UI 오브젝트를 생성하는 작은 팩토리입니다.
    /// runtime fallback 레이아웃과 미리보기 helper가 함께 사용합니다.
    /// </summary>
    internal static class BattleDeckReplacementViewElements
    {
        public static TMP_Text CreateText(
            string name,
            Transform parent,
            float fontSize,
            FontStyles fontStyle,
            TextAlignmentOptions alignment)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            TMP_Text text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = new Color(0.94f, 0.95f, 0.99f, 1f);
            return text;
        }

        public static Button CreateButton(string name, Transform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObject = CreatePanel(name, parent, BattleDeckReplacementUiMetrics.ButtonColor);
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(240f, 64f);

            LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 240f;
            layout.preferredHeight = 64f;

            Button button = buttonObject.AddComponent<Button>();
            button.onClick.AddListener(onClick);

            TMP_Text labelText = CreateText("Label", buttonObject.transform, 24f, FontStyles.Bold, TextAlignmentOptions.Center);
            labelText.text = label;

            RectTransform labelRect = labelText.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            return button;
        }

        public static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            GameObject panel = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            panel.GetComponent<Image>().color = color;
            return panel;
        }

        public static void StretchRect(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
