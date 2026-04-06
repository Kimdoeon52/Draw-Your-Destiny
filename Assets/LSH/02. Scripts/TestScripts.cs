/*using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;

public class TestScripts : MonoBehaviour
{
    [SerializeField] GameManager cardUse;
    [SerializeField] PlayerUnitInfoByJob unitInfo;

    // Update is called once per frame
    void Start()
    {
        cardUse = FindAnyObjectByType<GameManager>();
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            cardUse.GenerateHumans(1, unitInfo);
        }

    }
}
*/