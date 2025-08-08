using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Identity.Client;
using MimeKit;

namespace SmtpOAuth
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Azure AD app registration details
            var tenantId = "6a3548ab-7570-4271-91a8-58da00697029";
            var clientId = "YOUR_CLIENT_ID";
            var clientSecret = "YOUR_CLIENT_SECRET";
            var userEmail = "YOUR_EMAIL_ADDRESS";

            // Acquire token using MSAL
            var confidentialClient = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithClientSecret(clientSecret)
                .WithTenantId(tenantId)
                .Build();

            var scopes = new[] { "https://outlook.office365.com/.default" };
            var result = await confidentialClient
                .AcquireTokenForClient(scopes)
                .ExecuteAsync();

            var accessToken = result.AccessToken;

            // Create the email
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Sender Name", userEmail));
            message.To.Add(new MailboxAddress("Recipient Name", "recipient@example.com"));
            message.Subject = "Test Email via SMTP OAuth";
            message.Body = new TextPart("plain")
            {
                Text = "This is a test email sent using SMTP with OAuth2."
            };

            // Send the email using MailKit
            using (var client = new SmtpClient())
            {
                await client.ConnectAsync("smtp.office365.com", 587, SecureSocketOptions.StartTls);
                // Authenticate using OAuth2
                var oauth2 = new SaslMechanismOAuth2(userEmail, accessToken);
                await client.AuthenticateAsync(oauth2);

                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }

            Console.WriteLine("Email sent successfully.");
        }
    }
}
