using Identity.Domain.Common;
using Identity.Domain.Identity;

namespace Identity.Domain.Tests;

public class EmailTests
{
    [Fact]
    public void Create_WithValidEmail_NormalizesValue()
    {
        string emailValue = "Test@example.com ";
        Email email = Email.Create(emailValue);
        Assert.Equal("test@example.com", email.Value);
    }

    [Fact]
    public void Create_WithEmptyEmail_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Email.Create(""));
    }

    [Fact]
    public void Create_WithoutAtSign_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Email.Create("invalidemail.com"));
    }

}