using Payment.Application.DTOs;

namespace Payment.Application.Interfaces;

public interface IPaytrService
{
    Task<PaytrInitResponse> InitializePaymentAsync(PaytrInitRequest request, CancellationToken cancellationToken = default);
    Task<PaytrRefundResponse> ProcessRefundAsync(string merchantOid, decimal amount, string? reference = null, CancellationToken cancellationToken = default);
    bool VerifyCallback(PaytrCallbackRequest callback, string expectedHash);
    string GenerateCallbackHash(PaytrCallbackRequest callback);
}

