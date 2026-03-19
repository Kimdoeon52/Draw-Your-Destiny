using NYH.CoreCardSystem;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class GameStarter : MonoBehaviour
{
    [SerializeField] private List<CardData> myDeck;

    private IEnumerator Start()
    {
        yield return null; // 시스템 초기화 대기

        if (CardSystem.Instance != null && myDeck != null && myDeck.Count > 0)
        {
            CardSystem.Instance.Setup(myDeck);
            yield return new WaitForSeconds(0.1f);
            ActionSystem.Instance.Perform(new DrawCardsGA(5));
        }
        else
        {
            Debug.LogError("GameStarter: 덱 정보가 없거나 CardSystem이 없습니다.");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            ActionSystem.Instance.Perform(new DrawCardsGA(1));
        }
    }
}
