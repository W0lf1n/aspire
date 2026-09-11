using Aspire.Api.Auth;
using Aspire.Domain;
using Xunit;

namespace Aspire.Api.Tests;

public sealed class PairingCodeTests
{
    [Fact]
    public void A_code_matches_its_own_hash()
    {
        var stored = PairingCode.Hash("123456");

        Assert.True(PairingCode.Matches(stored, "123456"));
    }

    [Theory]
    [InlineData("123457")]
    [InlineData("")]
    [InlineData("1234567")]
    [InlineData(" 123456")]
    public void Anything_else_does_not(string given)
    {
        var stored = PairingCode.Hash("123456");

        Assert.False(PairingCode.Matches(stored, given));
    }

    [Fact]
    public void Two_hashes_of_one_code_differ()
    {
        // A salt each: two boards with the same code do not look alike at rest.
        Assert.NotEqual(PairingCode.Hash("123456"), PairingCode.Hash("123456"));
    }

    [Fact]
    public void The_hash_does_not_contain_the_code_and_fits_the_column()
    {
        var code = new string('7', 12);
        var stored = PairingCode.Hash(code);

        Assert.DoesNotContain(code, stored);
        Assert.True(stored.Length <= Board.CodeHashMaxLength, $"{stored.Length} characters");
    }

    [Fact]
    public void A_generated_code_is_digits_of_the_length_asked_for()
    {
        var code = PairingCode.Generate(12);

        Assert.Equal(12, code.Length);
        Assert.All(code, c => Assert.True(char.IsAsciiDigit(c), $"{c} is not a digit"));
        Assert.True(PairingCode.Matches(PairingCode.Hash(code), code));
    }

    [Fact]
    public void A_generated_code_never_starts_with_a_zero()
    {
        // A hundred of them: a leading zero is 1 in 10 if the rule is not
        // there, so this fails on the first run rather than on somebody's
        // board being unpairable because a digit was lost reading it out.
        for (var i = 0; i < 100; i++) Assert.NotEqual('0', PairingCode.Generate(12)[0]);
    }

    [Fact]
    public void Two_generated_codes_differ()
    {
        var codes = Enumerable.Range(0, 50).Select(_ => PairingCode.Generate(12)).ToList();

        Assert.Equal(codes.Count, codes.Distinct().Count());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData(-3)]
    public void A_code_shorter_than_two_digits_is_a_mistake_worth_throwing_over(int digits)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PairingCode.Generate(digits));
    }

    [Theory]
    [InlineData("")]
    [InlineData("sha256$abc")]
    [InlineData("pbkdf2-sha256$x$y$z")]
    [InlineData("pbkdf2-sha256$100000$not base64!$AAAA")]
    [InlineData("pbkdf2-sha256$0$AAAA$AAAA")]
    public void A_malformed_or_empty_hash_never_matches_and_never_throws(string stored)
    {
        Assert.False(PairingCode.Matches(stored, "123456"));
    }
}
