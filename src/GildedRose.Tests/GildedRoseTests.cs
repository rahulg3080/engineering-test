using GildedRose.Console;
using Xunit;

namespace GildedRose.Tests
{
    public class GildedRoseTests
{
    private const string Vest = "+5 Dexterity Vest";
    private const string Brie = "Aged Brie";
    private const string Sulfuras = "Sulfuras, Hand of Ragnaros";
    private const string Pass = "Backstage passes to a TAFKAL80ETC concert";
    private const string Conjured = "Conjured Mana Cake";

    /// <summary>Runs <paramref name="days"/> daily updates on a single item and returns it.</summary>
    private static Item Update(string name, int sellIn, int quality, int days = 1)
    {
        var item = new Item { Name = name, SellIn = sellIn, Quality = quality };
        var app = new Program { Items = new List<Item> { item } };

        for (var day = 0; day < days; day++)
        {
            app.UpdateQuality();
        }

        return item;
    }

    // ---------------------------------------------------------------- normal items

    [Fact]
    public void NormalItem_LosesOneQualityAndOneSellInPerDay()
    {
        var item = Update(Vest, sellIn: 10, quality: 20);

        Assert.Equal(9, item.SellIn);
        Assert.Equal(19, item.Quality);
    }

    [Theory]
    [InlineData(0, 10, 8)]   // sell-by day reached: SellIn goes negative, so it degrades twice as fast
    [InlineData(-1, 10, 8)]  // already expired
    [InlineData(-5, 2, 0)]
    public void NormalItem_DegradesTwiceAsFastOnceSellByDatePassed(int sellIn, int quality, int expected)
    {
        var item = Update(Vest, sellIn, quality);

        Assert.Equal(expected, item.Quality);
    }

    [Theory]
    [InlineData(5, 0)]
    [InlineData(0, 0)]
    [InlineData(0, 1)]  // expired item with 1 quality would drop by 2 but must stop at 0
    [InlineData(-3, 1)]
    public void NormalItem_QualityIsNeverNegative(int sellIn, int quality)
    {
        var item = Update(Vest, sellIn, quality);

        Assert.True(item.Quality >= 0);
    }

    // ---------------------------------------------------------------- aged brie

    [Theory]
    [InlineData(5, 10, 11)]
    [InlineData(0, 10, 12)]   // expired: increases twice as fast
    [InlineData(-3, 10, 12)]
    public void AgedBrie_IncreasesInQualityTheOlderItGets(int sellIn, int quality, int expected)
    {
        var item = Update(Brie, sellIn, quality);

        Assert.Equal(expected, item.Quality);
    }

    [Theory]
    [InlineData(5, 50)]
    [InlineData(5, 49)]
    [InlineData(0, 49)]   // would be +2 but is capped at 50
    [InlineData(-1, 50)]
    public void AgedBrie_QualityIsNeverMoreThan50(int sellIn, int quality)
    {
        var item = Update(Brie, sellIn, quality);

        Assert.Equal(50, item.Quality);
    }

    // ---------------------------------------------------------------- sulfuras

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(-1)]
    public void Sulfuras_NeverChanges(int sellIn)
    {
        var item = Update(Sulfuras, sellIn, quality: 80, days: 5);

        Assert.Equal(sellIn, item.SellIn);
        Assert.Equal(80, item.Quality);
    }

    // ---------------------------------------------------------------- backstage passes

    [Theory]
    [InlineData(15, 20, 21)]  // more than 10 days: +1
    [InlineData(11, 20, 21)]
    [InlineData(10, 20, 22)]  // 10 days or less: +2
    [InlineData(6, 20, 22)]
    [InlineData(5, 20, 23)]   // 5 days or less: +3
    [InlineData(1, 20, 23)]
    [InlineData(0, 20, 0)]    // concert is over after this update: worthless
    [InlineData(-1, 20, 0)]
    public void BackstagePass_QualityRisesAsConcertApproachesThenDropsToZero(int sellIn, int quality, int expected)
    {
        var item = Update(Pass, sellIn, quality);

        Assert.Equal(expected, item.Quality);
    }

    [Theory]
    [InlineData(15, 50)]
    [InlineData(10, 49)]  // would be +2
    [InlineData(5, 48)]   // would be +3
    [InlineData(5, 50)]
    public void BackstagePass_QualityIsNeverMoreThan50(int sellIn, int quality)
    {
        var item = Update(Pass, sellIn, quality);

        Assert.Equal(50, item.Quality);
    }

    // ---------------------------------------------------------------- conjured (new feature)

