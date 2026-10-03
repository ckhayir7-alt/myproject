using WRMS.Application.DTOs.Transfers;

namespace WRMS.Application.Interfaces;

public interface ITransferService
{
    Task<List<TransferListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<TransferDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> HasPendingTransferAsync(Guid weaponId, CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(CreateTransferRequest request, Guid currentUserId, CancellationToken cancellationToken = default);

    Task DecideAsync(DecideTransferRequest request, Guid currentUserId, string? currentUserName, CancellationToken cancellationToken = default);
}
