using Unity.VisualScripting;
using UnityEngine;

public class TestScripts : MonoBehaviour
{
    [SerializeField] CardUseGroup cardUse;
    
    // Update is called once per frame
    void Start()
    {
        cardUse = FindAnyObjectByType<CardUseGroup>();
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            cardUse.SpawnFarmer(2);
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            cardUse.SpawnMiner(2);
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            cardUse.SpawnSolider(2);
        }
    }
}
