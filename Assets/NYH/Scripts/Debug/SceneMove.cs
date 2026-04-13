using NYH.BattleCardSystem;
using NYH.CoreCardSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

// 테스트용
public class SceneMove : MonoBehaviour
{
    public void moveBattleScene()
    {
    
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
