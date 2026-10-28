namespace GiftFinder.Application.Common.Interfaces;

public interface IVnPayService
{
    /// <summary>
    /// Sinh URL thanh toán VNPAY (Sandbox) kèm chữ ký bảo mật HMAC-SHA512
    /// </summary>
    /// <param name="transactionId">Mã giao dịch nội bộ trong GiftFinderDb</param>
    /// <param name="amount">Số tiền thanh toán (VNĐ)</param>
    /// <param name="orderInfo">Nội dung thanh toán hiển thị trên VNPAY</param>
    /// <param name="ipAddress">Địa chỉ IP của người dùng</param>
    /// <returns>Đường dẫn chuyển hướng sang cổng thanh toán VNPAY</returns>
    string CreatePaymentUrl(Guid transactionId, long amount, string orderInfo, string ipAddress);

    /// <summary>
    /// Xác thực chữ ký Checksum HMAC-SHA512 từ tham số phản hồi của VNPAY (BR-29)
    /// </summary>
    bool ValidateSignature(IReadOnlyDictionary<string, string> queryParams, string inputHash);

    /// <summary>
    /// Bóc tách các tham số từ Webhook IPN hoặc Return URL của VNPAY
    /// </summary>
    VnPayIpnResult ParseIpn(IReadOnlyDictionary<string, string> queryParams);
}

public class VnPayIpnResult
{
    public bool IsValidSignature { get; set; }
    public string? TxnRef { get; set; }
    public Guid? TransactionId { get; set; }
    public long Amount { get; set; }
    public string? ResponseCode { get; set; }
    public string? TransactionStatus { get; set; }
    public string? GatewayTransactionNo { get; set; }
    public string? BankCode { get; set; }
    public string? OrderInfo { get; set; }
    public bool IsSuccess => IsValidSignature && ResponseCode == "00" && (TransactionStatus == null || TransactionStatus == "00");
}

