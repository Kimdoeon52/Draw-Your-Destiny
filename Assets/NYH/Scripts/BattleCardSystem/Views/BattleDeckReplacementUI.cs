namespace NYH.BattleCardSystem
{
    using System;
    using System.Collections.Generic;
    using TMPro;
    using NYH.CoreCardSystem;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 전투 덱 교체 화면의 메인 흐름만 담당하는 컨트롤러입니다.
    ///
    /// 이 클래스의 핵심 책임:
    /// - 교체 화면 열기 / 닫기
    /// - 현재 선택 카드 기억
    /// - 확인 / 취소 버튼 흐름 처리
    /// - 좌우 미리보기 갱신
    ///
    /// 이미 분리된 책임:
    /// - 후보 카드 슬롯 생성 / 정리 / 선택 프레임: BattleDeckReplacementCandidateListController
    /// - 후보 카드 프리팹 참조 해석: BattleDeckReplacementCardTemplateResolver
    /// - 런타임 인스턴스 탐색 / 생성: BattleDeckReplacementBootstrapper
    /// - 카드 시각 요소 생성: BattleDeckReplacementPreviewFactory
    /// - 인스펙터 참조 검증 / 버튼 연결 / 레이아웃 보정: BattleDeckReplacementViewBindingUtility
    /// </summary>
    public class BattleDeckReplacementUI : MonoBehaviour
    {
        public static BattleDeckReplacementUI Instance { get; private set; }

        /// <summary>
        /// 후보 카드 목록 생성과 선택 프레임 표시를 담당하는 helper입니다.
        /// 본체는 선택 인덱스만 넘기고, 슬롯 생성 세부 구현은 이쪽에 맡깁니다.
        /// </summary>
        private readonly BattleDeckReplacementCandidateListController candidateListController = new();

        /// <summary>
        /// 후보 카드 프리팹 참조가 프리팹 에셋인지, 씬 샘플 카드인지 판단해
        /// 실제 생성 가능한 템플릿으로 정리해 주는 helper입니다.
        /// </summary>
        private readonly BattleDeckReplacementCardTemplateResolver candidateTemplateResolver = new();

        [Header("화면 루트")]
        [Tooltip("교체 화면 전체를 켜고 끄는 최상위 패널입니다.")]
        [SerializeField] private GameObject overlayPanel;

        [Tooltip("새로 얻는 전투 카드 미리보기를 그릴 위치입니다.")]
        [SerializeField] private RectTransform rewardPreviewRoot;

        [Tooltip("현재 선택한 교체 대상 카드 미리보기를 그릴 위치입니다.")]
        [SerializeField] private RectTransform selectedPreviewRoot;

        [Tooltip("교체 후보 카드들을 동적으로 생성할 Content 루트입니다. 보통 Scroll View > Viewport > Content를 연결합니다.")]
        [SerializeField] private RectTransform candidateContentRoot;

        [Header("텍스트 (TMP 권장)")]
        [Tooltip("화면 상단의 메인 제목 텍스트입니다.")]
        [SerializeField] private TMP_Text titleText;

        [Tooltip("화면 상단의 보조 설명 텍스트입니다.")]
        [SerializeField] private TMP_Text subtitleText;

        [Tooltip("우측 선택 카드 미리보기 영역의 제목 텍스트입니다.")]
        [SerializeField] private TMP_Text selectedHeaderText;

        [Header("레거시 UI Text 대체용")]
        [Tooltip("TMP 대신 기본 UI Text를 쓰는 경우 메인 제목 텍스트를 연결합니다.")]
        [SerializeField] private Text legacyTitleText;

        [Tooltip("TMP 대신 기본 UI Text를 쓰는 경우 보조 설명 텍스트를 연결합니다.")]
        [SerializeField] private Text legacySubtitleText;

        [Tooltip("TMP 대신 기본 UI Text를 쓰는 경우 우측 미리보기 제목 텍스트를 연결합니다.")]
        [SerializeField] private Text legacySelectedHeaderText;

        [Header("버튼")]
        [Tooltip("현재 선택한 후보 카드를 실제 교체 대상으로 확정하는 버튼입니다.")]
        [SerializeField] private Button confirmButton;

        [Tooltip("교체를 취소하고 보상을 받지 않은 채 넘어가는 버튼입니다.")]
        [SerializeField] private Button cancelButton;

        [Header("후보 카드 프리팹")]
        [Tooltip("후보 카드 목록에 사용할 CardView 프리팹입니다. BattleCardViewMini.prefab을 직접 연결합니다.")]
        [SerializeField] private CardView candidateCardViewPrefab;

        /// <summary>
        /// 현재 선택된 교체 대상 카드 데이터입니다.
        /// 카드 클릭 시 값만 바뀌고, 실제 확정은 확인 버튼을 눌렀을 때만 일어납니다.
        /// </summary>
        private BattleCardData selectedCandidate;

        /// <summary>
        /// "선택 후 확인"을 눌렀을 때 상위 시스템으로 선택 결과를 돌려주는 콜백입니다.
        /// </summary>
        private Action<BattleCardData> onConfirmed;

        /// <summary>
        /// 취소하거나 창을 강제로 닫았을 때 호출되는 콜백입니다.
        /// </summary>
        private Action onCanceled;

        /// <summary>
        /// 현재 교체 화면이 열려 있는지 빠르게 확인할 때 쓰는 프로퍼티입니다.
        /// </summary>
        public bool IsOpen => overlayPanel != null && overlayPanel.activeSelf;

        /// <summary>
        /// 기존 교체 UI를 찾거나, 없으면 하나를 새로 준비해서 반환합니다.
        /// 실제 탐색/생성 세부 구현은 부트스트랩 helper에 위임합니다.
        /// </summary>
        public static BattleDeckReplacementUI GetOrCreate()
        {
            return BattleDeckReplacementBootstrapper.GetOrCreate(Instance);
        }

        /// <summary>
        /// 싱글톤 등록과 기본 UI 준비를 담당합니다.
        /// 이 시점에서는 화면만 숨겨 두고, 실제 표시는 Show(...)가 담당합니다.
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            EnsureViewReady(out BattleDeckReplacementViewContext context);
            if (context.OverlayPanel != null)
            {
                context.OverlayPanel.SetActive(false);
            }

            ApplyViewContext(context);
        }

        private void OnDestroy()
        {
            candidateTemplateResolver.ClearRuntimeCandidateTemplateCache();

            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// 교체 화면을 열고 현재 보상 상황을 화면에 반영합니다.
        ///
        /// 여기서 하는 일:
        /// - 선택 상태 초기화
        /// - 버튼과 제목 문구 갱신
        /// - 좌측 보상 카드 미리보기 표시
        /// - 우측 선택 카드 미리보기 placeholder 표시
        /// - 후보 카드 목록 생성
        /// - 레이아웃 즉시 재계산
        ///
        /// 여기서 하지 않는 일:
        /// - 실제 덱 교체
        /// - 보상 확정
        /// - 카드 드로우 실행
        /// </summary>
        public void Show(
            BattleCardData rewardCard,
            IReadOnlyList<BattleCardData> candidates,
            Action<BattleCardData> onConfirmed,
            Action onCanceled)
        {
            BattleDeckReplacementPreviewFactory previewFactory = EnsureViewReady(out BattleDeckReplacementViewContext context);
            if (!BattleDeckReplacementViewBindingUtility.HasMinimumBindings(context))
            {
                Debug.LogWarning("[BattleDeckReplacementUI] 필수 UI 참조가 빠져 있어 교체 화면을 열 수 없습니다.");
                onCanceled?.Invoke();
                return;
            }

            context.CandidateCardViewPrefab = candidateTemplateResolver.Resolve(context, transform);
            if (context.CandidateCardViewPrefab == null)
            {
                Debug.LogWarning("[BattleDeckReplacementUI] 후보 카드 프리팹이 연결되지 않았습니다. BattleCardViewMini.prefab을 직접 연결해 주세요.");
                onCanceled?.Invoke();
                return;
            }

            this.onConfirmed = onConfirmed;
            this.onCanceled = onCanceled;
            selectedCandidate = null;

            context.ConfirmButton.interactable = false;
            BattleDeckReplacementViewBindingUtility.SetButtonLabel(context.ConfirmButton, "선택 후 확인");
            BattleDeckReplacementViewBindingUtility.SetButtonLabel(context.CancelButton, "취소");
            BattleDeckReplacementViewBindingUtility.SetText(
                context.TitleText,
                context.LegacyTitleText,
                "교체할 카드");
            BattleDeckReplacementViewBindingUtility.SetText(
                context.SubtitleText,
                context.LegacySubtitleText,
                rewardCard != null ? $"새 보상: {rewardCard.CardName}" : string.Empty);
            BattleDeckReplacementViewBindingUtility.SetText(
                context.SelectedHeaderText,
                context.LegacySelectedHeaderText,
                "교체 미리보기");

            previewFactory.Clear(context.RewardPreviewRoot);
            previewFactory.Clear(context.SelectedPreviewRoot);
            candidateListController.Clear(context.CandidateContentRoot);

            if (context.RewardPreviewRoot != null)
            {
                previewFactory.RenderPrimaryPreview(context.RewardPreviewRoot, rewardCard, "새로 얻는 카드");
            }

            if (context.SelectedPreviewRoot != null)
            {
                previewFactory.RenderPlaceholder(context.SelectedPreviewRoot, "교체할 카드를 선택하세요.");
            }

            if (candidates != null)
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    BattleCardData candidate = candidates[i];
                    if (candidate != null)
                    {
                        candidateListController.CreateCandidate(this, previewFactory, context, candidate, i);
                    }
                }
            }

            ForceRebuildLayout(context);

            context.OverlayPanel.SetActive(true);
            ApplyViewContext(context);
        }

        /// <summary>
        /// 외부에서 창을 강제로 닫고 싶을 때 사용하는 메서드입니다.
        /// 의미상으로는 취소와 동일하게 처리합니다.
        /// </summary>
        public void Close()
        {
            Dismiss(invokeCancel: true);
        }

        /// <summary>
        /// 후보 카드 클릭 시 호출됩니다.
        ///
        /// 중요:
        /// 이 메서드는 "선택 상태만" 바꾸고, 창을 닫지 않습니다.
        /// 실제 교체 확정은 HandleConfirmClicked()에서만 일어납니다.
        /// </summary>
        internal void HandleCandidateCardClicked(BattleCardData candidate, int candidateIndex)
        {
            BattleDeckReplacementPreviewFactory previewFactory = EnsureViewReady(out BattleDeckReplacementViewContext context);

            selectedCandidate = candidate;
            context.ConfirmButton.interactable = selectedCandidate != null;
            candidateListController.ApplySelection(candidateIndex);

            BattleDeckReplacementViewBindingUtility.SetText(
                context.SelectedHeaderText,
                context.LegacySelectedHeaderText,
                candidate != null ? $"교체 대상: {candidate.CardName}" : "교체 미리보기");

            if (context.SelectedPreviewRoot != null)
            {
                previewFactory.Clear(context.SelectedPreviewRoot);
                previewFactory.RenderPrimaryPreview(context.SelectedPreviewRoot, candidate, "선택한 카드");
            }

            ForceRebuildLayout(context);
            ApplyViewContext(context);
        }

        /// <summary>
        /// "선택 후 확인" 버튼에서만 실제 선택 결과를 상위 시스템으로 전달합니다.
        /// 후보 카드를 클릭했다고 바로 교체되면 안 되므로, 이 메서드만 확정 경로로 둡니다.
        /// </summary>
        private void HandleConfirmClicked()
        {
            if (selectedCandidate == null)
            {
                return;
            }

            Action<BattleCardData> confirmCallback = onConfirmed;
            BattleCardData confirmedCandidate = selectedCandidate;
            Dismiss(invokeCancel: false);
            confirmCallback?.Invoke(confirmedCandidate);
        }

        /// <summary>
        /// 취소 버튼은 보상 미지급 경로로 종료합니다.
        /// 실제 교체 결과는 전달하지 않습니다.
        /// </summary>
        private void HandleCancelClicked()
        {
            Dismiss(invokeCancel: true);
        }

        /// <summary>
        /// 현재 화면을 닫고 내부 상태를 정리합니다.
        /// invokeCancel이 true인 경우에만 취소 콜백을 호출합니다.
        /// </summary>
        private void Dismiss(bool invokeCancel)
        {
            BattleDeckReplacementPreviewFactory previewFactory = EnsureViewReady(out BattleDeckReplacementViewContext context);
            if (context.OverlayPanel == null)
            {
                return;
            }

            context.OverlayPanel.SetActive(false);
            previewFactory.Clear(context.RewardPreviewRoot);
            previewFactory.Clear(context.SelectedPreviewRoot);
            candidateListController.Clear(context.CandidateContentRoot);
            selectedCandidate = null;

            if (context.ConfirmButton != null)
            {
                context.ConfirmButton.interactable = false;
            }

            Action cancelCallback = onCanceled;
            onConfirmed = null;
            onCanceled = null;

            ApplyViewContext(context);
            CardViewHoverSystem.Instance?.Hide();

            if (invokeCancel)
            {
                cancelCallback?.Invoke();
            }
        }

        /// <summary>
        /// 현재 직렬화 필드를 기준으로 작업용 Context를 만들고,
        /// 필요하면 fallback UI까지 준비한 뒤 공용 PreviewFactory를 반환합니다.
        ///
        /// 역할 분담:
        /// - UI 참조 정리와 버튼 재연결: ViewBindingUtility
        /// - fallback UI 생성: RuntimeLayoutBuilder
        /// - 카드 프리뷰 생성: PreviewFactory
        /// - 실제 선택/확인/닫기 흐름 제어: 이 클래스
        /// </summary>
        private BattleDeckReplacementPreviewFactory EnsureViewReady(out BattleDeckReplacementViewContext context)
        {
            context = CreateViewContext();
            if (!BattleDeckReplacementViewBindingUtility.HasAnyAssignedReference(context))
            {
                BattleDeckReplacementRuntimeLayoutBuilder.BuildIfNeeded(context, HandleConfirmClicked, HandleCancelClicked);
            }

            BattleDeckReplacementViewBindingUtility.FinalizeBindings(context, HandleConfirmClicked, HandleCancelClicked);
            ApplyViewContext(context);
            return new BattleDeckReplacementPreviewFactory(context);
        }

        /// <summary>
        /// 현재 인스펙터 필드 상태를 helper들이 다루기 쉬운 Context 객체로 복사합니다.
        /// </summary>
        private BattleDeckReplacementViewContext CreateViewContext()
        {
            return new BattleDeckReplacementViewContext(transform, GetComponent<RectTransform>())
            {
                OverlayPanel = overlayPanel,
                RewardPreviewRoot = rewardPreviewRoot,
                SelectedPreviewRoot = selectedPreviewRoot,
                CandidateContentRoot = candidateContentRoot,
                TitleText = titleText,
                SubtitleText = subtitleText,
                SelectedHeaderText = selectedHeaderText,
                LegacyTitleText = legacyTitleText,
                LegacySubtitleText = legacySubtitleText,
                LegacySelectedHeaderText = legacySelectedHeaderText,
                ConfirmButton = confirmButton,
                CancelButton = cancelButton,
                CandidateCardViewPrefab = candidateCardViewPrefab,
            };
        }

        /// <summary>
        /// helper들이 보정한 Context 값을 다시 직렬화 필드에 반영합니다.
        /// candidateCardViewPrefab은 런타임 템플릿으로 덮어쓰지 않기 위해
        /// 의도적으로 여기서 다시 대입하지 않습니다.
        /// </summary>
        private void ApplyViewContext(BattleDeckReplacementViewContext context)
        {
            overlayPanel = context.OverlayPanel;
            rewardPreviewRoot = context.RewardPreviewRoot;
            selectedPreviewRoot = context.SelectedPreviewRoot;
            candidateContentRoot = context.CandidateContentRoot;
            titleText = context.TitleText;
            subtitleText = context.SubtitleText;
            selectedHeaderText = context.SelectedHeaderText;
            legacyTitleText = context.LegacyTitleText;
            legacySubtitleText = context.LegacySubtitleText;
            legacySelectedHeaderText = context.LegacySelectedHeaderText;
            confirmButton = context.ConfirmButton;
            cancelButton = context.CancelButton;
        }

        /// <summary>
        /// 후보 영역과 미리보기 영역의 레이아웃을 즉시 다시 계산합니다.
        /// 동적 생성 직후 Scroll View / Grid 안에서 위치가 늦게 튀는 현상을 줄이기 위한 단계입니다.
        /// </summary>
        private static void ForceRebuildLayout(BattleDeckReplacementViewContext context)
        {
            if (context.CandidateContentRoot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(context.CandidateContentRoot);
            }

            if (context.RewardPreviewRoot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(context.RewardPreviewRoot);
            }

            if (context.SelectedPreviewRoot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(context.SelectedPreviewRoot);
            }

            Canvas.ForceUpdateCanvases();
        }
    }
}
