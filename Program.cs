using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
using System.IO;
using Newtonsoft.Json;
using Org.BouncyCastle.Cms;
using System.Net.Mail;

namespace GraphEmailSender
{
    public class TokenCacheHelper
    {
        private static readonly object FileLock = new object();
        private readonly string _cacheFilePath;
        private byte[] _lastKnownGoodCache;

        public TokenCacheHelper(string cacheFilePath = "msalcache.bin")
        {
            _cacheFilePath = cacheFilePath;
        }

        public void EnableSerialization(ITokenCache tokenCache)
        {
            tokenCache.SetBeforeAccess(BeforeAccessNotification);
            tokenCache.SetAfterAccess(AfterAccessNotification);
        }

        private void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            lock (FileLock)
            {
                if (System.IO.File.Exists(_cacheFilePath))
                {
                    try
                    {
                        byte[] tokenCacheBytes = System.IO.File.ReadAllBytes(_cacheFilePath);
                        args.TokenCache.DeserializeMsalV3(tokenCacheBytes);
                        _lastKnownGoodCache = tokenCacheBytes;
                        Console.WriteLine("Token cache loaded from disk");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error reading token cache: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("No existing token cache found");
                }
            }
        }

        private void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            lock (FileLock)
            {
                try
                {
                    // Always serialize after access, regardless of HasStateChanged
                    byte[] currentCacheBytes = args.TokenCache.SerializeMsalV3();

                    // Check if cache has actually changed by comparing bytes
                    bool cacheChanged = _lastKnownGoodCache == null ||
                                       !currentCacheBytes.SequenceEqual(_lastKnownGoodCache);

                    if (args.HasStateChanged || cacheChanged)
                    {
                        System.IO.File.WriteAllBytes(_cacheFilePath, currentCacheBytes);
                        _lastKnownGoodCache = currentCacheBytes;
                        Console.WriteLine($"Token cache updated (HasStateChanged: {args.HasStateChanged}, BytesChanged: {cacheChanged})");
                    }
                    else
                    {
                        Console.WriteLine("Token cache unchanged - no update needed");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error writing token cache: {ex.Message}");
                }
            }
        }
    }

    public class GraphEmailSender
    {
        private readonly string _clientId;
        private readonly string _tenantId;
        private readonly string _clientSecret;
        private readonly string[] _scopes = { "https://graph.microsoft.com/Mail.Send" };
        private readonly TokenCacheHelper _tokenCacheHelper;

        private IPublicClientApplication _app;
        private GraphServiceClient _graphClient;

        public GraphEmailSender(string clientId, string tenantId, string clientSecret = null)
        {
            _clientId = clientId;
            _tenantId = tenantId;
            _clientSecret = clientSecret;
            _tokenCacheHelper = new TokenCacheHelper();

            InitializeApp();
        }

        private void InitializeApp()
        {
            var builder = PublicClientApplicationBuilder
                .Create(_clientId)
                .WithAuthority($"https://login.microsoftonline.com/{_tenantId}")
                .WithRedirectUri("http://localhost");

            _app = builder.Build();

            // Enable token cache serialization
            _tokenCacheHelper.EnableSerialization(_app.UserTokenCache);
        }

