namespace NYH.CoreCardSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    public abstract class CardDataBase : ScriptableObject
    {
        public abstract int SharedCardID { get; }
        public abstract string SharedCardName { get; }
        public abstract Sprite SharedImage { get; }
        public abstract string SharedDescription { get; }
        public abstract int SharedBaseCost { get; }
        public abstract IReadOnlyList<Effect> SharedEffects { get; }
    }
}
