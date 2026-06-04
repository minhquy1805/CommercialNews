using System.Collections.Frozen;
using System.Text.Json;
using System.Text.Json.Nodes;
using Audit.Domain.Policies.Redaction;

namespace Audit.Infrastructure.Redaction;

public sealed class DefaultAuditRedactionPolicy : IAuditRedactionPolicy
{
    private const string PolicyNameValue = "DefaultAuditRedactionPolicy";
    private const string RedactionVersionValue = "v1";
    private const string RedactedValue = "[REDACTED]";

    private static readonly FrozenSet<string> SensitiveFieldNames = new[]
    {
        "password",
        "passwordHash",
        "password_hash",
        "oldPassword",
        "old_password",
        "newPassword",
        "new_password",
        "confirmPassword",
        "confirm_password",
        "accessToken",
        "access_token",
        "refreshToken",
        "refresh_token",
        "idToken",
        "id_token",
        "verificationToken",
        "verification_token",
        "emailVerificationToken",
        "email_verification_token",
        "resetToken",
        "reset_token",
        "passwordResetToken",
        "password_reset_token",
        "securityStamp",
        "security_stamp",
        "token",
        "jwt",
        "bearerToken",
        "bearer_token",
        "authorization",
        "authorizationHeader",
        "authorization_header",
        "rawAuthorizationHeader",
        "raw_authorization_header",
        "cookie",
        "cookies",
        "sessionCookie",
        "session_cookie",
        "setCookie",
        "set-cookie",
        "apiKey",
        "api_key",
        "api-key",
        "xApiKey",
        "x-api-key",
        "secret",
        "clientSecret",
        "client_secret",
        "providerSecret",
        "provider_secret",
        "configurationSecret",
        "configuration_secret",
        "privateKey",
        "private_key",
        "privateSigningKey",
        "private_signing_key",
        "signingKey",
        "signing_key",
        "connectionString",
        "connection_string",
        "smtpPassword",
        "smtp_password",
        "databasePassword",
        "database_password"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenSet<string> BlockedFieldNames = new[]
    {
        "rawPassword",
        "raw_password",
        "plainTextPassword",
        "plain_text_password",
        "creditCardNumber",
        "credit_card_number",
        "cardNumber",
        "card_number",
        "cvv",
        "cvc",
        "governmentId",
        "government_id",
        "governmentIdentityNumber",
        "government_identity_number",
        "nationalId",
        "national_id",
        "identityNumber",
        "identity_number"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public AuditRedactionResult Redact(
        string sourceModule,
        string eventType,
        string? jsonPayload)
    {
        return RedactJson(
            jsonPayload,
            pathPrefix: "$");
    }

    public AuditRedactionResult RedactHeaders(
        string sourceModule,
        string eventType,
        string? headersJson)
    {
        return RedactJson(
            headersJson,
            pathPrefix: "$.headers");
    }

    public AuditRedactionResult RedactMetadata(
        string sourceModule,
        string eventType,
        string? metadataJson)
    {
        return RedactJson(
            metadataJson,
            pathPrefix: "$.metadata");
    }

    private static AuditRedactionResult RedactJson(
        string? json,
        string pathPrefix)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return AuditRedactionResult.Allowed(
                sanitizedJson: null,
                policyName: PolicyNameValue,
                redactionVersion: RedactionVersionValue);
        }

        JsonNode? rootNode;

        try
        {
            rootNode = JsonNode.Parse(json);
        }
        catch (JsonException)
        {
            return AuditRedactionResult.Blocked(
                reason: "Audit payload contains invalid JSON and cannot be redacted safely.",
                blockedFields: new[] { pathPrefix },
                policyName: PolicyNameValue,
                redactionVersion: RedactionVersionValue);
        }

        if (rootNode is null)
        {
            return AuditRedactionResult.Allowed(
                sanitizedJson: null,
                policyName: PolicyNameValue,
                redactionVersion: RedactionVersionValue);
        }

        var redactedFields = new List<string>();
        var blockedFields = new List<string>();

        RedactNode(
            rootNode,
            pathPrefix,
            redactedFields,
            blockedFields);

        if (blockedFields.Count > 0)
        {
            return AuditRedactionResult.Blocked(
                reason: "Audit payload contains fields that are not allowed to be persisted.",
                blockedFields: blockedFields,
                policyName: PolicyNameValue,
                redactionVersion: RedactionVersionValue);
        }

        string sanitizedJson = rootNode.ToJsonString(JsonOptions);

        return AuditRedactionResult.Allowed(
            sanitizedJson: sanitizedJson,
            policyName: PolicyNameValue,
            redactionVersion: RedactionVersionValue,
            redactedFields: redactedFields);
    }

    private static void RedactNode(
        JsonNode node,
        string currentPath,
        List<string> redactedFields,
        List<string> blockedFields)
    {
        if (node is JsonObject jsonObject)
        {
            RedactObject(
                jsonObject,
                currentPath,
                redactedFields,
                blockedFields);

            return;
        }

        if (node is JsonArray jsonArray)
        {
            RedactArray(
                jsonArray,
                currentPath,
                redactedFields,
                blockedFields);
        }
    }

    private static void RedactObject(
        JsonObject jsonObject,
        string currentPath,
        List<string> redactedFields,
        List<string> blockedFields)
    {
        foreach (var property in jsonObject.ToArray())
        {
            string propertyName = property.Key;
            JsonNode? propertyValue = property.Value;
            string propertyPath = $"{currentPath}.{propertyName}";

            if (BlockedFieldNames.Contains(propertyName))
            {
                blockedFields.Add(propertyPath);
                continue;
            }

            if (SensitiveFieldNames.Contains(propertyName))
            {
                jsonObject[propertyName] = RedactedValue;
                redactedFields.Add(propertyPath);
                continue;
            }

            if (propertyValue is not null)
            {
                RedactNode(
                    propertyValue,
                    propertyPath,
                    redactedFields,
                    blockedFields);
            }
        }
    }

    private static void RedactArray(
        JsonArray jsonArray,
        string currentPath,
        List<string> redactedFields,
        List<string> blockedFields)
    {
        for (int index = 0; index < jsonArray.Count; index++)
        {
            JsonNode? item = jsonArray[index];

            if (item is null)
            {
                continue;
            }

            RedactNode(
                item,
                $"{currentPath}[{index}]",
                redactedFields,
                blockedFields);
        }
    }
}
