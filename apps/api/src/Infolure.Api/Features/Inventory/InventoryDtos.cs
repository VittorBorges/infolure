using Infolure.Api.Features.Catalog;

namespace Infolure.Api.Features.Inventory;

// DTOs de inventário (US-06). Espelham contracts/api.yaml.

public record AddInventoryRequest(
    Guid LureId,
    Guid? ColorId,
    int Quantity = 1,
    string? Condition = null,
    string? Notes = null);

public record UpdateInventoryRequest(
    int? Quantity,
    string? Condition,
    string? Notes);

public record InventoryColorDto(Guid Id, string Name, string? HexPrimary);

public record InventoryEntryDto(
    Guid Id,
    LureCardDto Lure,
    InventoryColorDto? Color,
    int Quantity,
    string? Condition,
    string? Notes,
    DateTimeOffset AddedAt);

public record InventoryListResponse(IReadOnlyList<InventoryEntryDto> Data, int TotalUniqueLures);

public enum InventoryResult { Ok, UserNotFound, LureNotFound, Conflict, Invalid, NotOwner, NotFound }
