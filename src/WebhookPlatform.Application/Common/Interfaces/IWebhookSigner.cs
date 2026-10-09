namespace WebhookPlatform.Application.Common.Interfaces;

/// <summary>
/// Webhook payload'larını HMAC SHA-256 ile imzalama ve doğrulama arayüzü.
/// </summary>
public interface IWebhookSigner
{
    /// <summary>
    /// Verilen payload ve gizli anahtardan HMAC SHA-256 imzası üretir.
    /// Format: t={timestamp},v1={signature}
    /// </summary>
    string GenerateSignature(string payload, string secretKey, long timestamp);

    /// <summary>
    /// Gelen imzanın geçerli olup olmadığını ve replay attack süresini kontrol eder.
    /// </summary>
    bool VerifySignature(string payload, string signatureHeader, string secretKey, long toleranceSeconds = 300);
}
