using UnityEngine;
using TMPro;

/// <summary>
/// 개별 적 유닛의 설정을 담는 컴포넌트입니다.
/// 유닛 프리팹에 부착하여 AI 전략과 UI 요소를 연결합니다.
/// </summary>
public class EnemyUnitPatton : MonoBehaviour
{
    [Header("AI 설정")]
    [Tooltip("이 유닛이 개별적으로 사용할 AI 전략입니다. (할당되지 않으면 매니저의 기본 전략 사용)")]
    public AIBehaviorStrategySO myStrategy;

    [Header("UI 설정")]
    [Tooltip("데미지 숫자를 띄워줄 TextMeshPro 오브젝트입니다.")]
    public TextMeshPro damageText;
}
