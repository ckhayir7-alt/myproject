using WRMS.Application.DTOs.Owners;
using WRMS.Domain.Enums;

namespace WRMS.Application.Interfaces;

public interface IOwnerService
{
    Task<List<OwnerListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<OwnerDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> IsNationalIdInUseAsync(string nationalId, Guid? excludeId = null, CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(SaveOwnerRequest request, Guid currentUserId, CancellationToken cancellationToken = default);

    Task UpdateAsync(SaveOwnerRequest request, Guid currentUserId, CancellationToken cancellationToken = default);

    Task SetStatusAsync(Guid id, OwnerStatus status, string? reason, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<Guid> AddDocumentAsync(Guid ownerId, string fileName, string storedFileName, long sizeBytes, string contentType, string? description, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<OwnerDocumentDto?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
}
