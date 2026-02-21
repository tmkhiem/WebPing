using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.Net.Mail;
using System.Text;

namespace WebPing.Utilities;

static class MailUtilities
{
    public static UserCredential ReadGoogleCredentials()
    {
        string[] scopes = new string[] { "https://www.googleapis.com/auth/gmail.send", "https://www.googleapis.com/auth/drive" };
        var clientIdFile = "webping-credentials.json";

        var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
            GoogleClientSecrets.FromFile(clientIdFile).Secrets,
            scopes,
            "user",
            CancellationToken.None,
            new FileDataStore("webping-googleapis-data", true)).Result;

        var accessToken = credential.GetAccessTokenForRequestAsync().Result;
        Console.WriteLine("Access Token: " + accessToken);
        return credential;
    }

    public static void SendEmail(UserCredential credential, string recipient, string subject, string body)
    {
        using (var service = new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential
        }))
        {
            var mailMessage = new MailMessage
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8,
            };

            mailMessage.To.Add(recipient);

            var mimeMessage = MimeKit.MimeMessage.CreateFromMailMessage(mailMessage);

            Message gmailMessage;

            using (var ms = new MemoryStream())
            {
                mimeMessage.WriteTo(ms);
                ms.Position = 0;

                gmailMessage = new Message
                {
                    Raw = Base64UrlEncode(Encoding.UTF8.GetString(ms.ToArray())),
                };
            }

            service.Users.Messages.Send(gmailMessage, "me").Execute();
        }
    }

    public static string Base64UrlEncode(string input)
    {
        var inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
        // Special "url-safe" base64 encode.
        return Convert.ToBase64String(inputBytes)
          .Replace('+', '-')
          .Replace('/', '_')
          .Replace("=", "");
    }

}