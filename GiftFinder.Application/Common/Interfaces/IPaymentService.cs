namespace GiftFinder.Application.Common.Interfaces;

public interface IPaymentService
{
    PaymentCheckoutInfo GenerateVietQrPayment(Guid transactionId, long amount, string description);
    bool VerifyWebhookSignature(string payloadJson, string signature);
}

public class PaymentCheckoutInfo
{
    public string BankId { get; set; } = "MB";
    public string BankName { get; set; } = "MBBank (Ngân hàng Quân Đội)";
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string TransferContent { get; set; } = string.Empty;
    public string QrCodeUrl { get; set; } = string.Empty;
    public string PaymentUrl { get; set; } = string.Empty;
}
