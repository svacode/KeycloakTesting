using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace KeycloakTesting.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly string _keycloakBaseUrl = "http://localhost:8080"; // Keycloak URL
        private readonly string _realm = "myrealm"; // Your Keycloak realm name
        private readonly string _adminClientId = "admin-cli"; // Admin client id
        private readonly string _adminUsername = "seva"; // Admin username
        private readonly string _adminPassword = "seva99"; // Admin password

        public UserController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }


        [HttpPost("register")]
        public async Task<IActionResult> RegisterUser([FromBody] UserRegistrationRequest model)
        {
            // Get the Admin token to authenticate with Keycloak
            var token = await GetAdminToken();
            if (token == null)
            {
                return Unauthorized("Unable to get admin token");
            }

            // Create user in Keycloak
            var userCreated = await CreateUserInKeycloak(token, model);
            if (userCreated)
            {
                return Ok("User registered successfully");
            }

            return BadRequest("Failed to register user");
        }

        // Get an Admin token from Keycloak
        private async Task<string> GetAdminToken()
        {
            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, $"{_keycloakBaseUrl}/realms/master/protocol/openid-connect/token");

            tokenRequest.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", _adminClientId),
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("username", _adminUsername),
                new KeyValuePair<string, string>("password", _adminPassword)
            });

            var tokenResponse = await _httpClient.SendAsync(tokenRequest);
            if (!tokenResponse.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await tokenResponse.Content.ReadAsStringAsync();
            var tokenData = JsonConvert.DeserializeObject<dynamic>(json);
            return tokenData?.access_token;
        }

        // Create a user in Keycloak
        private async Task<bool> CreateUserInKeycloak(string token, UserRegistrationRequest model)
        {
            var userPayload = new
            {
                username = model.Username,
                email = model.Email,
                enabled = true,
                credentials = new[]
                {
                    new { type = "password", value = model.Password, temporary = false }
                }
            };

            var requestContent = new StringContent(JsonConvert.SerializeObject(userPayload), Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.PostAsync($"{_keycloakBaseUrl}/admin/realms/{_realm}/users", requestContent);

            return response.IsSuccessStatusCode;
        }
    }

    // Model for user registration
    public class UserRegistrationRequest
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }
}


