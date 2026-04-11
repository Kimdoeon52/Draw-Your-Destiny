namespace NYH.CoreCardSystem
{
    using UnityEngine;

    public interface ICardViewPlayHandler
    {
        bool TryPlayCard(CardView cardView, Vector2 screenPosition, bool wasDragged);
    }
}
