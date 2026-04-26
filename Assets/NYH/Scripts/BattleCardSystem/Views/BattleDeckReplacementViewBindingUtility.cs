namespace NYH.BattleCardSystem
{
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 인스펙터로 연결된 참조를 검증하고, 공통 보정 작업과 버튼 연결을 처리하는 helper입니다.
    /// 메인 컨트롤러가 UI 세부 규칙까지 직접 알지 않도록 분리해둡니다.
    /// </summary>
    internal static class BattleDeckReplacementViewBindingUtility
    {
        /// <summary>
        /// 인스펙터 참조가 하나라도 연결되어 있는지 확인합니다.
        /// 하나도 없으면 runtime fallback UI를 만들어야 한다고 판단합니다.
        /// </summary>
        public static bool HasAnyAssignedReference(BattleDeckReplacementViewContext context)
        {
            return context.OverlayPanel != null
                || context.RewardPreviewRoot != null
                || context.SelectedPreviewRoot != null
                || context.CandidateContentRoot != null
                || context.TitleText != null
                || context.SubtitleText != null
                || context.SelectedHeaderText != null
                || context.LegacyTitleText != null
                || context.LegacySubtitleText != null
                || context.LegacySelectedHeaderText != null
                || context.ConfirmButton != null
                || context.CancelButton != null;
        }

        /// <summary>
        /// 직접 연결 방식으로 교체 UI를 사용하기 위해 필요한 최소 참조가 모두 있는지 검사합니다.
        /// 카드 미리보기 루트는 선택 사항이지만, 제목/후보 목록/버튼은 필수입니다.
        /// </summary>
        public static bool HasMinimumBindings(BattleDeckReplacementViewContext context)
        {
            return context.OverlayPanel != null
                && context.CandidateContentRoot != null
                && HasTextBinding(context.TitleText, context.LegacyTitleText)
                && context.ConfirmButton != null
                && context.CancelButton != null;
        }

        /// <summary>
        /// 인스펙터 연결 UI와 runtime fallback UI에 공통으로 필요한 보정 작업을 적용합니다.
        /// 후보 카드 레이아웃 보정, 버튼 콜백 연결, 초기 비활성 상태 설정이 여기서 이뤄집니다.
        /// </summary>
        public static void FinalizeBindings(
            BattleDeckReplacementViewContext context,
            UnityEngine.Events.UnityAction onConfirm,
            UnityEngine.Events.UnityAction onCancel)
        {
            if (!HasMinimumBindings(context))
            {
                return;
            }

            NormalizeExistingCanvas(context);
            EnsureCandidateContentLayout(context);
            HookButtonCallbacks(context, onConfirm, onCancel);
        }

        /// <summary>
        /// 후보 카드 Content에 이미 레이아웃이 있으면 그 설정을 그대로 존중합니다.
        /// 아무 레이아웃도 없을 때만 runtime fallback용 가로 레이아웃을 보정합니다.
        /// </summary>
        public static void EnsureCandidateContentLayout(BattleDeckReplacementViewContext context)
        {
            if (context.CandidateContentRoot == null)
            {
                return;
            }

            GridLayoutGroup gridLayout = context.CandidateContentRoot.GetComponent<GridLayoutGroup>();
            if (gridLayout != null)
            {
                gridLayout.enabled = true;
                return;
            }

            HorizontalLayoutGroup horizontalLayout = context.CandidateContentRoot.GetComponent<HorizontalLayoutGroup>();
            VerticalLayoutGroup verticalLayout = context.CandidateContentRoot.GetComponent<VerticalLayoutGroup>();
            if (horizontalLayout != null || verticalLayout != null)
            {
                return;
            }

            if (horizontalLayout == null)
            {
                horizontalLayout = context.CandidateContentRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            horizontalLayout.spacing = BattleDeckReplacementUiMetrics.CandidateSpacing;
            horizontalLayout.padding = new RectOffset(8, 8, 8, 8);
            horizontalLayout.childAlignment = TextAnchor.UpperLeft;
            horizontalLayout.childControlWidth = false;
            horizontalLayout.childControlHeight = false;
            horizontalLayout.childForceExpandWidth = false;
            horizontalLayout.childForceExpandHeight = false;

            ContentSizeFitter fitter = context.CandidateContentRoot.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = context.CandidateContentRoot.gameObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        /// <summary>
        /// TMP와 Legacy Text를 공통 방식으로 설정합니다.
        /// 컨트롤러가 어떤 텍스트 타입이 연결됐는지 일일이 알 필요가 없도록 합니다.
        /// </summary>
        public static void SetText(TMP_Text tmpText, Text legacyText, string value)
        {
            if (tmpText != null)
            {
                tmpText.text = value;
                tmpText.gameObject.SetActive(!string.IsNullOrEmpty(value));
            }

            if (legacyText != null)
            {
                legacyText.text = value;
                legacyText.gameObject.SetActive(!string.IsNullOrEmpty(value));
            }
        }

        /// <summary>
        /// 버튼 자식 텍스트가 TMP인지 Legacy Text인지에 상관없이 버튼 라벨을 갱신합니다.
        /// </summary>
        public static void SetButtonLabel(Button button, string value)
        {
            if (button == null)
            {
                return;
            }

            TMP_Text tmpLabel = button.GetComponentInChildren<TMP_Text>(true);
            if (tmpLabel != null)
            {
                tmpLabel.text = value;
                return;
            }

            Text legacyLabel = button.GetComponentInChildren<Text>(true);
            if (legacyLabel != null)
            {
                legacyLabel.text = value;
            }
        }

        private static bool HasTextBinding(TMP_Text tmpText, Text legacyText)
        {
            return tmpText != null || legacyText != null;
        }

        private static void HookButtonCallbacks(
            BattleDeckReplacementViewContext context,
            UnityEngine.Events.UnityAction onConfirm,
            UnityEngine.Events.UnityAction onCancel)
        {
            if (context.ConfirmButton != null)
            {
                context.ConfirmButton.onClick.RemoveAllListeners();
                context.ConfirmButton.onClick.AddListener(onConfirm);
            }

            if (context.CancelButton != null)
            {
                context.CancelButton.onClick.RemoveAllListeners();
                context.CancelButton.onClick.AddListener(onCancel);
            }
        }

        private static void NormalizeExistingCanvas(BattleDeckReplacementViewContext context)
        {
            Canvas nestedCanvas = context.RootTransform.GetComponentInChildren<Canvas>(true);
            if (nestedCanvas == null)
            {
                return;
            }

            if (nestedCanvas.transform.localScale.sqrMagnitude <= 0.0001f)
            {
                nestedCanvas.transform.localScale = Vector3.one;
            }
        }
    }
}
