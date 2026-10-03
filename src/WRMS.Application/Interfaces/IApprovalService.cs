using WRMS.Application.DTOs.Approvals;

namespace WRMS.Application.Interfaces;

public interface IApprovalService
{
    Task<List<PendingApprovalListItemDto>> GetPendingAsync(CancellationToken cancellationToken = default);

    Task<ApprovalReviewDto?> GetForReviewAsync(Guid approvalId, CancellationToken cancellationToken = default);

    Task DecideAsync(ReviewDecisionRequest request, Guid currentUserId, string? currentUserName, CancellationToken cancellationToken = default);
}
