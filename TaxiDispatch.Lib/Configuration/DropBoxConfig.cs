using System.Text.Json;

namespace TaxiDispatch.Configuration
{
    public class DropBoxConfig
    {
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
        public string AuthCode { get; set; }
        public string AccessCode { get; set; }
        public string RefreshToken { get; set; }

        /// <summary>
        /// Updates the appsettings.json file with the new refresh token.
        /// </summary>
        /// <param name="refreshToken">The new refresh token to save.</param>
        /// <param name="configPath">The path to the appsettings.json file.</param>
        public void UpdateRefreshToken(string refreshToken, string configPath = "appsettings.json")
        {
            // Load the existing appsettings.json file
            var json = File.ReadAllText(configPath);

            // Parse the JSON
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { AllowTrailingCommas = true });
            var root = document.RootElement;

            // Modify the "Dropbox:RefreshToken" value
            var jsonObject = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            if (jsonObject == null) throw new Exception("Failed to parse appsettings.json");

            if (jsonObject.TryGetValue("Dropbox", out var dropboxSection) && dropboxSection is JsonElement dropboxElement)
            {
                var dropboxSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(dropboxElement.GetRawText());
                if (dropboxSettings != null)
                {
                    dropboxSettings["RefreshToken"] = refreshToken;
                    jsonObject["Dropbox"] = dropboxSettings;
                }
            }

            // Serialize the updated settings back to the appsettings.json file
            var updatedJson = JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, updatedJson);
        }
    }
}

