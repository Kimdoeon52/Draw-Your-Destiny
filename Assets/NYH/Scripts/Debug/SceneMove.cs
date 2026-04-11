using NYH.BattleCardSystem;
using NYH.CoreCardSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

// 테스트용
public class SceneMove : MonoBehaviour
{
    public void moveBattleScene()
    {
        Debug.Log($"[SceneMove] moveBattleScene 호출: activeScene={SceneManager.GetActiveScene().name}, hasBattleDeckCollection={(BattleDeckCollection.Instance != null)}");
        if (BattleDeckCollection.Instance != null)
        {
            Debug.Log($"[SceneMove] 배틀 덱 상태: baseDeck={BattleDeckCollection.Instance.BaseBattleDeck.Count}, earned={BattleDeckCollection.Instance.EarnedBattleCards.Count}");
        }

        if (CardSystem.Instance != null)
        {
            CivilizationDeckStateStore stateStore = CivilizationDeckStateStore.GetOrCreate();
            stateStore.Store(CardSystem.Instance.CaptureRuntimeState());
            Debug.Log("[SceneMove] 문명 덱 런타임 상태 저장 완료");
        }
        else
        {
            Debug.LogWarning("[SceneMove] CardSystem.Instance가 없어 문명 덱 상태를 저장하지 못했습니다.");
        }

        Debug.Log("[SceneMove] NYH3 배틀 씬 로드 시작");
        SceneManager.LoadScene("NYH3");
    }
}
