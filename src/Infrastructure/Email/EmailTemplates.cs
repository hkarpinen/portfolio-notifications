namespace Infrastructure.Email;

internal static class EmailTemplates
{
    public static string ConfirmEmail(string displayName, string confirmUrl) => Layout(
        title: "Confirm your email",
        body: $"Hi {HtmlEncode(displayName)}, thanks for signing up. Click the button below to confirm your email address and activate your account.",
        ctaUrl: confirmUrl,
        ctaText: "Confirm Email",
        footerNote: "If you didn't create an account, you can safely ignore this email.");

    public static string PasswordReset(string displayName, string resetUrl) => Layout(
        title: "Reset your password",
        body: $"Hi {HtmlEncode(displayName)}, we received a request to reset your password. Click the button below to choose a new one. This link expires in 1 hour.",
        ctaUrl: resetUrl,
        ctaText: "Reset Password",
        footerNote: "If you didn't request a password reset, you can safely ignore this email. Your password won't change.");

    public static string HouseholdInvite(string inviterName, string householdName, string invitationCode, string joinUrl) => Layout(
        title: $"You've been invited to {HtmlEncode(householdName)}",
        body: $"{HtmlEncode(inviterName)} has invited you to join <strong>{HtmlEncode(householdName)}</strong>. Use the code below at the join page, or click the button to go there directly.",
        ctaUrl: joinUrl,
        ctaText: "Join Household",
        footerNote: $"Your invite code: <code style=\"font-family:monospace;background:#ddd2b6;padding:2px 6px;border:1px solid #cabf9f;\">{HtmlEncode(invitationCode)}</code>");

    public static string ForumInvite(string communityName, string communityUrl) => Layout(
        title: $"You've been invited to {HtmlEncode(communityName)}",
        body: $"You've been invited to join the <strong>{HtmlEncode(communityName)}</strong> community. Click the button below to check it out.",
        ctaUrl: communityUrl,
        ctaText: "View Community",
        footerNote: "If you weren't expecting this, you can ignore it.");

    private static string Layout(string title, string body, string ctaUrl, string ctaText, string footerNote) => $"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8">
          <meta name="viewport" content="width=device-width,initial-scale=1">
          <title>{HtmlEncode(title)}</title>
        </head>
        <body style="margin:0;padding:0;background-color:#f1eadb;">
          <table role="presentation" width="100%" cellpadding="0" cellspacing="0">
            <tr>
              <td align="center" style="padding:48px 20px;background-color:#f1eadb;">
                <table role="presentation" width="100%" style="max-width:560px;" cellpadding="0" cellspacing="0">
                  <tr><td style="height:3px;background-color:#b22a1a;"></td></tr>
                  <tr>
                    <td style="background-color:#ece4d0;border-left:1px solid #15120a;border-right:1px solid #15120a;padding:36px 44px 40px;">
                      <p style="margin:0 0 32px;font-family:Georgia,'Times New Roman',serif;font-size:11px;font-weight:bold;letter-spacing:0.12em;text-transform:uppercase;color:#786f56;">hankkarpinen.com</p>
                      <h1 style="margin:0 0 20px;font-family:Georgia,'Times New Roman',serif;font-size:26px;font-weight:900;letter-spacing:-0.02em;color:#15120a;line-height:1.2;">{HtmlEncode(title)}</h1>
                      <p style="margin:0 0 32px;font-family:Georgia,'Times New Roman',serif;font-size:15px;line-height:1.7;color:#3a3424;">{body}</p>
                      <a href="{ctaUrl}" style="display:inline-block;background-color:#b22a1a;color:#f1eadb;font-family:Georgia,'Times New Roman',serif;font-size:14px;font-weight:bold;text-decoration:none;padding:12px 24px;border:2px solid #7c1a0e;">{HtmlEncode(ctaText)}</a>
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:16px 44px;border:1px solid #15120a;border-top:0;background-color:#ddd2b6;">
                      <p style="margin:0 0 8px;font-family:Georgia,'Times New Roman',serif;font-size:13px;line-height:1.5;color:#3a3424;">{footerNote}</p>
                      <p style="margin:0;font-family:Georgia,'Times New Roman',serif;font-size:12px;color:#786f56;">hankkarpinen.com &middot; You received this because of your account.</p>
                    </td>
                  </tr>
                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;

    private static string HtmlEncode(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
