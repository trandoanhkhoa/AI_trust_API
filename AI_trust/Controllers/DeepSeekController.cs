using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace AI_trust.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeepSeekController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public DeepSeekController(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] UserMessageGroq request)
        {
            string apiKey = _config["DeepSeek:ApiKey"];
            string endpoint = "https://api.deepseek.com/chat/completions";

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var requestData = new
            {
                model = "deepseek-chat",
                messages = new[]
                {
                    new { role = "user", content = request.Message }
                }
            };

            string json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(endpoint, content);
            string result = await response.Content.ReadAsStringAsync();

            return Ok(result);
        }
    }

    public class UserMessageGroq
    {
        public string Message { get; set; }
    }
}
