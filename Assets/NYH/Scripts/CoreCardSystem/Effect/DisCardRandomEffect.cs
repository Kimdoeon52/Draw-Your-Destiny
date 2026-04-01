using NYH.CoreCardSystem;
using System.Collections.Generic;
using UnityEngine;

/*
 * [ Action System 가이드: 새로운 카드 효과(Ability) 추가하기 ]
 * 
 * 새로운 카드 능력을 만들 때는 아래의 3단계를 순서대로 진행하면 됩니다.
 * 
 * 1단계: GameAction 만들기 (명령서)
 *   - 위치: 'Scripts/CoreCardSystem/Actions' 폴더
 *   - 역할: "어떤 데이터를 가지고 어떤 행동을 할 것인가?"를 정의하는 바구니입니다.
 *   - 예시: SummonUnitGA.cs를 만들고 'GameAction'을 상속받은 뒤, 'int amount' 변수를 넣습니다.
 * 
 * 2단계: Effect 만들기 (데이터 연결) - [현재 이 파일의 역할]
 *   - 위치: 'Scripts/CoreCardSystem/Effect' 폴더
 *   - 역할: 기획자(사용자)가 인스펙터 창에서 수치를 조절하고, 카드가 어떤 명령(GA)을 내릴지 결정합니다.
 *   - 방법: 'Effect'를 상속받고, 'GetGameAction()' 메서드에서 1단계에서 만든 GameAction을 생성하여 반환합니다.
 * 
 * 3단계: System에서 실행 로직 구현 (실행기)
 *   - 위치: 'Scripts/CoreCardSystem/Systems' 폴더 (예: CardSystem.cs)
 *   - 역할: 실제로 게임 안에서 카드를 지우거나, 유닛을 소환하는 코드를 짜는 곳입니다.
 *   - 방법: 
 *     1) Awake()에서 'ActionSystem.AttachPerformer<새로운GA>(Perform)'를 등록합니다.
 *     2) Perform() 코루틴 안에 'else if (action is 새로운GA ga)' 블록을 추가하고 실제 로직을 코딩합니다.
 * 
 * [ 전체 흐름도 ]
 * 카드를 냄 -> CardSystem이 카드의 Effects 리스트를 읽음 -> 각 Effect의 GetGameAction() 호출 
 * -> 생성된 GameAction(명령서)이 ActionSystem에 등록됨 -> 연결된 System의 Perform()이 실행됨
 */

public class DiscardRandomEffect : Effect
{
    [Header("랜덤으로 버릴 장수")]
    [SerializeField] private int discardAmount; 

    public override GameAction GetGameAction(int effectIndex = 0, Card sourceCard = null)
    {
        // [연결 지점] 
        // 여기서 DiscardRandomGA라는 명령서를 생성하여 시스템에 전달합니다.
        // 이 명령서(GA)가 생성되면, CardSystem.cs에 등록된 로직이 이를 감지하고 실행합니다.
        return new DiscardRandomGA(discardAmount);
    }

    public override Dictionary<string, string> GetDescriptionTokens()
    {
        return new Dictionary<string, string>
        {
            { "discardAmount", discardAmount.ToString() }
        };
    }

}
