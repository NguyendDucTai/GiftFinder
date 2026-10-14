using System.Security.Cryptography;
using System.Text;
using GiftFinder.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GiftFinder.Infrastructure.Services;

public class VietQrPaymentService : IPaymentService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<VietQrPaymentService> _logger;

    public VietQrPaymentService(IConfiguration configuration, ILogger<VietQrPaymentService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public PaymentCheckoutInfo GenerateVietQrPayment(Guid transactionId, long amount, string description)
    {
        var bankId = _configuration["Payment:BankId"] ?? "MB";
        var bankName = _configuration["Payment:BankName"] ?? "MBBank (Ngân hàng TMCP Quân Đội)";
        var accountNumber = _configuration["Payment:AccountNumber"] ?? "0388688999";
        var accountName = _configuration["Payment:AccountName"] ?? "CONG TY GIFTFINDER";

        // Nội dung chuyển khoản duy nhất gắn liền với TransactionId (dễ dàng đối soát ngân hàng)
        var transferContent = $"GFPREMIUM {transactionId:N}"[..18].ToUpperInvariant();

        var encodedContent = Uri.EscapeDataString(transferContent);
        var encodedAccountName = Uri.EscapeDataString(accountName);

        // Sinh link ảnh mã VietQR chuẩn Napas 24/7 từ VietQR API
        var qrCodeUrl = $"https://img.vietqr.io/image/{bankId}-{accountNumber}-compact2.png?amount={amount}&addInfo={encodedContent}&accountName={encodedAccountName}";

        // Deep link cho ứng dụng ngân hàng di động
        var paymentUrl = $"https://api.vietqr.io/{bankId}/{accountNumber}/{amount}/{encodedContent}/vietqr_net_2.jpg";

        _logger.LogInformation("Sinh mã VietQR thành công cho Transaction {TransactionId}: Số tiền {Amount:N0}đ, Nội dung: {Content}",
            transactionId, amount, transferContent);

        return new PaymentCheckoutInfo
        {
            BankId = bankId,
            BankName = bankName,
            AccountNumber = accountNumber,
            AccountName = accountName,
            Amount = amount,
            TransferContent = transferContent,
            QrCodeUrl = qrCodeUrl,
            PaymentUrl = paymentUrl
        };
    }

    public bool VerifyWebhookSignature(string payloadJson, string signature)
    {
        var secret = _configuration["Payment:WebhookSecret"] ?? "giftfinder_payment_secret_2026";
        if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(secret))
        {
            return false;
        }

        // 1. Kiểm tra nếu signature truyền trực tiếp mã Secret
        if (string.Equals(signature.Trim(), secret.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // 2. Kiểm tra chữ ký HMAC-SHA256 chuẩn
        try
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadJson ?? string.Empty));
            var computedSignature = Convert.ToHexString(hashBytes);

            return string.Equals(computedSignature, signature.Trim(), StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi kiểm tra chữ ký Webhook HMAC-SHA256");
            return false;
        }
    }
}
