namespace Infolure.Api.Features.Reviews;

// DTOs de reviews (US-08). Espelham contracts/api.yaml.

public record CreateReviewRequest(int Rating, string? Body);
public record UpdateReviewRequest(int? Rating, string? Body);

public record ReviewAuthorDto(string? Username, string? AvatarUrl);

public record ReviewDto(
    Guid Id,
    int Rating,
    string? Body,
    int HelpfulCount,
    bool? IsHelpful,
    DateTimeOffset CreatedAt,
    ReviewAuthorDto Author);

public record ReviewAggregate(double? AvgRating, int TotalReviews, Dictionary<string, int> Distribution);

public record ReviewsListResponse(IReadOnlyList<ReviewDto> Data, ReviewAggregate Aggregate);

public record HelpfulResponse(int HelpfulCount, bool IsHelpful);

public enum ReviewResult { Ok, UserNotFound, LureNotFound, Conflict, Invalid, NotOwner, NotFound }
