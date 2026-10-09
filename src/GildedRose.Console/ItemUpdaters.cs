namespace GildedRose.Console
{

    /// <summary>
    /// Names the inventory system treats specially. Anything not listed here is a "normal" item.
    /// </summary>
    internal static class ItemNames
    {
        public const string AgedBrie = "Aged Brie";
        public const string Sulfuras = "Sulfuras, Hand of Ragnaros";
        public const string BackstagePass = "Backstage passes to a TAFKAL80ETC concert";

        /// <summary>Any item whose name starts with this is a conjured item (e.g. "Conjured Mana Cake").</summary>
        public const string ConjuredPrefix = "Conjured";
    }

    /// <summary>
    /// The only place that knows the quality bounds. Each call moves quality by one step and
    /// refuses to cross the limit, so callers can simply repeat it to move by more than one.
    /// </summary>
    internal static class QualityRules
    {
        public const int Min = 0;
        public const int Max = 50;

        public static void Increase(Item item)
        {
            if (item.Quality < Max)
            {
                item.Quality++;
            }
        }

        public static void Decrease(Item item)
        {
            if (item.Quality > Min)
            {
                item.Quality--;
            }
        }
    }

    /// <summary>Knows how one kind of item ages over a single day.</summary>
    internal interface IItemUpdater
    {
        void Update(Item item);
    }

    /// <summary>
    /// Template for the daily update. The order matters and mirrors the original system:
    /// 1. adjust quality using today's SellIn value,
    /// 2. move SellIn forward by one day,
    /// 3. apply the extra "past its sell-by date" adjustment if SellIn has now gone negative.
    /// </summary>
    internal abstract class ItemUpdater : IItemUpdater
    {
        public virtual void Update(Item item)
        {
            UpdateQualityForToday(item);

            item.SellIn--;

            if (item.SellIn < 0)
            {
                UpdateQualityWhenExpired(item);
            }
        }

        protected abstract void UpdateQualityForToday(Item item);

        protected abstract void UpdateQualityWhenExpired(Item item);
    }

    /// <summary>
    /// Items that lose quality every day, and lose it twice as fast once expired.
    /// Normal items degrade by 1 per step, Conjured items by 2 per step.
    /// </summary>
    internal sealed class DegradingItemUpdater : ItemUpdater
    {
        private readonly int _degradePerStep;

        public DegradingItemUpdater(int degradePerStep)
        {
            _degradePerStep = degradePerStep;
        }

        protected override void UpdateQualityForToday(Item item) => Degrade(item);

        protected override void UpdateQualityWhenExpired(Item item) => Degrade(item);

        private void Degrade(Item item)
        {
            for (var step = 0; step < _degradePerStep; step++)
            {
                QualityRules.Decrease(item);
            }
        }
    }

    /// <summary>Aged Brie gains quality as it ages, and gains it twice as fast once expired.</summary>
    internal sealed class AgedBrieUpdater : ItemUpdater
    {
        protected override void UpdateQualityForToday(Item item) => QualityRules.Increase(item);

        protected override void UpdateQualityWhenExpired(Item item) => QualityRules.Increase(item);
    }

    /// <summary>
    /// Backstage passes gain +1 (more than 10 days left), +2 (10 days or less), +3 (5 days or less),
    /// and are worthless once the concert has passed.
    /// </summary>
    internal sealed class BackstagePassUpdater : ItemUpdater
    {
        private const int DoubleRateThreshold = 10;
        private const int TripleRateThreshold = 5;

        protected override void UpdateQualityForToday(Item item)
        {
            QualityRules.Increase(item);

            if (item.SellIn <= DoubleRateThreshold)
            {
                QualityRules.Increase(item);
            }

            if (item.SellIn <= TripleRateThreshold)
            {
                QualityRules.Increase(item);
            }
        }

        protected override void UpdateQualityWhenExpired(Item item) => item.Quality = QualityRules.Min;
    }

    /// <summary>Sulfuras is legendary: it is never sold and never changes (Quality stays at 80).</summary>
    internal sealed class SulfurasUpdater : ItemUpdater
    {
        public override void Update(Item item)
        {
            // Intentionally nothing: neither SellIn nor Quality ever changes.
        }

        protected override void UpdateQualityForToday(Item item)
        {
        }

        protected override void UpdateQualityWhenExpired(Item item)
        {
        }
    }

    /// <summary>
    /// Chooses the right updater for an item. Adding a new kind of item means adding one updater
    /// class and one line here; nothing else needs to change.
    /// </summary>
    internal static class ItemUpdaterFactory
    {
        // Updaters hold no per-item state, so a single shared instance of each is safe.
        private static readonly IItemUpdater Normal = new DegradingItemUpdater(degradePerStep: 1);
        private static readonly IItemUpdater Conjured = new DegradingItemUpdater(degradePerStep: 2);
        private static readonly IItemUpdater AgedBrie = new AgedBrieUpdater();
        private static readonly IItemUpdater BackstagePass = new BackstagePassUpdater();
        private static readonly IItemUpdater Sulfuras = new SulfurasUpdater();

        public static IItemUpdater For(Item item) => item.Name switch
        {
            ItemNames.AgedBrie => AgedBrie,
            ItemNames.Sulfuras => Sulfuras,
            ItemNames.BackstagePass => BackstagePass,
            var name when name.StartsWith(ItemNames.ConjuredPrefix, StringComparison.Ordinal) => Conjured,
            _ => Normal,
        };
    }

}