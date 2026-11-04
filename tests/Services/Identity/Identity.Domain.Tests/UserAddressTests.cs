using Identity.Domain.Common;
using Identity.Domain.Identity;

namespace Identity.Domain.Tests;

public class UserAddressTests
{
    [Fact]
    public void AddAddress_FirstAddress_AppearsInListAndBecomesDefault()
    {
        var user = User.Register("test@example.com", "Jean Dupont");

        var address = user.AddAddress("12 rue de la Paix", null, "Paris", "75002", "France");

        Assert.Contains(address, user.Addresses);
        Assert.True(address.IsDefault);
    }

    [Fact]
    public void AddAddress_SecondAddress_IsNotDefaultByDefault()
    {
        var user = User.Register("test@example.com", "Jean Dupont");
        user.AddAddress("12 rue de la Paix", null, "Paris", "75002", "France");

        var second = user.AddAddress("5 avenue Victor Hugo", null, "Lyon", "69002", "France");

        Assert.False(second.IsDefault);
        Assert.Equal(2, user.Addresses.Count);
    }

    [Fact]
    public void AddAddress_WithEmptyLine1_ThrowsDomainException()
    {
        var user = User.Register("test@example.com", "Jean Dupont");

        Assert.Throws<DomainException>(() => user.AddAddress("", null, "Paris", "75002", "France"));
    }

    [Fact]
    public void SetDefaultAddress_MarksChosenAddressAsDefaultAndUnmarksOthers()
    {
        var user = User.Register("test@example.com", "Jean Dupont");
        var first = user.AddAddress("12 rue de la Paix", null, "Paris", "75002", "France");
        var second = user.AddAddress("5 avenue Victor Hugo", null, "Lyon", "69002", "France");

        user.SetDefaultAddress(second.Id);

        Assert.False(first.IsDefault);
        Assert.True(second.IsDefault);
    }

    [Fact]
    public void SetDefaultAddress_WithUnknownId_ThrowsDomainException()
    {
        var user = User.Register("test@example.com", "Jean Dupont");

        Assert.Throws<DomainException>(() => user.SetDefaultAddress(Guid.NewGuid()));
    }

    [Fact]
    public void RemoveAddress_RemovingDefault_PromotesAnotherAddressToDefault()
    {
        var user = User.Register("test@example.com", "Jean Dupont");
        var first = user.AddAddress("12 rue de la Paix", null, "Paris", "75002", "France");
        var second = user.AddAddress("5 avenue Victor Hugo", null, "Lyon", "69002", "France");

        user.RemoveAddress(first.Id);

        Assert.DoesNotContain(first, user.Addresses);
        Assert.True(second.IsDefault);
    }

    [Fact]
    public void RemoveAddress_LastRemainingAddress_LeavesNoAddresses()
    {
        var user = User.Register("test@example.com", "Jean Dupont");
        var address = user.AddAddress("12 rue de la Paix", null, "Paris", "75002", "France");

        user.RemoveAddress(address.Id);

        Assert.Empty(user.Addresses);
    }

    [Fact]
    public void UpdateAddress_WithValidData_UpdatesFields()
    {
        var user = User.Register("test@example.com", "Jean Dupont");
        var address = user.AddAddress("12 rue de la Paix", null, "Paris", "75002", "France");

        user.UpdateAddress(address.Id, "1 place Bellecour", "Appartement 4", "Lyon", "69002", "France");

        Assert.Equal("1 place Bellecour", address.Line1);
        Assert.Equal("Appartement 4", address.Line2);
        Assert.Equal("Lyon", address.City);
        Assert.Equal("69002", address.PostalCode);
    }

    [Fact]
    public void UpdateDisplayName_WithValidName_UpdatesDisplayName()
    {
        var user = User.Register("test@example.com", "Jean Dupont");

        user.UpdateDisplayName("Jean-Pierre Dupont");

        Assert.Equal("Jean-Pierre Dupont", user.DisplayName);
    }

    [Fact]
    public void UpdateDisplayName_WithEmptyName_ThrowsDomainException()
    {
        var user = User.Register("test@example.com", "Jean Dupont");

        Assert.Throws<DomainException>(() => user.UpdateDisplayName(" "));
    }
}