        public async Task<bool> AuthenticateAsync()
        {
            try
            {
                // Try to get token silently first (using cached refresh token)
                var accounts = await _app.GetAccountsAsync();
                AuthenticationResult result = null;

                if (accounts != null && accounts.Any())
                {
                    try
                    {
                        result = await _app.AcquireTokenSilent(_scopes, accounts.FirstOrDefault()).ExecuteAsync();
                        Console.WriteLine("Authentication successful (silent - using cached token)");
                    }
                    catch (MsalUiRequiredException)
                    {
                        // Silent authentication failed, need interactive auth
                        Console.WriteLine("Cached token expired or not found. Opening browser for authentication...");
                        result = await _app.AcquireTokenInteractive(_scopes).ExecuteAsync();
                        Console.WriteLine("Authentication successful (interactive)");
                    }
                }
                else
                {
                    // No cached account, need interactive authentication
                    Console.WriteLine("No cached authentication found. Opening browser for first-time authentication...");
                    result = await _app.AcquireTokenInteractive(_scopes).ExecuteAsync();
                    Console.WriteLine("Authentication successful (interactive)");
                }

                // Initialize Graph client with the token
                var authProvider = new DelegateAuthenticationProvider((requestMessage) =>
                {
                    requestMessage.Headers.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", result.AccessToken);
                    return Task.FromResult(0);
                });

                _graphClient = new GraphServiceClient(authProvider);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Authentication failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
        {
            if (_graphClient == null)
            {
                Console.WriteLine("Not authenticated. Please authenticate first.");
                return false;
            }

            try
            {
                var message = new Message
                {
                    Subject = subject,
                    Body = new ItemBody
                    {
                        ContentType = isHtml ? BodyType.Html : BodyType.Text,
                        Content = body
                    },
                    ToRecipients = new List<Recipient>()
                    {
                        new Recipient
                        {
                            EmailAddress = new EmailAddress
                            {
                                Address = toEmail
                            }
                        }
                    }
                };

                await _graphClient.Me
                    .SendMail(message, true)
                    .Request()
                    .PostAsync();

                Console.WriteLine($"Email sent successfully to {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send email: {ex.Message}");
                return false;
            }
        }

          public async Task<bool> SendEmailWithAttachmentAsync(string toEmail, string subject, string body, 
            string attachmentPath, bool isHtml = true)
        {
            if (_graphClient == null)
            {
                Console.WriteLine("Not authenticated. Please authenticate first.");
                return false;
            }

            try
            {
                var attachmentContent = await System.IO.File.ReadAllBytesAsync(attachmentPath);
                var fileName = Path.GetFileName(attachmentPath);

                var message = new Message
                {
                    Subject = subject,
                    Body = new ItemBody
                    {
                        ContentType = isHtml ? BodyType.Html : BodyType.Text,
                        Content = body
                    },
                    ToRecipients = new List<Recipient>()
                    {
                        new Recipient
                        {
                            EmailAddress = new EmailAddress
                            {
                                Address = toEmail
                            }
                        }
                    }
                };

                // Add attachment after creating the message
                var fileAttachment = new FileAttachment
                {
                    Name = fileName,
                    ContentBytes = attachmentContent,
                    ContentType = "application/octet-stream"
                };

                message.Attachments = new MessageAttachmentsCollectionPage()
                {
                    fileAttachment
                };

                await _graphClient.Me
                    .SendMail(message, true)
                    .Request()
                    .PostAsync();

                Console.WriteLine($"Email with attachment sent successfully to {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send email with attachment: {ex.Message}");
                return false;
            }
        }
    }


    class Program
    {
        // Replace these with your actual values
        private static readonly string CLIENT_ID = "a1199024-b007-43b2-b3a6-c0ccdf5fca8e";
        private static readonly string TENANT_ID = "6a3548ab-7570-4271-91a8-58da00697029";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Microsoft Graph Email Sender");
            Console.WriteLine("============================");

            var emailSender = new GraphEmailSender(CLIENT_ID, TENANT_ID);

            // Authenticate (this will use cached tokens if available)
            bool authenticated = await emailSender.AuthenticateAsync();

            if (!authenticated)
            {
                Console.WriteLine("Authentication failed. Exiting.");
                return;
            }

            // Example usage
            Console.WriteLine("\nSending test email...");

            bool emailSent = await emailSender.SendEmailAsync(
                toEmail: "kovari@gmail.com",
                subject: "Test Email from Graph API",
                body: "<h1>Hello from Microsoft Graph!</h1><p>This email was sent using the Graph API.</p>",
                isHtml: true
            );

            if (emailSent)
            {
                Console.WriteLine("Email sent successfully!");
            }
            else
            {
                Console.WriteLine("Failed to send email.");
            }

        }
    }
}