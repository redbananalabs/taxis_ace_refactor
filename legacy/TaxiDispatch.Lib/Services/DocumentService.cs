using TaxiDispatch.Data;
using TaxiDispatch.Domain;
using TaxiDispatch.Configuration;
using Dropbox.Api;
using Dropbox.Api.Files;
using Dropbox.Api.Sharing;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;

namespace TaxiDispatch.Services
{
    public class DocumentService : BaseService<DocumentService>
    {
        private readonly DropBoxConfig _config;
        private DropboxClient _client;
        private readonly UINotificationService _notificationService;


        public DocumentService(IDbContextFactory<TaxiDispatchContext> factory, 
            UINotificationService notificationService,
            IOptions<DropBoxConfig> config, ILogger<DocumentService> logger) : base(factory, logger) 
        {
            _config = config.Value;
            _notificationService = notificationService;
        }

        const string InsuranceFilePrefix = "Insurance Certificate ";
        const string MotFilePrefix = "MOT Certificate ";
        const string DbsFilePrefix = "DBS Certificate ";
        const string VehicleLicenceFilePrefix = "Vehicle Licence ";
        const string DriversLicenceFilePrefix = "Drivers Licence ";
        const string SafeGuardingFilePrefix = "Safe Guarding ";
        const string FirstAidCertFilePrefix = "First Aid Certificate ";
        const string ProfilePicFilePrefix = "Profile Picture ";


        public async Task<string> UploadDocument(int userId, string fullname,IFormFile file, DocumentType type)
        {
            await InitDropBox();

            var dbboxPath = "/Driver Documents/";

            var date = DateTime.Now.ToString("dd-MM-yy");

            var fname = await _dB.Users.Where(o => o.Id == userId)
                .Select(o => o.FullName)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrEmpty(fname))
            {
                dbboxPath = $"/{userId} - {fname}/";
            }
            else
            { 
                dbboxPath = $"/{userId}/"; 
            }

            switch (type)
            {
                case DocumentType.Insurance:
                    dbboxPath += InsuranceFilePrefix + date;
                    break;
                case DocumentType.MOT:
                    dbboxPath += MotFilePrefix + date;
                    break;
                case DocumentType.DBS:
                    dbboxPath += DbsFilePrefix + date;
                    break;
                case DocumentType.VehicleBadge:
                    dbboxPath += VehicleLicenceFilePrefix + date;
                    break;
                case DocumentType.DriverLicence:
                    dbboxPath += DriversLicenceFilePrefix + date;
                    break;
                case DocumentType.SafeGuarding:
                    dbboxPath += SafeGuardingFilePrefix + date;
                    break;
                case DocumentType.FirstAidCert:
                    dbboxPath += FirstAidCertFilePrefix + date;
                    break;
                case DocumentType.DriverPhoto:
                    dbboxPath += ProfilePicFilePrefix + date;
                    break;
            }

            dbboxPath = dbboxPath + ".jpg";

            var res = await UploadFileAsync(file, dbboxPath);

            await _notificationService.AddDocumentUploadNotification(userId, fullname, type, res);

            return res;
        }

        public async Task UploadInvoice(int accno, string accName, Stream stream, string filename)
        {
            await InitDropBox();
            var dbboxPath = $"/Invoices/{accno} - {accName}/{filename}";
            await UploadFileAsync(stream, dbboxPath);
        }

        public async Task UploadCreditNote(int accno, string accName, Stream stream, string filename)
        {
            await InitDropBox();
            var dbboxPath = $"/CreditNotes/{accno} - {accName}/{filename}";
            await UploadFileAsync(stream, dbboxPath);
        }

        public async Task UploadStatement(int userId, string fullname, Stream stream, string filename)
        {
            await InitDropBox();
            var dbboxPath = $"/Statements/{userId} - {fullname.Trim()}/{filename}";
            await UploadFileAsync(stream, dbboxPath);
        }

        private async Task InitDropBox()
        {
            //var refresh = string.Empty;
            // authorize
            var refresh = _config.RefreshToken;

            if (string.IsNullOrEmpty(refresh))
            {
                throw new Exception("the refresh token is empty or invalid.");
            //    refresh = await GetRefreshToken(); // needs fix
            }

            var accessToken = await GetAccessToken(refresh);

            _client = new DropboxClient(accessToken);
        }

        #region Private DropBox
        private async Task<string> GetAccessToken(string refreshToken)
        {
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.dropbox.com/oauth2/token");
                request.Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("refresh_token", refreshToken),
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("client_id", _config.ApiKey),
                    new KeyValuePair<string, string>("client_secret", _config.ApiSecret)
                });

                var response = await client.SendAsync(request);
                string json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    JObject tokenResponse = JObject.Parse(json);
                    return tokenResponse["access_token"].ToString();
                }
                else
                {
                    throw new Exception("Failed to refresh token: " + json);
                }
            }
        }

        private async Task<string> GetRefreshToken()
        {
            string authorizeUri = $"https://www.dropbox.com/oauth2/authorize?client_id={_config.ApiKey}&response_type=code&token_access_type=offline";

            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.dropboxapi.com/oauth2/token");
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("code", _config.AuthCode),
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    new KeyValuePair<string, string>("client_id", _config.ApiKey),
                    new KeyValuePair<string, string>("client_secret", _config.ApiSecret) // REMOVE token_access_type
                });

                request.Content = content;
                var response = await client.SendAsync(request);

                // Debugging: Print response details
                var responseString = await response.Content.ReadAsStringAsync();

                response.EnsureSuccessStatusCode();

                var json = JObject.Parse(responseString);
                var refreshToken = json["refresh_token"]?.ToString();

                return refreshToken;
            }
        }
     
        private async Task<string> UploadFileAsync(IFormFile file, string dropboxPath)
        {
            await using var fileStream = file.OpenReadStream();
            var response = await _client.Files.UploadAsync(
                dropboxPath,
                WriteMode.Overwrite.Instance,
                body: fileStream);

            //return response;
            return await GetSharedLink(dropboxPath);
        }

        private async Task<string> UploadFileAsync(Stream stream, string dropboxPath)
        {
            var response = await _client.Files.UploadAsync(
                dropboxPath,
                WriteMode.Overwrite.Instance, body: stream);

            //return response;
            return await GetSharedLink(dropboxPath);
        }

        private async Task<string> GetSharedLink(string filePath)
        {
            try
            {
                var sharedLinkMetadata = await _client.Sharing.CreateSharedLinkWithSettingsAsync(filePath);
                return ConvertToDirectLink(sharedLinkMetadata.Url);
            }
            catch (ApiException<CreateSharedLinkWithSettingsError> ex)
            {
                if (ex.ErrorResponse.IsSharedLinkAlreadyExists)
                {
                    // If a shared link already exists, retrieve it
                    var listSharedLinks = await _client.Sharing.ListSharedLinksAsync(filePath);
                    if (listSharedLinks.Links.Count > 0)
                    {
                        return ConvertToDirectLink(listSharedLinks.Links[0].Url);
                    }
                }
                throw;
            }
        }

        private string ConvertToDirectLink(string dropboxUrl)
        {
            dropboxUrl = dropboxUrl.Replace("&dl=0", "&raw=1"); // Convert to a direct download link
            return dropboxUrl;
        }
        #endregion
    }
}


