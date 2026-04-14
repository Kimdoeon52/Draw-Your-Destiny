using UnityEngine;

public class TurnManager : MonoBehaviour
{
    GameManager gameManager;

    public void TurnEndButton()
    {
        GameManager.Instance.EndTurn();
    }

    public void TurnStartButton()
    {
        GameManager.Instance.StartTurn();
    }
}
