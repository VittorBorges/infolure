namespace Infolure.Api.Infrastructure.Persistence.Entities;

// Domínio de conteúdo (reviews) — espelha data-model.md.

public class LureReview
{
    public Guid Id { get; set; }
    public Guid LureId { get; set; }
    public Guid? UserId { get; set; }
    public short Rating { get; set; } // 1..5
    public string? Body { get; set; }
    public string Locale { get; set; } = "pt";
    public int HelpfulCount { get; set; }
    public string Status { get; set; } = "published"; // pending | published | hidden
    public DateTimeOffset CreatedAt { get; set; }

    public Lure Lure { get; set; } = null!;
    public User? User { get; set; }
    public ICollection<ReviewHelpfulVote> HelpfulVotes { get; set; } = new List<ReviewHelpfulVote>();
}

public class ReviewHelpfulVote
{
    public Guid ReviewId { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public LureReview Review { get; set; } = null!;
    public User User { get; set; } = null!;
}
