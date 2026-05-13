//-------------------------------------------------------------------------------------------------
//
// ResendEmailSender.cs -- The ResendEmailSender class.
//
// Copyright (c) 2026 JBT Marel. All rights reserved.
//
//-------------------------------------------------------------------------------------------------
using Arelia.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Resend;

namespace Arelia.Infrastructure.Services;

/// <summary>
/// Sends transactional emails (confirmation links, password resets) via the Resend API.
/// Configure the API token via <c>Resend:ApiToken</c> in app settings or the
/// <c>Resend__ApiToken</c> environment variable.
/// </summary>
public sealed class ResendEmailSender(IResend resend, IConfiguration configuration) : IEmailSender<ApplicationUser>
{
	//-----------------------------------------------------------------------------------------
	/// <inheritdoc />
	public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
	{
		const string subject = "One small click for you, one giant leap for your choir";

		var body = BuildEmailLayout(
			heading: "Welcome to Arelia! 🎵",
			preheader: "Confirm your email to get started.",
			paragraphs:
			[
				"Your account is ready and waiting — it just needs to know you're really you. (We're careful like that.)",
				"Click the button below and you'll be all set to manage rehearsals, track attendance, and generally keep things harmonious."
			],
			buttonText: "Confirm my email",
			buttonUrl: confirmationLink,
			footerNote: "If you didn't create an Arelia account, you can safely ignore this email. Nothing will change, we promise."
		);

		return SendAsync(email, subject, body);
	}

	//-----------------------------------------------------------------------------------------
	/// <inheritdoc />
	public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
	{
		const string subject = "Forgot your password? No judgement.";

		var body = BuildEmailLayout(
			heading: "Let's get you back in tune 🎼",
			preheader: "Reset your Arelia password.",
			paragraphs:
			[
				"It happens to the best of us. Click the button below and you'll be back on stage in no time.",
				"This link is valid for a limited time, so don't let it sit too long — unlike a fine wine, password reset links do not improve with age."
			],
			buttonText: "Reset my password",
			buttonUrl: resetLink,
			footerNote: "If you didn't request a password reset, you can ignore this email. Your password will remain unchanged."
		);

		return SendAsync(email, subject, body);
	}

	//-----------------------------------------------------------------------------------------
	/// <inheritdoc />
	public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
	{
		const string subject = "Your secret password reset code 🤫";

		var body = BuildEmailLayout(
			heading: "Your reset code has arrived",
			preheader: "Use this code to reset your Arelia password.",
			paragraphs:
			[
				"Someone — presumably you — requested a password reset. Guard this code like the setlist for opening night:"
			],
			codeBlock: resetCode,
			footerNote: "If you didn't request this, please ignore this email. Your account is safe and sound."
		);

		return SendAsync(email, subject, body);
	}

	//-----------------------------------------------------------------------------------------
	private async Task SendAsync(string toEmail, string subject, string htmlBody)
	{
		var from = configuration["Resend:From"] ?? "noreply@arnfada.com";

		var message = new EmailMessage();
		message.From = from;
		message.To.Add(toEmail);
		message.Subject = subject;
		message.HtmlBody = htmlBody;

		await resend.EmailSendAsync(message);
	}

