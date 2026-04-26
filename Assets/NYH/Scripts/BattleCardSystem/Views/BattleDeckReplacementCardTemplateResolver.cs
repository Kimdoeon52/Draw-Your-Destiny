namespace NYH.BattleCardSystem
{
    using NYH.CoreCardSystem;
    using UnityEngine;

    /// <summary>
    /// 후보 카드 프리팹 참조를 실제로 생성 가능한 형태로 정리하는 helper입니다.
    ///
    /// 주로 해결하는 문제:
    /// - 프리팹 에셋을 연결한 정상 케이스
    /// - Content 안의 샘플 카드 오브젝트를 실수로 연결한 케이스
    ///
    /// 후자의 경우에는 Show 전에 목록을 비우면 원본까지 사라지므로,
    /// 숨겨진 런타임 템플릿을 따로 복제해 안전하게 사용합니다.
    /// </summary>
    internal sealed class BattleDeckReplacementCardTemplateResolver
    {
        private CardView runtimeCandidateCardTemplate;
        private CardView runtimeCandidateCardTemplateSource;

        /// <summary>
        /// 현재 context와 연결 상태를 기준으로 실제 사용할 후보 카드 템플릿을 반환합니다.
        /// </summary>
        public CardView Resolve(
            BattleDeckReplacementViewContext context,
            Transform runtimeTemplateParent)
        {
            CardView assignedPrefab = context.CandidateCardViewPrefab;
            if (assignedPrefab == null)
            {
                return runtimeCandidateCardTemplate;
            }

            if (!IsSceneTemplateInsideCandidateContent(context, assignedPrefab))
            {
                ClearRuntimeCandidateTemplateCache();
                return assignedPrefab;
            }

            if (runtimeCandidateCardTemplate != null && runtimeCandidateCardTemplateSource == assignedPrefab)
            {
                return runtimeCandidateCardTemplate;
            }

            ClearRuntimeCandidateTemplateCache();

            runtimeCandidateCardTemplate = Object.Instantiate(assignedPrefab, runtimeTemplateParent);
            runtimeCandidateCardTemplateSource = assignedPrefab;
            runtimeCandidateCardTemplate.gameObject.name = $"{assignedPrefab.gameObject.name}_RuntimeTemplate";
            runtimeCandidateCardTemplate.gameObject.SetActive(false);
            runtimeCandidateCardTemplate.transform.SetParent(runtimeTemplateParent, false);
            return runtimeCandidateCardTemplate;
        }

        /// <summary>
        /// 임시 복제해 둔 런타임 템플릿을 제거합니다.
        /// </summary>
        public void ClearRuntimeCandidateTemplateCache()
        {
            if (runtimeCandidateCardTemplate != null)
            {
                Object.Destroy(runtimeCandidateCardTemplate.gameObject);
            }

            runtimeCandidateCardTemplate = null;
            runtimeCandidateCardTemplateSource = null;
        }

        /// <summary>
        /// 연결된 프리팹 참조가 프리팹 에셋이 아니라
        /// 현재 Content 아래에 놓인 씬 오브젝트인지 검사합니다.
        /// </summary>
        private static bool IsSceneTemplateInsideCandidateContent(
            BattleDeckReplacementViewContext context,
            CardView candidatePrefab)
        {
            if (context.CandidateContentRoot == null || candidatePrefab == null)
            {
                return false;
            }

            if (!candidatePrefab.gameObject.scene.IsValid())
            {
                return false;
            }

            return candidatePrefab.transform.IsChildOf(context.CandidateContentRoot);
        }
    }
}
