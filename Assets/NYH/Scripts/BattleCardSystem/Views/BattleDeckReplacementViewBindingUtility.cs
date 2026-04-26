namespace NYH.BattleCardSystem
{
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 인스펙터에서 연결된 교체 UI 참조를 검사하고,
    /// 공통 보정 작업을 수행하는 정적 helper입니다.
    ///
    /// 담당 범위:
    /// - 필수 참조가 있는지 검사
    /// - 후보 카드 Content의 레이아웃 보정
    /// - 확인/취소 버튼 콜백 재연결
    /// - TMP / Legacy Text 공통 처리
    ///
    /// 담당하지 않는 것:
    /// - 실제 카드 생성
    /// - 선택 상태 저장
    /// - 창 열기/닫기 흐름 제어
    /// </summary>
    internal static class BattleDeckReplacementViewBindingUtility
    {
        /// <summary>
        /// 직렬화된 참조가 하나라도 연결되어 있는지 검사합니다.
        /// 전부 비어 있으면 "인스펙터 기반 UI가 없다"고 판단하고
        /// 런타임 fallback UI 생성 경로를 타게 됩니다.
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
        /// 직접 연결 방식으로 UI를 쓸 때 꼭 필요한 최소 참조가 모두 있는지 검사합니다.
        /// 보상/선택 미리보기 루트는 선택 사항이지만,
        /// 오버레이 패널, 후보 목록 Content, 제목 텍스트, 확인/취소 버튼은 필수입니다.
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
        /// 인스펙터 연결 UI와 런타임 fallback UI 모두에 공통으로 적용되는 마무리 작업입니다.
        /// 이 메서드는 "참조를 정리하고 버튼을 다시 연결하는 것"까지만 담당합니다.
        /// 창을 여닫는 책임은 BattleDeckReplacementUI 쪽에 남겨 둡니다.
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
        /// 후보 카드 Content에 이미 레이아웃 컴포넌트가 있으면 그대로 존중합니다.
        /// 지금 프로젝트에서는 GridLayoutGroup을 직접 세팅해서 쓰는 경우가 많으므로
        /// GridLayoutGroup이 있으면 건드리지 않고 유지합니다.
        ///
        /// 아무 레이아웃도 없을 때만 fallback으로 HorizontalLayoutGroup을 추가합니다.
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
        /// TMP_Text와 Legacy Text를 구분하지 않고 공통으로 문자열을 적용합니다.
        /// 값이 비어 있으면 해당 텍스트 오브젝트를 숨겨서 빈 영역이 덜 거슬리게 합니다.
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
        /// 버튼 라벨을 TMP/Legacy 구분 없이 갱신합니다.
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

        /// <summary>
        /// 버튼 콜백을 매번 현재 UI 흐름 기준으로 다시 연결합니다.
        /// 선택 UI는 Show가 여러 번 호출될 수 있으므로
        /// 기존 리스너를 제거하고 현재 콜백만 다시 붙입니다.
        /// </summary>
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

        /// <summary>
        /// 기존 프리팹/UI 루트 안에 중첩 Canvas가 비정상적으로 축소되어 있을 때
        /// 최소한의 안전장치로 scale을 1로 되돌립니다.
        /// </summary>
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
