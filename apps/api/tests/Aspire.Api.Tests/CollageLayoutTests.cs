using Aspire.Infrastructure.Media;
using Xunit;

namespace Aspire.Api.Tests;

public sealed class CollageLayoutTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 3)]
    [InlineData(4, 2)]
    [InlineData(5, 3)]
    [InlineData(6, 3)]
    public void Up_to_three_stack_and_above_that_they_pair(int count, int rows)
    {
        Assert.Equal(rows, CollageLayout.RowsFor(count));
    }

    [Fact]
    public void One_photograph_is_the_whole_canvas()
    {
        Assert.Equal([new CollageCell(0, 0, 1170, 2532)], CollageLayout.For(1, 1170, 2532));
    }

    [Fact]
    public void Four_are_two_by_two()
    {
        var cells = CollageLayout.For(4, 1000, 2000);

        Assert.Equal(
            [
                new CollageCell(0, 0, 500, 1000),
                new CollageCell(500, 0, 500, 1000),
                new CollageCell(0, 1000, 500, 1000),
                new CollageCell(500, 1000, 500, 1000)
            ],
            cells);
    }

    [Fact]
    public void Five_put_the_odd_one_last_and_full_width()
    {
        var cells = CollageLayout.For(5, 1000, 3000);

        // Two, two, then one across the bottom: the earlier rows take the
        // extra, so the single photograph is the one you end on.
        Assert.Equal(new CollageCell(0, 2000, 1000, 1000), cells[4]);
        Assert.Equal(2, cells.Count(c => c.Y == 0));
        Assert.Equal(2, cells.Count(c => c.Y == 1000));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void Every_count_covers_the_canvas_exactly(int count)
    {
        // An awkward size on purpose: 1179 x 2556 divides by neither 2 nor 3,
        // so any rounding that is repeated instead of carried leaves a seam.
        const int width = 1179;
        const int height = 2556;
        var cells = CollageLayout.For(count, width, height);

        Assert.Equal(count, cells.Count);
        Assert.All(cells, cell =>
        {
            Assert.True(cell.Width > 0);
            Assert.True(cell.Height > 0);
            Assert.InRange(cell.X, 0, width - 1);
            Assert.InRange(cell.Y, 0, height - 1);
            Assert.True(cell.X + cell.Width <= width);
            Assert.True(cell.Y + cell.Height <= height);
        });

        Assert.Equal((long)width * height, cells.Sum(c => (long)c.Width * c.Height));
        Assert.Contains(cells, c => c.X + c.Width == width);
        Assert.Contains(cells, c => c.Y + c.Height == height);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(CollageLayout.MaxPhotographs + 1)]
    public void A_count_it_cannot_arrange_is_refused(int count)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CollageLayout.For(count, 1170, 2532));
    }
}