	//-----------------------------------------------------------------------------------------
	private static string BuildEmailLayout(
		string heading,
		string preheader,
		string[] paragraphs,
		string? buttonText = null,
		string? buttonUrl = null,
		string? codeBlock = null,
		string? footerNote = null)
	{
		var paragraphHtml = string.Join("\n", paragraphs.Select(p =>
			$"""<p style="margin:0 0 16px 0;font-size:15px;line-height:1.6;color:#342f2a;">{p}</p>"""));

		var buttonHtml = buttonText is not null && buttonUrl is not null
			? $"""
			  <table width="100%" cellpadding="0" cellspacing="0" style="margin-top:8px;margin-bottom:8px;">
			    <tr>
			      <td align="center" style="padding:24px 0;">
			        <a href="{buttonUrl}"
			           style="display:inline-block;background-color:#b98962;color:#fffdf9;font-size:15px;font-weight:600;
			                  text-decoration:none;padding:14px 36px;border-radius:10px;letter-spacing:0.2px;">
			          {buttonText}
			        </a>
			      </td>
			    </tr>
			  </table>
			  <p style="margin:0 0 16px 0;font-size:13px;line-height:1.5;color:#6a6057;">
			    If the button doesn't work, copy and paste this link into your browser:<br>
			    <a href="{buttonUrl}" style="color:#b98962;word-break:break-all;">{buttonUrl}</a>
			  </p>
			  """
			: string.Empty;

		var codeHtml = codeBlock is not null
			? $"""
			  <table width="100%" cellpadding="0" cellspacing="0">
			    <tr>
			      <td align="center" style="padding:24px 0;">
			        <div style="display:inline-block;background-color:#f6efe6;border:1px solid #ded4c7;
			                    border-radius:10px;padding:18px 40px;">
			          <span style="font-size:32px;font-weight:700;letter-spacing:6px;color:#342f2a;font-family:monospace;">
			            {codeBlock}
			          </span>
			        </div>
			      </td>
			    </tr>
			  </table>
			  """
			: string.Empty;

		var footerHtml = footerNote is not null
			? $"""<p style="margin:4px 0 0 0;font-size:13px;color:#6a6057;">{footerNote}</p>"""
			: string.Empty;

		return $"""
		        <!DOCTYPE html>
		        <html lang="en">
		        <head>
		          <meta charset="utf-8">
		          <meta name="viewport" content="width=device-width, initial-scale=1.0">
		          <meta name="color-scheme" content="light">
		          <!--[if !mso]><!-->
		          <style>@import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;600;700&display=swap');</style>
		          <!--<![endif]-->
		        </head>
		        <body style="margin:0;padding:0;background-color:#f7f3ec;
		                     font-family:'Inter','Roboto',system-ui,-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;">
		          <!-- Preheader (hidden preview text) -->
		          <div style="display:none;max-height:0;overflow:hidden;mso-hide:all;font-size:1px;color:#f7f3ec;">
		            {preheader}
		          </div>

		          <table width="100%" cellpadding="0" cellspacing="0"
		                 style="background-color:#f7f3ec;padding:48px 20px;">
		            <tr>
		              <td align="center">
		                <table width="100%" cellpadding="0" cellspacing="0" style="max-width:580px;">

		                  <!-- Logo / wordmark -->
		                  <tr>
		                    <td style="padding-bottom:28px;text-align:center;">
		                      <span style="font-size:26px;font-weight:700;color:#6f6256;letter-spacing:-0.5px;">Arelia</span>
		                    </td>
		                  </tr>

		                  <!-- Card -->
		                  <tr>
		                    <td style="background-color:#fffdf9;border-radius:16px;border:1px solid #ded4c7;
		                               box-shadow:0 10px 28px rgba(73,61,49,0.08);padding:40px 48px;">

		                      <!-- Heading -->
		                      <h1 style="margin:0 0 20px 0;font-size:22px;font-weight:700;
		                                 color:#342f2a;line-height:1.3;">
		                        {heading}
		                      </h1>

		                      <!-- Body paragraphs -->
		                      {paragraphHtml}

		                      <!-- CTA button (optional) -->
		                      {buttonHtml}

		                      <!-- Code block (optional) -->
		                      {codeHtml}

		                    </td>
		                  </tr>

		                  <!-- Footer -->
		                  <tr>
		                    <td style="padding:28px 8px 0;text-align:center;">
		                      {footerHtml}
		                      <p style="margin:12px 0 0 0;font-size:12px;color:#6a6057;">
		                        This mailbox does not accept replies. Feel free to try — the void will receive your message with great indifference.
		                      </p>
		                      <p style="margin:8px 0 0 0;font-size:12px;color:#6a6057;">
		                        © 2026 Arelia · You're receiving this because an account action was performed.
		                      </p>
		                    </td>
		                  </tr>

		                </table>
		              </td>
		            </tr>
		          </table>
		        </body>
		        </html>
		        """;
	}
}
