using UnityEngine;
namespace NYH.CoreCardSystem
{
	public class ContinueBehaviour : GameAction 
	{
		public int Amount { get; private set; }

		public ContinueBehaviour(int amount)
		{
			Amount = amount;
		}
	}
}
