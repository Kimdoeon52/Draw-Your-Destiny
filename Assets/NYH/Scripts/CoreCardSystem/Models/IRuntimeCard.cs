namespace NYH.CoreCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    public interface IRuntimeCard
    {
        int RuntimeCardID { get; }
        string RuntimeTitle { get; }
        string RuntimeDescription { get; }
        Sprite RuntimeImage { get; }
        int RuntimeCost { get; }
        IReadOnlyList<Effect> RuntimeEffects { get; }
    }
}
