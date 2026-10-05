using System.Net;
using System.Net.Mail;
using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Application.Common.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GiftFinder.Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailSettings> options, ILogger<SmtpEmailService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            _logger.LogWarning("Không thể gửi email: Địa chỉ người nhận đang trống.");
            return;
        }

        // Nếu người dùng chưa cấu hình thông tin SMTP hoặc mật khẩu thật, log thông báo mô phỏng và không làm crash app
        if (string.IsNullOrWhiteSpace(_settings.SenderEmail) ||
            string.IsNullOrWhiteSpace(_settings.Password) ||
            _settings.SenderEmail.Contains("your-email@") ||
            _settings.Password.Contains("app-password"))
        {
            _logger.LogInformation(
                "[MÔ PHỎNG EMAIL SMTP] Chưa cấu hình thông tin Email thật trong appsettings.json.\n" +
                "-> Tới: {ToEmail}\n" +
                "-> Tiêu đề: {Subject}\n" +
                "-> Hệ thống đã hoàn tất xử lý thông báo giả lập thành công.",
                toEmail, subject);
            return;
        }

        try
        {
            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            mailMessage.To.Add(new MailAddress(toEmail));

            using var smtpClient = new SmtpClient(_settings.SmtpServer, _settings.Port)
            {
                Credentials = new NetworkCredential(
                    string.IsNullOrWhiteSpace(_settings.Username) ? _settings.SenderEmail : _settings.Username,
                    _settings.Password
                ),
                EnableSsl = _settings.EnableSsl
            };

            _logger.LogInformation("Đang gửi email thật tới {ToEmail} qua SMTP server {SmtpServer}:{Port}...", toEmail, _settings.SmtpServer, _settings.Port);
            await smtpClient.SendMailAsync(mailMessage, cancellationToken);
            _logger.LogInformation("Đã gửi email thành công tới {ToEmail}.", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra khi gửi email tới {ToEmail}: {Message}", toEmail, ex.Message);
            // Không ném lại exception để tránh làm gián đoạn toàn bộ batch gửi thông báo
        }
    }
}
