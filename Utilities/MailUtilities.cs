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

        // Read clientIdFile

        // Due to NativeAoT, this code does not work. Below is the error message:

        /*Unhandled Exception: Newtonsoft.Json.JsonSerializationException: Unable to find a constructor to use for type Google.Apis.Auth.OAuth2.GoogleClientSecrets. A class should either have a default constructor, one constructor with arguments or a constructor marked with the JsonConstructor attribute. Path 'installed', line 1, position 13.
             at Newtonsoft.Json.Serialization.JsonSerializerInternalReader.CreateNewObject(JsonReader, JsonObjectContract, JsonProperty, JsonProperty, String, Boolean&) + 0x1a6
             at Newtonsoft.Json.Serialization.JsonSerializerInternalReader.CreateObject(JsonReader, Type, JsonContract, JsonProperty, JsonContainerContract, JsonProperty, Object) + 0x2cf
             at Newtonsoft.Json.Serialization.JsonSerializerInternalReader.CreateValueInternal(JsonReader, Type, JsonContract, JsonProperty, JsonContainerContract, JsonProperty, Object) + 0x9e
             at Newtonsoft.Json.Serialization.JsonSerializerInternalReader.Deserialize(JsonReader, Type, Boolean) + 0x25f
             at Newtonsoft.Json.JsonSerializer.DeserializeInternal(JsonReader, Type) + 0x100
             at Google.Apis.Json.NewtonsoftJsonSerializer.Deserialize[T](Stream) + 0xa2
             at Google.Apis.Auth.OAuth2.GoogleClientSecrets.FromFile(String) + 0x56
             at WebPing.Utilities.MailUtilities.ReadGoogleCredentials() + 0x42
             at Program.<<Main>$>d__0.MoveNext() + 0x13c
             --- End of stack trace from previous location ---
             at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw() + 0x1c
             at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task) + 0xbe
             at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task, ConfigureAwaitOptions) + 0x4e   
                  at Program.<Main>(String[] args) + 0x24
                  at WebPing!<BaseAddress>+0x15192dc */

        // Leave time so developers can connect to the container and inspect its filesystem

        try
        {
            // Try access "webping-googleapis-data" folder and read token.json

            // 1. List files in "webping-googleapis-data" folder
            var folderContents = Directory.GetFiles("webping-googleapis-data");

            // 2. Try read the first file in the folder
            if (folderContents.Length > 0)
            {
                var tokenFile = folderContents[0];
                var tokenContent = File.ReadAllText(tokenFile);
                Console.WriteLine("Token file content: " + tokenContent);
            }
            else
            {
                Console.WriteLine("No files found in webping-googleapis-data folder.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error accessing webping-googleapis-data folder: " + ex.Message);
            while (true)
                System.Threading.Thread.Sleep(60000); // Sleep for 1 minute to allow inspection of the folder
        }

        var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
            GoogleClientSecrets.FromFile(clientIdFile).Secrets,
            scopes,
            "user",
            CancellationToken.None,
            new FileDataStore("webping-googleapis-data", true)).Result;

        var accessToken = credential.GetAccessTokenForRequestAsync().Result;
        // Console.WriteLine("Access Token: " + accessToken);
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