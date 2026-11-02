using Catalog.Domain.Common;

namespace Catalog.Domain;

public sealed class Category
{
    public Guid Id { get; }
    public string Name { get; private set; }

    private Category(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    public static Category Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Le nom de la catégorie est requis.");
        }

        return new Category(Guid.NewGuid(), name.Trim());
    }
}