using System;

namespace Gyvr.Mythril2D
{
    [Serializable]
    public struct Loot
    {
        public Item item;
        public GameConditionSet conditions;
        public int quantity;
        public int dropRate;
        public int minimumMonsterLevel;
        public int minimumPlayerLevel;

        public bool IsAvailable() => GameConditionChecker.IsConditionSetMet(conditions);
        public bool ResolveDrop() => UnityEngine.Random.Range(1, 101) <= dropRate;
    }
}
