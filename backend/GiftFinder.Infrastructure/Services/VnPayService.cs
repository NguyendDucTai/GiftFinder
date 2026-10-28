using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using GiftFinder.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GiftFinder.Infrastructure.Services;

public class VnPayService : IVnPayService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<VnPayService> _logger;

    public VnPayService(IConfiguration configuration, ILogger<VnPayService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public string CreatePaymentUrl(Guid transactionId, long amount, string orderInfo, string ipAddress)
    {
        var vnpSection = _configuration.GetSection("VnPay");
        var baseUrl = vnpSection["BaseUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        var tmnCode = vnpSection["TmnCode"] ?? "GFVNPAY1";
        var hashSecret = vnpSection["HashSecret"] ?? "RAOANNKABCDEFGHIZKLMNOPQRSTUVWXYZ12";
        var returnUrl = vnpSection["ReturnUrl"] ?? "https://localhost:7276/api/Webhooks/vnpay-return";
        var version = vnpSection["Version"] ?? "2.1.0";
        var command = vnpSection["Command"] ?? "pay";
        var currCode = vnpSection["CurrCode"] ?? "VND";
        var locale = vnpSection["Locale"] ?? "vn";

        // Chuẩn hóa địa chỉ IP
        var safeIp = string.IsNullOrWhiteSpace(ipAddress) || ipAddress == "::1" ? "127.0.0.1" : ipAddress.Trim();

        // Thời gian tạo theo giờ Việt Nam (UTC+7)
        var vnNow = DateTime.UtcNow.AddHours(7);
        var createDate = vnNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var expireDate = vnNow.AddMinutes(15).ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);

        var requestData = new SortedList<string, string>(new VnPayCompare())
        {
            { "vnp_Version", version },
            { "vnp_Command", command },
            { "vnp_TmnCode", tmnCode },
            { "vnp_Amount", (amount * 100).ToString(CultureInfo.InvariantCulture) },
            { "vnp_CreateDate", createDate },
            { "vnp_CurrCode", currCode },
            { "vnp_IpAddr", safeIp },
            { "vnp_Locale", locale },
            { "vnp_OrderInfo", orderInfo },
            { "vnp_OrderType", "other" },
            { "vnp_ReturnUrl", returnUrl },
            { "vnp_TxnRef", transactionId.ToString("N") },
            { "vnp_ExpireDate", expireDate }
        };

        var hashData = new StringBuilder();
        var query = new StringBuilder();

        foreach (var (key, value) in requestData)
        {
            if (string.IsNullOrEmpty(value)) continue;

            var encodedKey = WebUtility.UrlEncode(key);
            var encodedValue = WebUtility.UrlEncode(value);

            hashData.Append(encodedKey).Append('=').Append(encodedValue).Append('&');
            query.Append(encodedKey).Append('=').Append(encodedValue).Append('&');
        }

        if (hashData.Length > 0)
        {
            hashData.Remove(hashData.Length - 1, 1);
        }

        var secureHash = HmacSha512(hashSecret, hashData.ToString());
        query.Append("vnp_SecureHash=").Append(secureHash);

        var paymentUrl = $"{baseUrl}?{query}";
        _logger.LogInformation("[VnPay] Tạo URL thanh toán thành công cho đơn {TxnRef}, Số tiền: {Amount:N0}đ", transactionId, amount);

        return paymentUrl;
    }

    public bool ValidateSignature(IReadOnlyDictionary<string, string> queryParams, string inputHash)
    {
        if (string.IsNullOrWhiteSpace(inputHash)) return false;

        var hashSecret = _configuration["VnPay:HashSecret"] ?? "RAOANNKABCDEFGHIZKLMNOPQRSTUVWXYZ12";
        var sortedParams = new SortedList<string, string>(new VnPayCompare());

        foreach (var (k, v) in queryParams)
        {
            if (string.IsNullOrWhiteSpace(k) || string.IsNullOrWhiteSpace(v)) continue;
            if (k.StartsWith("vnp_SecureHash", StringComparison.OrdinalIgnoreCase)) continue;
            if (!k.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase)) continue;

            sortedParams.Add(k, v);
        }

        var hashData = new StringBuilder();
        foreach (var (k, v) in sortedParams)
        {
            hashData.Append(WebUtility.UrlEncode(k)).Append('=').Append(WebUtility.UrlEncode(v)).Append('&');
        }

        if (hashData.Length > 0)
        {
            hashData.Remove(hashData.Length - 1, 1);
        }

        var computedHash = HmacSha512(hashSecret, hashData.ToString());
        return string.Equals(computedHash, inputHash.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public VnPayIpnResult ParseIpn(IReadOnlyDictionary<string, string> queryParams)
    {
        var result = new VnPayIpnResult();

        queryParams.TryGetValue("vnp_SecureHash", out var secureHash);
        result.IsValidSignature = !string.IsNullOrEmpty(secureHash) && ValidateSignature(queryParams, secureHash);

        if (queryParams.TryGetValue("vnp_TxnRef", out var txnRef))
        {
            result.TxnRef = txnRef;
            if (Guid.TryParse(txnRef, out var g))
            {
                result.TransactionId = g;
            }
            else if (txnRef.Length == 32 && Guid.TryParseExact(txnRef, "N", out var gExact))
            {
                result.TransactionId = gExact;
            }
        }

        if (queryParams.TryGetValue("vnp_Amount", out var amtStr) && long.TryParse(amtStr, out var rawAmt))
        {
            result.Amount = rawAmt / 100;
        }

        if (queryParams.TryGetValue("vnp_ResponseCode", out var rspCode))
        {
            result.ResponseCode = rspCode;
        }

        if (queryParams.TryGetValue("vnp_TransactionStatus", out var txnStatus))
        {
            result.TransactionStatus = txnStatus;
        }

        if (queryParams.TryGetValue("vnp_TransactionNo", out var gatewayTxnNo))
        {
            result.GatewayTransactionNo = gatewayTxnNo;
        }

        if (queryParams.TryGetValue("vnp_BankCode", out var bankCode))
        {
            result.BankCode = bankCode;
        }

        if (queryParams.TryGetValue("vnp_OrderInfo", out var orderInfo))
        {
            result.OrderInfo = orderInfo;
        }

        return result;
    }

    private static string HmacSha512(string key, string inputData)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var inputBytes = Encoding.UTF8.GetBytes(inputData);

        using var hmac = new HMACSHA512(keyBytes);
        var hashBytes = hmac.ComputeHash(inputBytes);

        var hex = new StringBuilder(hashBytes.Length * 2);
        foreach (var b in hashBytes)
        {
            hex.AppendFormat("{0:x2}", b);
        }
        return hex.ToString();
    }
}

public class VnPayCompare : IComparer<string>
{
    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x == null) return -1;
        if (y == null) return 1;
        return string.CompareOrdinal(x, y);
    }
}