    [Fact]
    public void ConjuredItem_DegradesTwiceAsFastAsNormal()
    {
        var item = Update(Conjured, sellIn: 3, quality: 6);

        Assert.Equal(2, item.SellIn);
        Assert.Equal(4, item.Quality);
    }

    [Theory]
    [InlineData(0, 10, 6)]    // expired: twice as fast as an expired normal item (2 * 2)
    [InlineData(-1, 10, 6)]
    [InlineData(-1, 3, 0)]    // would be -4 but is capped at 0
    public void ConjuredItem_DegradesTwiceAsFastAsNormalOnceSellByDatePassed(int sellIn, int quality, int expected)
    {
        var item = Update(Conjured, sellIn, quality);

        Assert.Equal(expected, item.Quality);
    }

    [Theory]
    [InlineData(5, 0)]
    [InlineData(5, 1)]   // would be -2 but is capped at 0
    [InlineData(0, 3)]
    public void ConjuredItem_QualityIsNeverNegative(int sellIn, int quality)
    {
        var item = Update(Conjured, sellIn, quality);

        Assert.True(item.Quality >= 0);
    }

    [Theory]
    [InlineData("Conjured Mana Cake")]
    [InlineData("Conjured Elixir of the Mongoose")]
    public void ConjuredItems_AreRecognisedByTheirNamePrefix(string name)
    {
        var item = Update(name, sellIn: 5, quality: 10);

        Assert.Equal(8, item.Quality);
    }

    [Fact]
    public void ItemMerelyContainingTheWordConjured_IsNotConjured()
    {
        var item = Update("Not Conjured At All", sellIn: 5, quality: 10);

        Assert.Equal(9, item.Quality);
    }

    // ---------------------------------------------------------------- whole inventory

    [Fact]
    public void UpdateQuality_UpdatesEveryItemInTheInventory()
    {
        var app = new Program
        {
            Items = new List<Item>
            {
                new Item { Name = Vest, SellIn = 10, Quality = 20 },
                new Item { Name = Brie, SellIn = 2, Quality = 0 },
                new Item { Name = Sulfuras, SellIn = 0, Quality = 80 },
                new Item { Name = Pass, SellIn = 15, Quality = 20 },
                new Item { Name = Conjured, SellIn = 3, Quality = 6 },
            },
        };

        app.UpdateQuality();

        Assert.Collection(
            app.Items,
            i => Assert.Equal((9, 19), (i.SellIn, i.Quality)),
            i => Assert.Equal((1, 1), (i.SellIn, i.Quality)),
            i => Assert.Equal((0, 80), (i.SellIn, i.Quality)),
            i => Assert.Equal((14, 21), (i.SellIn, i.Quality)),
            i => Assert.Equal((2, 4), (i.SellIn, i.Quality)));
    }

    [Fact]
    public void UpdateQuality_WithNoItems_DoesNothing()
    {
        var app = new Program();

        app.UpdateQuality();

        Assert.Empty(app.Items);
    }

    // ---------------------------------------------------------------- golden master

    /// <summary>
    /// Safety net for the refactoring: for every pre-existing item type, across a wide grid of
    /// SellIn/Quality values (including out-of-range ones) and over several days, the new
    /// implementation must behave exactly like the original code did.
    /// Conjured is excluded because the original code had no such concept.
    /// </summary>
    [Fact]
    public void RefactoredImplementation_MatchesOriginalBehaviourForAllExistingItemTypes()
    {
        var names = new[] { Vest, "Elixir of the Mongoose", Brie, Sulfuras, Pass };
        var mismatches = new List<string>();

        foreach (var name in names)
        {
            for (var sellIn = -12; sellIn <= 20; sellIn++)
            {
                for (var quality = 0; quality <= 80; quality++)
                {
                    var expected = new List<Item> { new Item { Name = name, SellIn = sellIn, Quality = quality } };
                    var actual = new List<Item> { new Item { Name = name, SellIn = sellIn, Quality = quality } };
                    var app = new Program { Items = actual };

                    for (var day = 1; day <= 12; day++)
                    {
                        LegacyGildedRose.UpdateQuality(expected);
                        app.UpdateQuality();

                        if (expected[0].SellIn != actual[0].SellIn || expected[0].Quality != actual[0].Quality)
                        {
                            mismatches.Add(
                                $"{name} (start SellIn={sellIn}, Quality={quality}) day {day}: " +
                                $"expected ({expected[0].SellIn}, {expected[0].Quality}) " +
                                $"but got ({actual[0].SellIn}, {actual[0].Quality})");
                            break;
                        }
                    }
                }
            }
        }

        Assert.Empty(mismatches);
    }
}
}
