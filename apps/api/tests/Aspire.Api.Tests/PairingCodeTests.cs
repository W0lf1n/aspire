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
