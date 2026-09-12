using Aspire.Infrastructure.Media;
using Xunit;

namespace Aspire.Api.Tests;

/// <summary>
/// Which part of a photograph a cell shows (D54). Pure geometry, so every
/// edge of it is arithmetic rather than a picture somebody has to look at.
///
/// The numbers are `object-position` percentages, which is what the browser
/// does with the same two on the same photograph — so what these assert is
/// also the promise that a wallpaper crops where the reel crops.
/// </summary>
public sealed class FocalCropTests
{
    // A landscape photograph, and a cell the shape of a phone screen.
    private const int WideW = 2000;
    private const int WideH = 1000;

    [Fact]
    public void The_middle_and_no_zoom_is_the_centre_crop_it_always_was()
    {
        // A 2000×1000 source into a 500×500 cell: cover takes a 1000×1000
        // square, and the middle of the source is where it comes from.
        var window = FocalCrop.For(WideW, WideH, 500, 500, 0.5, 0.5, 1.0);

        Assert.Equal(1000, window.Width);
        Assert.Equal(1000, window.Height);
        Assert.Equal(500, window.X);
        Assert.Equal(0, window.Y);
    }

    [Fact]
    public void Nought_is_the_left_edge_and_one_is_the_right()
    {
        Assert.Equal(0, FocalCrop.For(WideW, WideH, 500, 500, 0, 0.5, 1.0).X);
        Assert.Equal(1000, FocalCrop.For(WideW, WideH, 500, 500, 1, 0.5, 1.0).X);
    }

    [Fact]
    public void Nought_is_the_top_edge_and_one_is_the_bottom()
    {
        // A portrait source into a square cell overflows downwards instead.
        Assert.Equal(0, FocalCrop.For(1000, 2000, 500, 500, 0.5, 0, 1.0).Y);
        Assert.Equal(1000, FocalCrop.For(1000, 2000, 500, 500, 0.5, 1, 1.0).Y);
    }

    [Fact]
    public void The_axis_that_does_not_overflow_has_nothing_to_choose()
    {
        // Cover means exactly one axis overflows at zoom 1; the other is
        // flush, and moving along it can only be nought.
        foreach (var y in new[] { 0.0, 0.5, 1.0 })
        {
            var window = FocalCrop.For(WideW, WideH, 500, 500, 0.5, y, 1.0);
            Assert.Equal(0, window.Y);
            Assert.Equal(WideH, window.Height);
        }
    }

    [Fact]
    public void Zooming_takes_less_of_the_photograph()
    {
        var all = FocalCrop.For(WideW, WideH, 500, 500, 0.5, 0.5, 1.0);
        var closer = FocalCrop.For(WideW, WideH, 500, 500, 0.5, 0.5, 2.0);

        Assert.Equal(500, closer.Width);
        Assert.Equal(500, closer.Height);
        Assert.True(closer.Width < all.Width);
        // Still centred, so it closed in from both sides by the same amount.
        Assert.Equal(750, closer.X);
        Assert.Equal(250, closer.Y);
    }

    [Fact]
    public void Zooming_gives_the_other_axis_something_to_choose()
    {
        // The whole reason the flush axis is worth having a number for: once
        // there is zoom, both axes overflow.
        var top = FocalCrop.For(WideW, WideH, 500, 500, 0.5, 0, 2.0);
        var bottom = FocalCrop.For(WideW, WideH, 500, 500, 0.5, 1, 2.0);

        Assert.Equal(0, top.Y);
        Assert.Equal(500, bottom.Y);
    }

    [Fact]
    public void The_window_never_leaves_the_photograph()
    {
        foreach (var (fx, fy, zoom) in new[]
        {
            (0.0, 0.0, 1.0), (1.0, 1.0, 3.0), (0.5, 0.5, 3.0), (0.0, 1.0, 2.5)
        })
        {
            var window = FocalCrop.For(WideW, WideH, 1170, 2532, fx, fy, zoom);

            Assert.True(window.X >= 0 && window.Y >= 0);
            Assert.True(window.Right <= WideW, $"right {window.Right} past {WideW}");
            Assert.True(window.Bottom <= WideH, $"bottom {window.Bottom} past {WideH}");
            Assert.True(window.Width >= 1 && window.Height >= 1);
        }
    }

    [Theory]
    // Numbers that cannot mean anything are the middle and all of it rather
    // than an exception: this is geometry on its way to a picture.
    [InlineData(-1.0, 2.0, 0.0)]
    [InlineData(double.NaN, double.NaN, double.NaN)]
    public void Nonsense_is_clamped_rather_than_thrown(double fx, double fy, double zoom)
    {
        var window = FocalCrop.For(WideW, WideH, 500, 500, fx, fy, zoom);

        Assert.True(window.X >= 0 && window.Right <= WideW);
        Assert.True(window.Y >= 0 && window.Bottom <= WideH);
    }

    [Fact]
    public void A_cell_or_a_photograph_with_no_size_is_answered_rather_than_divided_by()
    {
        Assert.Equal(1, FocalCrop.For(0, 0, 500, 500, 0.5, 0.5, 1).Width);
        Assert.Equal(WideW, FocalCrop.For(WideW, WideH, 0, 500, 0.5, 0.5, 1).Width);
    }

    [Fact]
    public void A_square_photograph_in_a_square_cell_is_the_whole_of_it()
    {
        var window = FocalCrop.For(800, 800, 400, 400, 0.3, 0.7, 1.0);

        Assert.Equal(0, window.X);
        Assert.Equal(0, window.Y);
        Assert.Equal(800, window.Width);
        Assert.Equal(800, window.Height);
    }
}
