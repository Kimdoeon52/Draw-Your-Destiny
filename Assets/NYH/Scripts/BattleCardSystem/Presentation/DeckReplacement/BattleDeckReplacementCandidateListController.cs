namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;
    using NYH.CoreCardSystem;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 교체 후보 카드 목록의 생성, 선택 표시, 정리를 담당하는 전용 helper입니다.
    ///
    /// 이 클래스가 담당하는 일:
    /// - Content 아래 후보 카드 생성
    /// - 클릭 핸들러 부착
    /// - 선택 프레임 생성과 토글
    /// - 후보 목록 전체 정리
    ///
    /// 이 클래스가 담당하지 않는 일:
    /// - 어떤 카드가 최종 확정되었는지 판단
    /// - 확인 / 취소 버튼 처리
    /// - 좌우 미리보기 갱신
    /// </summary>
    internal sealed class BattleDeckReplacementCandidateListController
    {
        private readonly List<CandidateView> candidateViews = new();

        /// <summary>
        /// 후보 카드 하나를 Content의 직접 자식으로 생성하고,
        /// 선택 가능한 카드로 동작하도록 필요한 컴포넌트를 붙입니다.
        /// </summary>
        public void CreateCandidate(
            BattleDeckReplacementUI owner,
            BattleDeckReplacementPreviewFactory previewFactory,
            BattleDeckReplacementViewContext context,
            BattleCardData candidate,
            int index)
        {
            if (owner == null || previewFactory == null || context == null || candidate == null)
            {
                return;
            }

            GameObject candidateObject;
            CardView cardView = previewFactory.CreateCandidateCardView(context.CandidateContentRoot, candidate);
            if (cardView != null)
            {
                candidateObject = cardView.gameObject;
            }
            else
            {
                candidateObject = previewFactory.RenderFallbackCandidate(context.CandidateContentRoot, candidate);
            }

            if (candidateObject == null)
            {
                return;
            }

            candidateObject.name = $"Candidate_{index}_{candidate.CardName}";

            RectTransform rectTransform = candidateObject.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
            }

            LayoutElement layout = candidateObject.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = candidateObject.AddComponent<LayoutElement>();
            }

            Vector2 visualSize = ResolveVisualSize(rectTransform);
            layout.preferredWidth = visualSize.x;
            layout.preferredHeight = visualSize.y;

            BattleDeckReplacementCandidateClickHandler clickHandler =
                candidateObject.GetComponent<BattleDeckReplacementCandidateClickHandler>();
            if (clickHandler == null)
            {
                clickHandler = candidateObject.AddComponent<BattleDeckReplacementCandidateClickHandler>();
            }

            clickHandler.Setup(owner, candidate, index);

            BattleDeckReplacementSelectionFrame selectionFrame = EnsureSelectionFrame(candidateObject.transform);
            candidateViews.Add(new CandidateView(index, selectionFrame));
        }

        /// <summary>
        /// 현재 선택된 후보 인덱스를 기준으로 선택 표시를 갱신합니다.
        /// 같은 카드 데이터가 여러 장 있어도, 인덱스로 구분해 한 장만 강조합니다.
        /// </summary>
        public void ApplySelection(int selectedIndex)
        {
            foreach (CandidateView candidateView in candidateViews)
            {
                bool isSelected = candidateView.Index == selectedIndex;
                candidateView.SelectionFrame?.SetSelected(isSelected);
            }
        }

        /// <summary>
        /// 후보 카드 목록과 선택 상태 캐시를 모두 지웁니다.
        /// </summary>
        public void Clear(RectTransform contentRoot)
        {
            candidateViews.Clear();

            if (contentRoot == null)
            {
                return;
            }

            for (int i = contentRoot.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(contentRoot.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// 카드 루트의 실제 표시 크기를 추정합니다.
        /// 프리팹마다 RectTransform 값이 다를 수 있어 여러 fallback 순서를 사용합니다.
        /// </summary>
        private static Vector2 ResolveVisualSize(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return new Vector2(
                    BattleDeckReplacementUiMetrics.PreviewCardWidth,
                    BattleDeckReplacementUiMetrics.PreviewCardHeight);
            }

            Vector2 rectSize = rectTransform.rect.size;
            if (rectSize.x > 0f && rectSize.y > 0f)
            {
                return rectSize;
            }

            Vector2 sizeDelta = rectTransform.sizeDelta;
            if (sizeDelta.x > 0f && sizeDelta.y > 0f)
            {
                return sizeDelta;
            }

            BoxCollider boxCollider = rectTransform.GetComponent<BoxCollider>();
            if (boxCollider != null && boxCollider.size.x > 0f && boxCollider.size.y > 0f)
            {
                return new Vector2(boxCollider.size.x, boxCollider.size.y);
            }

            BoxCollider2D boxCollider2D = rectTransform.GetComponent<BoxCollider2D>();
            if (boxCollider2D != null && boxCollider2D.size.x > 0f && boxCollider2D.size.y > 0f)
            {
                return boxCollider2D.size;
            }

            return new Vector2(
                BattleDeckReplacementUiMetrics.PreviewCardWidth,
                BattleDeckReplacementUiMetrics.PreviewCardHeight);
        }

        /// <summary>
        /// 카드 루트에 선택 프레임을 보장합니다.
        /// 이미 있으면 재사용하고, 없으면 새로 생성합니다.
        /// </summary>
        private static BattleDeckReplacementSelectionFrame EnsureSelectionFrame(Transform cardRoot)
        {
            BattleDeckReplacementSelectionFrame existingFrame =
                cardRoot.GetComponentInChildren<BattleDeckReplacementSelectionFrame>(true);
            if (existingFrame != null)
            {
                existingFrame.SetSelected(false);
                return existingFrame;
            }

            GameObject frameObject = new(
                "SelectionFrame",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Outline),
                typeof(BattleDeckReplacementSelectionFrame));
            frameObject.transform.SetParent(cardRoot, false);

            RectTransform frameRect = frameObject.GetComponent<RectTransform>();
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.offsetMin = new Vector2(-8f, -8f);
            frameRect.offsetMax = new Vector2(8f, 8f);

            Image frameImage = frameObject.GetComponent<Image>();
            frameImage.color = BattleDeckReplacementUiMetrics.CandidateSelectedColor;
            frameImage.raycastTarget = false;

            Outline outline = frameObject.GetComponent<Outline>();
            outline.effectDistance = new Vector2(6f, 6f);
            outline.effectColor = BattleDeckReplacementUiMetrics.CandidateOutlineColor;

            BattleDeckReplacementSelectionFrame selectionFrame =
                frameObject.GetComponent<BattleDeckReplacementSelectionFrame>();
            selectionFrame.Initialize(frameImage, outline);
            selectionFrame.SetSelected(false);
            return selectionFrame;
        }

        /// <summary>
        /// 후보 카드 한 장에 대한 선택 표시 캐시입니다.
        /// </summary>
        private sealed class CandidateView
        {
            public CandidateView(int index, BattleDeckReplacementSelectionFrame selectionFrame)
            {
                Index = index;
                SelectionFrame = selectionFrame;
            }

            public int Index { get; }

            public BattleDeckReplacementSelectionFrame SelectionFrame { get; }
        }
    }
}
