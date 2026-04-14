using NYH.BattleCardSystem;
using NYH.CoreCardSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

// 테스트용
public class SceneMove : MonoBehaviour
{
    public void moveBattleScene()
    {
        BattleSessionController battleSession = FindFirstObjectByType<BattleSessionController>(FindObjectsInactive.Include);
        if (battleSession != null)
        {
            Debug.Log("[SceneMove] BattleSessionController를 사용해 같은 씬에서 전투 모드로 전환합니다.");
            battleSession.EnterBattle();
            return;
        }

        if (BattleDeckCollection.Instance != null)
        {
            Debug.Log($"[SceneMove] 배틀 덱 상태: baseDeck={BattleDeckCollection.Instance.BaseBattleDeck.Count}, earned={BattleDeckCollection.Instance.EarnedBattleCards.Count}");
        }

        if (CardSystem.Instance != null)
        {
            CivilizationDeckStateStore stateStore = CivilizationDeckStateStore.GetOrCreate();
            CardPileRuntimeState runtimeState = CardSystem.Instance.CaptureRuntimeState();
            Debug.Log($"[SceneMove] 전투 진입 직전 문명 덱 상태: {FormatState(runtimeState)}");
            stateStore.Store(runtimeState);
            Debug.Log("[SceneMove] 문명 덱 런타임 상태 저장 완료");
        }
        else
        {
            Debug.LogWarning("[SceneMove] CardSystem.Instance가 없어 문명 덱 상태를 저장하지 못했습니다.");
        }

        Debug.Log("[SceneMove] NYH3 배틀 씬 로드 시작");
        SceneManager.LoadScene("NYH3");
    }
    public void moveMainScene()
    {
        BattleSessionController battleSession = FindFirstObjectByType<BattleSessionController>(FindObjectsInactive.Include);
        if (battleSession != null && battleSession.IsBattleActive)
        {
            Debug.Log("[SceneMove] BattleSessionController를 사용해 문명 모드로 복귀합니다.");
            battleSession.ExitBattle();
            return;
        }

        SceneManager.LoadScene("NYH4");
    }

    private static string FormatState(CardPileRuntimeState state)
    {
        if (state == null)
        {
            return "state=NULL";
        }

        int draw = state.DrawPile != null ? state.DrawPile.Count : 0;
        int hand = state.Hand != null ? state.Hand.Count : 0;
        int discard = state.DiscardPile != null ? state.DiscardPile.Count : 0;
        int extinction = state.ExtinctionPile != null ? state.ExtinctionPile.Count : 0;
        int total = draw + hand + discard + extinction;

        return $"draw={draw}, hand={hand}, discard={discard}, extinction={extinction}, total={total}";
    }
}
