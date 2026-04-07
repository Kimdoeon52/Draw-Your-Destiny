namespace NYH.CoreCardSystem
{
    using UnityEngine;

    /*
     * CardViewRefreshService
     *
     * - 카드 UI 새로고침 전담
     *
     * 역할:
     * - 카드 뷰 UI를 갱신하는 책임만 담당합니다.
     * - 규칙/액션 처리 코드가 CardView 탐색과 Setup 호출까지 직접 알지 않게 분리합니다.
     *
     * 여기에 넣는 것:
     * - 현재 씬의 CardView 탐색
     * - hover preview가 아닌 실제 카드 UI 갱신
     *
     * 여기에 넣지 않는 것:
     * - 카드 게임 규칙
     * - 액션 등록
     * - 자원 변경
     *
     * 사용하는 법:
     * - _CardSystem이 한 번 생성해 performer들에게 콜백으로 넘깁니다.
     * - 코스트/modifier 변경 후 RefreshVisibleCardViews()를 호출하면 됩니다.
     */
    public class CardViewRefreshService
    {
        public void RefreshVisibleCardViews()
        {
            CardView[] allViews = Object.FindObjectsByType<CardView>(FindObjectsSortMode.None);
            foreach (var view in allViews)
            {
                if (view != null && view.Card != null && !view.IsHoverPreview)
                {
                    view.Setup(view.Card);
                }
            }
        }
    }
}
