using UnityEngine;
using TMPro;

public class EnemyUnitPatton : MonoBehaviour
{
    [Header("AI 설정")]
    [Tooltip("이 유닛이 개별적으로 사용할 AI 전략임")]
    public AIBehaviorStrategySO myStrategy;
     [Header("UI 설정")]
    [Tooltip("데미지를 띄워줄 TextMeshPro 오브젝트")]
    public TextMeshPro damageText;
}


