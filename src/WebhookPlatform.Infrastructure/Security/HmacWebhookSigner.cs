using System.Security.Cryptography;
using System.Text;
using WebhookPlatform.Application.Common.Interfaces;

namespace WebhookPlatform.Infrastructure.Security;

/// <summary>
/// HMAC SHA-256 algoritmasıyla güvenli imzalama ve doğrulama yapan servis.
/// Zaman aşımı (Replay Attack) ve Timing Attack önleme mekanizmalarını barındırır.
/// </summary>
public class HmacWebhookSigner : IWebhookSigner
{
    public string GenerateSignature(string payload, string secretKey, long timestamp)
    {
        var stringToSign = $"{timestamp}.{payload}";
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        var dataBytes = Encoding.UTF8.GetBytes(stringToSign);

        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(dataBytes);
        var signature = Convert.ToHexString(hashBytes).ToLowerInvariant();

        return $"t={timestamp},v1={signature}";
    }

    public bool VerifySignature(string payload, string signatureHeader, string secretKey, long toleranceSeconds = 300)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader) || string.IsNullOrWhiteSpace(secretKey))
            return false;

        // Header formatı: t={timestamp},v1={signature}
        var parts = signatureHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var timestampPart = parts.FirstOrDefault(p => p.StartsWith("t="))?[2..];
        var signaturePart = parts.FirstOrDefault(p => p.StartsWith("v1="))?[3..];

        if (string.IsNullOrWhiteSpace(timestampPart) || string.IsNullOrWhiteSpace(signaturePart))
            return false;

        if (!long.TryParse(timestampPart, out var timestamp))
            return false;

        // 1. Replay Attack Koruması: Zaman farkı toleransı aşıldı mı? (Varsayılan 5 dakika / 300 sn)
        var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (Math.Abs(currentTimestamp - timestamp) > toleranceSeconds)
            return false;

        // 2. Beklenen imzayı hesapla
        var expectedSignature = GenerateSignature(payload, secretKey, timestamp);
        var expectedSigPart = expectedSignature.Split(',').FirstOrDefault(p => p.StartsWith("v1="))?[3..];

        if (string.IsNullOrWhiteSpace(expectedSigPart))
            return false;

        // 3. Timing Attack Koruması: Sabit zamanlı karşılaştırma (FixedTimeEquals)
        var expectedBytes = Encoding.UTF8.GetBytes(expectedSigPart);
        var actualBytes = Encoding.UTF8.GetBytes(signaturePart);

        return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }
}
