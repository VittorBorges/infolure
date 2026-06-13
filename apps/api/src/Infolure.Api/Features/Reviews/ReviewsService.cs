using Infolure.Api.Features.Auth;
using Infolure.Api.Infrastructure.Persistence;
using Infolure.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infolure.Api.Features.Reviews;

/// <summary>
/// Avaliações (US-08): uma review por (utilizador, isca), agregado (média + distribuição),
/// voto "útil" (um por utilizador/review). Reviews publicadas imediatamente (sem moderação em v1).
/// </summary>
public class ReviewsService(AppDbContext db, UserResolver users)
{
    public async Task<(ReviewResult Result, ReviewDto? Review)> CreateAsync(
        string sub, string slug, CreateReviewRequest req, CancellationToken ct = default)
    {
        var userId = await users.ResolveUserIdAsync(sub, ct);
        if (userId is null) return (ReviewResult.UserNotFound, null);
        if (req.Rating is < 1 or > 5) return (ReviewResult.Invalid, null);
        if (req.Body is { Length: > 1000 }) return (ReviewResult.Invalid, null);

        var lureId = await db.Lures.Where(l => l.Slug == slug && l.Status == "published")
            .Select(l => (Guid?)l.Id).FirstOrDefaultAsync(ct);
        if (lureId is null) return (ReviewResult.LureNotFound, null);

        if (await db.LureReviews.AnyAsync(r => r.LureId == lureId && r.UserId == userId, ct))
            return (ReviewResult.Conflict, null);

        var review = new LureReview
        {
            Id = Guid.NewGuid(),
            LureId = lureId.Value,
            UserId = userId,
            Rating = (short)req.Rating,
            Body = req.Body,
            Status = "published",
        };
        db.LureReviews.Add(review);
        await db.SaveChangesAsync(ct);
        return (ReviewResult.Ok, await MapAsync(review.Id, userId, ct));
    }

    public async Task<(ReviewResult Result, ReviewDto? Review)> UpdateAsync(
        string sub, Guid reviewId, UpdateReviewRequest req, CancellationToken ct = default)
    {
        var userId = await users.ResolveUserIdAsync(sub, ct);
        if (userId is null) return (ReviewResult.UserNotFound, null);

        var review = await db.LureReviews.FirstOrDefaultAsync(r => r.Id == reviewId, ct);
        if (review is null) return (ReviewResult.NotFound, null);
        if (review.UserId != userId) return (ReviewResult.NotOwner, null);

        if (req.Rating is { } rt && rt is < 1 or > 5) return (ReviewResult.Invalid, null);
        if (req.Body is { Length: > 1000 }) return (ReviewResult.Invalid, null);

        if (req.Rating is { } rating) review.Rating = (short)rating;
        if (req.Body is not null) review.Body = req.Body;
        await db.SaveChangesAsync(ct);
        return (ReviewResult.Ok, await MapAsync(review.Id, userId, ct));
    }

    public async Task<ReviewResult> DeleteAsync(string sub, Guid reviewId, CancellationToken ct = default)
    {
        var userId = await users.ResolveUserIdAsync(sub, ct);
        if (userId is null) return ReviewResult.UserNotFound;

        var review = await db.LureReviews.FirstOrDefaultAsync(r => r.Id == reviewId, ct);
        if (review is null) return ReviewResult.NotFound;
        if (review.UserId != userId) return ReviewResult.NotOwner;

        db.LureReviews.Remove(review);
        await db.SaveChangesAsync(ct);
        return ReviewResult.Ok;
    }

    public async Task<ReviewsListResponse?> ListAsync(
        string slug, string sort, string? sub, CancellationToken ct = default)
    {
        var lureId = await db.Lures.Where(l => l.Slug == slug && l.Status == "published")
            .Select(l => (Guid?)l.Id).FirstOrDefaultAsync(ct);
        if (lureId is null) return null;

        Guid? userId = sub is null ? null : await users.ResolveUserIdAsync(sub, ct);

        var query = db.LureReviews.Where(r => r.LureId == lureId && r.Status == "published");
        query = sort == "helpful"
            ? query.OrderByDescending(r => r.HelpfulCount).ThenByDescending(r => r.CreatedAt)
            : query.OrderByDescending(r => r.CreatedAt);

        var reviews = await query.Include(r => r.User).AsNoTracking().ToListAsync(ct);

        var votedIds = userId is null
            ? new HashSet<Guid>()
            : (await db.ReviewHelpfulVotes
                .Where(v => v.UserId == userId && reviews.Select(r => r.Id).Contains(v.ReviewId))
                .Select(v => v.ReviewId).ToListAsync(ct)).ToHashSet();

        var data = reviews.Select(r => new ReviewDto(
            r.Id, r.Rating, r.Body, r.HelpfulCount,
            userId is null ? null : votedIds.Contains(r.Id),
            r.CreatedAt,
            new ReviewAuthorDto(r.User?.Username, r.User?.AvatarUrl))).ToList();

        var ratings = reviews.Select(r => (int)r.Rating).ToList();
        var distribution = Enumerable.Range(1, 5)
            .ToDictionary(i => i.ToString(), i => ratings.Count(x => x == i));
        var aggregate = new ReviewAggregate(
            ratings.Count > 0 ? ratings.Average() : null, ratings.Count, distribution);

        return new ReviewsListResponse(data, aggregate);
    }

    public async Task<(ReviewResult Result, HelpfulResponse? Response)> ToggleHelpfulAsync(
        string sub, Guid reviewId, CancellationToken ct = default)
    {
        var userId = await users.ResolveUserIdAsync(sub, ct);
        if (userId is null) return (ReviewResult.UserNotFound, null);

        var review = await db.LureReviews.FirstOrDefaultAsync(r => r.Id == reviewId, ct);
        if (review is null) return (ReviewResult.NotFound, null);

        var vote = await db.ReviewHelpfulVotes
            .FirstOrDefaultAsync(v => v.ReviewId == reviewId && v.UserId == userId, ct);

        bool isHelpful;
        if (vote is null)
        {
            db.ReviewHelpfulVotes.Add(new ReviewHelpfulVote { ReviewId = reviewId, UserId = userId.Value });
            review.HelpfulCount += 1;
            isHelpful = true;
        }
        else
        {
            db.ReviewHelpfulVotes.Remove(vote);
            review.HelpfulCount = Math.Max(0, review.HelpfulCount - 1);
            isHelpful = false;
        }
        await db.SaveChangesAsync(ct);
        return (ReviewResult.Ok, new HelpfulResponse(review.HelpfulCount, isHelpful));
    }

    private async Task<ReviewDto> MapAsync(Guid reviewId, Guid? userId, CancellationToken ct)
    {
        var r = await db.LureReviews.Where(x => x.Id == reviewId).Include(x => x.User)
            .AsNoTracking().FirstAsync(ct);
        var voted = userId is not null &&
            await db.ReviewHelpfulVotes.AnyAsync(v => v.ReviewId == reviewId && v.UserId == userId, ct);
        return new ReviewDto(r.Id, r.Rating, r.Body, r.HelpfulCount, userId is null ? null : voted,
            r.CreatedAt, new ReviewAuthorDto(r.User?.Username, r.User?.AvatarUrl));
    }
}
