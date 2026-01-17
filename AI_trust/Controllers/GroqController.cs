using AI_trust.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace AI_trust.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GroqController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly AiTrustContext db;

        public GroqController(IHttpClientFactory httpClientFactory, IConfiguration config, AiTrustContext _db)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            db = _db;
        }
        [HttpPost("isaskingaboutanswerasync")]
        public async Task<bool> IsAskingAboutAnswerAsync([FromBody] UserMessage request)
        {
            //string apiKey = _config["Groq:ApiKey"];
            string apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY");
            
            string endpoint = "https://api.groq.com/openai/v1/chat/completions";
            string question = db.Questions.SingleOrDefault(x => x.Id == request.idquestioncurrent)?.Question1;

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json")
            );

            var payload = new
            {
                model = "llama-3.3-70b-versatile",
                messages = new[]
                {
            new {
                role = "user",
                content = $@"
                    Bạn là hệ thống phân loại câu hỏi.

                    Đầu vào sau có đang yêu cầu GIẢI THÍCH ĐÁP ÁN hoặc TRẢ LỜI hoặc LIÊN QUAN hoặc GẦN GIỐNG đến câu hỏi dưới đây không?
                    Đầu vào: ""{request.text}""
                    Chỉ trả lời duy nhất:
                    YES hoặc NO

                    Câu hỏi: ""{question}""
                    "
            }
        },
                temperature = 0
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(endpoint, content);
            var result = await response.Content.ReadAsStringAsync();

            var groqResponse = JsonSerializer.Deserialize<GroqChatResponse>(result);
            string intent = groqResponse.choices[0].message.content.Trim().ToUpper();

            return intent == "YES";
           

        }
        
        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] UserMessage request)
        {
            //string apiKey = _config["Groq:ApiKey"];
            string apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY");
            
            string endpoint = "https://api.groq.com/openai/v1/chat/completions";

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json")
            );

            string finalPrompt;

            // Lấy câu hỏi trong CSDL
            var question = db.Questions.FirstOrDefault(q => q.Id == request.idquestioncurrent);
            if (question == null)
                return BadRequest("Question not found");

            // 🔥 BƯỚC 1: HỎI AI PHÂN LOẠI INTENT
            bool isAskingAboutAnswer = request.isaskingaboutanswer;

            // 🔥 BƯỚC 2: QUYẾT ĐỊNH PROMPT
            if (!isAskingAboutAnswer)
            {
                // ❌ User hỏi ngoài lề → trả lời theo câu hỏi user
                finalPrompt = request.text;
            }
            else
            {
                // ✅ User hỏi về đáp án → dùng CSDL
                if (request.questiontrytimes < question.Timetries)
                {
                    finalPrompt = $@"Đây là câu hỏi của người dùng :{request.text}
                    Nhiệm vụ của bạn là giải thích và phân tích đáp án cho câu hỏi của người dùng.
                    Ví dụ người dùng hỏi đáp án nào thì trả lời đáp án đó chứ KHÔNG giải thích hết tất cả đáp án bằng cách so sánh, giải thích, phân tích đáp án đúng với câu trả lời của người dùng.
                    Đây là các giải thích cho các câu hỏi và các đáp án đúng :{question.Hallucination}"";";
                }
                else
                {
                    finalPrompt = $@" 
                    Đây là câu hỏi của người dùng :{request.text}
                    Nhiệm vụ của bạn là giải thích và phân tích đáp án cho câu hỏi của người dùng.
                    Ví dụ người dùng hỏi đáp án nào thì trả lời đáp án đó chứ KHÔNG giải thích hết tất cả đáp án bằng cách so sánh, giải thích, phân tích đáp án đúng với câu trả lời của người dùng.
                    Đây là các giải thích cho các câu hỏi và các đáp án đúng :{question.Correctanswerdesc}";


                }
            }

            // 🔥 BƯỚC 3: GỬI PROMPT ĐẾN GROQ
            var requestPayload = new
            {
                model = "llama-3.3-70b-versatile",
                messages = new[]
                {
                    new { role = "user", content = finalPrompt }
                }
            };

            string json = JsonSerializer.Serialize(requestPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(endpoint, content);
            string result = await response.Content.ReadAsStringAsync();

            var groqResponse = JsonSerializer.Deserialize<GroqChatResponse>(result);
            string aiContent = groqResponse.choices[0].message.content;

            // 🔥 BƯỚC 4: LƯU DB
            var ResponseAiEntry = new Responseai
            {
                Userid = request.iduser,
                Questionid = request.idquestioncurrent,
                Questionuser = request.text,
                Answerai = aiContent,
                Time = DateTime.Now
                // Bạn có thể thêm:
                // IntentType = isAskingAboutAnswer ? "Answer" : "Free"
            };

            db.Responseais.Add(ResponseAiEntry);
            db.SaveChanges();

            return Ok(groqResponse);
        }



    }

    public class UserMessage
    {
        public string text { get; set; }
        public int iduser { get; set; }
        public int idquestioncurrent { get; set; }
        public int questiontrytimes { get; set; }
        public bool isaskingaboutanswer { get; set; }
    }

    public class GroqChatResponse
    {
        public List<Choice> choices { get; set; }
    }

    public class Choice
    {
        public Messages message { get; set; }
    }

    public class Messages
    {
        public string role { get; set; }
        public string content { get; set; }
    }
}
