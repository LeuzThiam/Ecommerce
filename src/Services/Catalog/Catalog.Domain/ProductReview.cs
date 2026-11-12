using Catalog.Domain.Common;

namespace Catalog.Domain;

// Avis client sur un produit - alimenté pour l'instant uniquement par l'import dummyjson.com
// (qui fournit ses propres avis fictifs par produit), jamais saisi par un vrai client. Même
// pattern que ProductVariant : entité enfant avec Id généré par le Domain, jamais par la DB.
public sealed class ProductReview
{
    public Guid Id { get; }
    public int Rating { get; private set; }
    public string Comment { get; private set; }
    public string ReviewerName { get; private set; }
    public string ReviewerEmail { get; private set; }
    public DateTime ReviewedAtUtc { get; private set; }

    private ProductReview(Guid id, int rating, string comment, string reviewerName, string reviewerEmail, DateTime reviewedAtUtc)
    {
        Id = id;
        Rating = rating;
        Comment = comment;
        ReviewerName = reviewerName;
        ReviewerEmail = reviewerEmail;
        ReviewedAtUtc = reviewedAtUtc;
    }

    internal static ProductReview Create(int rating, string comment, string reviewerName, string reviewerEmail, DateTime reviewedAtUtc)
    {
        if (rating < 1 || rating > 5)
        {
            throw new DomainException("La note doit être comprise entre 1 et 5.");
        }

        if (string.IsNullOrWhiteSpace(comment))
        {
            throw new DomainException("Le commentaire est requis.");
        }

        if (string.IsNullOrWhiteSpace(reviewerName))
        {
            throw new DomainException("Le nom de l'auteur est requis.");
        }

        return new ProductReview(Guid.NewGuid(), rating, comment.Trim(), reviewerName.Trim(), reviewerEmail.Trim(), reviewedAtUtc);
    }
}
