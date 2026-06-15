using Infolure.Api.Infrastructure.Persistence.Auditing;

namespace Infolure.Api.Infrastructure.Persistence.Entities;

// Domínio de conteúdo (reviews) — espelha data-model.md.
// Feature 002: IAuditable em todas. Review.Status (moderação) mantém-se distinto.

public class LureReview : IAuditable
{
    public Guid Id { get; set; }
    public Guid LureId { get; set; }
    public Guid? UserId { get; set; }
    public short Rating { get; set; } // 1..5
    public string? Body { get; set; }
    public string Locale { get; set; } = "pt";
    public int HelpfulCount { get; set; }
    public string Status { get; set; } = "published"; // pending | published | hidden (moderação)
    public DateTimeOffset CreatedAt { get; set; }

    public Lure Lure { get; set; } = null!;
    public User? User { get; set; }
    public ICollection<ReviewHelpfulVote> HelpfulVotes { get; set; } = new List<ReviewHelpfulVote>();

    public bool IsActive { get; set; } = true;
    public string Source { get; set; } = AuditSource.Manual;
    public DateTimeOffset? DeletedAt { get; set; }
}

public class ReviewHelpfulVote : IAuditable
{
    public Guid ReviewId { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public LureReview Review { get; set; } = null!;
    public User User { get; set; } = null!;

    public bool IsActive { get; set; } = true;
    public string Source { get; set; } = AuditSource.Manual;
    public DateTimeOffset? DeletedAt { get; set; }
}
