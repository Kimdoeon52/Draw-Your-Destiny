/*using NYH.CoreCardSystem;


// 임시 보관
namespace NYH
{RegisterDurationGA
    public class RegisterDurationGA : GameAction
    {
        public int Amount { get; private set; }

        public RegisterDurationGA(int amount)
        {
            Amount = amount;
        }
    }
}
*/

/*
 *  1 [System.Serializable]
    2 public class DurationEffect : Effect
    3 {
    4     public int duration; // �� �� ���� ���ӵ��� ���� (��: 3)
    5
    6     public override GameAction GetGameAction()
    7     {
    8         // ī�带 �� �� �� �׼��� ����Ǹ� CardSystem�� �����մϴ�.
    9         return new RegisterDurationGA(duration);
   10     }
   11 }

  2. Ȱ��ȭ�� ȿ���� �����ϴ� ������ (ActiveDuration.cs)
  ���� ���� �ʵ� ���� �� �ִ� "���� ȿ��" �� ��ü�� �����մϴ�.

    1 public class ActiveDuration
    2 {
    3     public Card SourceCard;      // ���� � ī�忴���� (ȿ������ �ٽ� ���� ����)
    4     public int RemainingTurns;   // ���� �� ��
    5
    6     public ActiveDuration(Card card, int turns)
    7     {
    8         SourceCard = card;
    9         RemainingTurns = turns;
   10     }
   11 }

  3. CardSystem.cs������ ó�� (�ٽ� ����)
  ���⼭ ī�带 �� �� ����ϰ�, ���� ���� ������ ȿ���� "��ߵ�" ��ŵ�ϴ�.

    1 public class CardSystem : Singleton<CardSystem>
    2 {
    3     // ���� Ȱ��ȭ�� ���� ȿ�� ����Ʈ
    4     private List<ActiveDuration> activeDurations = new();
    5
    6     // [A] ī�带 �� ��: ���� ȿ���� �ִٸ� ����Ʈ�� ���
    7     private IEnumerator PlayCardPerformer(PlayCardGA playCardGA)
    8     {
    9         // ... ���� ī�� ���� ���� (���� �̵� ��) ...
   10
   11         // ī�� ȿ�� �߿� DurationEffect�� �ִ��� üũ
   12         foreach (var effect in playCardGA.Card.Effects)
   13         {
   14             if (effect is DurationEffect de)
   15             {
   16                 // ���� ȿ�� ����Ʈ�� �߰� (ī��� ������ ���� �����ʹ� ���� ����)
   17                 activeDurations.Add(new ActiveDuration(playCardGA.Card, de.duration));
   18             }
   19         }
   20     }
   21
   22     // [B] �� ���� ��: ����Ʈ�� ���� ȿ�� ��ߵ� (���Ⱑ ����Ʈ!)
   23     public IEnumerator ProcessTurnEndEffects()
   24     {
   25         List<ActiveDuration> expired = new();
   26
   27         foreach (var active in activeDurations)
   28         {
   29             // 1. �ش� ī�尡 ���� '��� ȿ��'�� �ٽ� ���� ��Ͽ� ����
   30             // (�̹� ���� GoldCardEffect ���� ���⼭ �ٽ� �ҷ���!)
   31             foreach (var effect in active.SourceCard.Effects)
   32             {
   33                 // ���� �ð� ȿ�� ��ü�� ���� ������ ���� ȿ���鸸 ����
   34                 if (effect is not DurationEffect)
   35                 {
   36                     ActionSystem.Instance.AddReaction(new PerformEffectGA(effect));
   37                 }
   38             }
   39
   40             // 2. �� ���� �� ���� üũ
   41             active.RemainingTurns--;
   42             if (active.RemainingTurns <= 0) expired.Add(active);
   43         }
   44
   45         // 3. ���� ȿ���� ����
   46         foreach (var ex in expired) activeDurations.Remove(ex);
   47
   48         yield return null;
   49     }
   50 }
   1 public void EndTurn()
   2 {
   3     endTurn = true;
   4
   5     // ���� ���� �� CardSystem�� "��, ���� ���� ȿ���� �� �ߵ�����!"��� ����
   6     StartCoroutine(CardSystem.Instance.ProcessTurnEndEffects());
   7 }
 * 
 */