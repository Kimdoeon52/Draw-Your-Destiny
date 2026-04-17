using System.Collections.Generic;
using UnityEngine;

public class EnemyBrainManager : MonoBehaviour
{
    private static EnemyBrainManager _instance;
    public static EnemyBrainManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<EnemyBrainManager>();
                if (_instance == null)
                {
                    var go = new GameObject("EnemyBrainManager");
                    _instance = go.AddComponent<EnemyBrainManager>();
                }
            }
            return _instance;
        }
    }

    private Dictionary<int, EnemyBrainBase> brainsByCivID = new();

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
    }

    public void Register(int civID, EnemyBrainBase brain)
    {
        if (!brainsByCivID.ContainsKey(civID))
            brainsByCivID.Add(civID, brain);
    }

    public EnemyBrainBase GetBrain(int civID)
    {
        brainsByCivID.TryGetValue(civID, out var brain);
        return brain;
    }
}
