using Identity.Domain.Identity;
using Identity.Domain.Common;

namespace Identity.Domain.Tests;

public class UserTests
{
    [Fact]
    public void Register_WithValidData_CreatesUser()
    {
        string email = "test@example.com";
        string displayName = "  Jean Dupont  ";

        User user = User.Register(email, displayName);

        Assert.Equal("Jean Dupont", user.DisplayName);
        Assert.Equal("test@example.com", user.Email.Value);
        Assert.NotEqual(Guid.Empty, user.Id);
    }

    [Fact]
    public void Register_WithEmptyDisplayName_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => User.Register("test@example.com", ""));
    }

    [Fact]
    public void Register_WithInvalidEmail_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => User.Register("invalid-email", "Jean Dupont"));
    }
}