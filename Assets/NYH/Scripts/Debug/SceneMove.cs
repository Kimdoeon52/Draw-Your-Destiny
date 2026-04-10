using UnityEngine;
using UnityEngine.SceneManagement;


//테스트용 
public class SceneMove : MonoBehaviour
{
    public void moveBattleScene()
    {
        SceneManager.LoadScene("Battle");
    }
    
}
