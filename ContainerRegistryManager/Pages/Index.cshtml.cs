using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ContainerRegistryManager.Pages
{
    public class IndexModel : PageModel
    {
        private const string KEYVAULT_BASE_URI = "https://jvvault.vault.azure.net/secrets/";
        private const string KEYVAULT_SECRETNANE_CLIENTID = "AzureContainerRegistry-ClientId";
        private const string KEYVAULT_SECRETNANE_CLIENTSECRET = "AzureContainerRegistry-ClientSecret";

        public string TagList { get; set; }
        public string Error { get; set; }

        public async Task OnGet()
        {
            AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
            string clientId = default(string);
            string secret = default(string);

            try
            {
                var keyVaultClient = new KeyVaultClient(
                    new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));

                var clientidBundle = await keyVaultClient.GetSecretAsync($"{KEYVAULT_BASE_URI}{KEYVAULT_SECRETNANE_CLIENTID}")
                    .ConfigureAwait(false);
                clientId = clientidBundle.Value;

                var secretBundle = await keyVaultClient.GetSecretAsync($"{KEYVAULT_BASE_URI}{KEYVAULT_SECRETNANE_CLIENTSECRET}")
                    .ConfigureAwait(false);
                secret = secretBundle.Value;
            }
            catch (Exception ex)
            {
                Error = $"Something went wrong: {ex.Message}";
            }

            var byteArray = Encoding.ASCII.GetBytes($"{clientId}:{secret}");

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            var response = await client.GetAsync("https://jvreg.azurecr.io/acr/v1/pythondemo/_tags?n=100");
            var content = response.Content;
            var result = await content.ReadAsStringAsync();

            JToken parsedJson = JToken.Parse(result);
            var beautified = parsedJson.ToString(Formatting.Indented);

            TagList = beautified;
        }
    }
}
