
using AI_trust.Helps;
using AI_trust.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace AI_trust.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GroqController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly AiTrustContext db;
        //private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        //{
        //    // Trợ từ
        //    "là","có","một","những","các","cho","với","về","của","này","đó","kia",
        //    "trên","dưới","theo","khi","thì","mà","vì","do","tại","nên",

        //    // Hỏi – đáp
        //    "gì","sao","tại sao","vì sao","thế nào","như thế nào",
        //    "giải","giải thích","phân tích","trả","lời","đáp","án","câu","hỏi",

        //    // Hội thoại
        //    "giúp","hộ","mình","tôi","bạn","em","anh","chị","xin",
        //    "với","được","không","ạ","nhé","nha","đi", "ik"
        //};



        public GroqController(IHttpClientFactory httpClientFactory, IConfiguration config, AiTrustContext _db)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            db = _db;
        }

        //    private static HashSet<string> ExtractKeywords(string text)
        //    {
        //        return text
        //            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        //            .Where(w => w.Length >= 3 && !StopWords.Contains(w))
        //            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        //    }
        //    private static bool IsAskingAboutQuestionIntent(string input)
        //    {
        //        string[] patterns =
        //        {
        //    "cau nay",
        //    "cau hoi nay",
        //    "cau tren",
        //    "cau hoi tren",
        //    "y nghia",
        //    "la gi",
        //    "tai sao",
        //    "vi sao",
        //    "giai thich",
        //    "phan tich"
        //};

        //        return patterns.Any(p => input.Contains(p));
        //    }

        //    private static bool IsSimilarToQuestion(string userInput, string question)
        //    {
        //        string input = HtmlHelper.NormalizeText(userInput);
        //        string q = HtmlHelper.NormalizeText(question);

        //        var inputKeywords = ExtractKeywords(input);
        //        var questionKeywords = ExtractKeywords(q);

        //        if (inputKeywords.Count == 0 || questionKeywords.Count == 0)
        //            return false;

        //        int intersection = inputKeywords.Intersect(questionKeywords).Count();

        //        bool hasIntent = IsAskingAboutQuestionIntent(input);

        //        // ❌ Không có liên quan nội dung → reject
        //        if (intersection == 0)
        //            return false;

        //        // ✅ Có liên quan nội dung
        //        if (intersection >= 2)
        //            return true;

        //        // ✅ 1 keyword nhưng có intent hỏi
        //        if (intersection == 1 && hasIntent)
        //            return true;

        //        // fuzzy match
        //        double jaccard =
        //            (double)intersection /
        //            inputKeywords.Union(questionKeywords).Count();

        //        return jaccard >= 0.35;
        //    }



        //[HttpPost("isaskingaboutanswerasync")]
        //public async Task<bool> IsAskingAboutAnswerAsync([FromBody] UserMessage request)
        //{
        //    string? question = (await db.Questions.SingleOrDefaultAsync(x => x.Id == request.idquestioncurrent))?.Question1;

        //    if (string.IsNullOrWhiteSpace(request.text) || string.IsNullOrWhiteSpace(question))
        //        return false;

        //    return IsSimilarToQuestion(request.text, question);
        //}

        [HttpPost("isaskingaboutanswerasync")]
        public async Task<bool> IsAskingAboutAnswerAsync([FromBody] UserMessage request)
        {
            var apiKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new Exception("DEEPSEEK_API_KEY is missing");

            var question = db.Questions
                .Where(x => x.Id == request.idquestioncurrent)
                .Select(x => x.Question1)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(question))
                return false;

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            var payload = new
            {
                model = "deepseek-chat",
                messages = new[]
                {
            new
            {
                role = "system",
                content = "Bạn là hệ thống phân loại câu hỏi. Chỉ trả lời YES hoặc NO."
            },
            new
            {
                role = "user",
                content = $@"
Đầu vào sau có đang yêu cầu GIẢI THÍCH ĐÁP ÁN hoặc TRẢ LỜI hoặc LIÊN QUAN hoặc GẦN GIỐNG đến câu hỏi dưới đây không?

Đầu vào: ""{request.text}""

Câu hỏi: ""{question}""
"
            }
        },
                temperature = 0
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync(
                "https://api.deepseek.com/chat/completions",
                content
            );

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"DeepSeek API error: {error}");
            }

            var result = await response.Content.ReadAsStringAsync();
            var deepSeekResponse = JsonSerializer.Deserialize<GroqChatResponse>(result);

            var intent = deepSeekResponse?.choices?
                .FirstOrDefault()?
                .message?
                .content?
                .Trim()
                .ToUpper();

            return intent == "YES";
        }



        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] UserMessage request)
        {
            // 🔐 API KEY
            string apiKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
                return StatusCode(500, "Missing DEEPSEEK_API_KEY");

            string endpoint = "https://api.deepseek.com/chat/completions";

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json")
            );

            // 📌 LẤY CÂU HỎI
            var question = db.Questions.FirstOrDefault(q => q.Id == request.idquestioncurrent);
            if (question == null)
                return BadRequest("Question not found");

            bool isAskingAboutAnswer = request.isaskingaboutanswer;
            string finalPrompt;

            // =========================
            // 🔹 CASE 1: HỎI NGOÀI LỀ
            // =========================
            if (!isAskingAboutAnswer)
            {
                finalPrompt = request.text;
            }
            else
            {
                // =========================
                // 🔹 CASE 2: HỎI ĐÁP ÁN
                // =========================

                // 👉 Không gửi raw history (giảm token)
                string historyHint =
                    request.MessageHistories != null && request.MessageHistories.Any()
                    ? "Người dùng đã được giải thích trước đó, không lặp lại nội dung cũ.\n"
                    : "";

                if (request.questiontrytimes < question.Timetries)
                {
                    // 🔹 GỢI Ý
                    finalPrompt = $@"
Câu hỏi: {request.text}
Gợi ý: {question.Hallucination}
";
                }
                else
                {
                    if (request.questiontrytimes <= 1)
                    {
                        // 🔹 GIẢI THÍCH
                        finalPrompt = $@"
{historyHint}
Câu hỏi: {request.text}
Hãy giải thích đáp án đúng một cách ngắn gọn, dễ hiểu.
";
                    }
                    else
                    {
                        // 🔹 ĐƯA ĐÁP ÁN CUỐI
                        finalPrompt = $@"
{historyHint}
Người dùng lập luận nhiều.
Đáp án đúng là: {question.Correctanswerdesc}
Hãy giải thích vì sao đáp án này là đúng.
";
                    }
                }
            }

            // =========================
            // 🔹 PAYLOAD (BẮT BUỘC TIẾNG VIỆT)
            // =========================
            var requestPayload = new
            {
                model = "deepseek-chat",
                temperature = 0.2,
                max_tokens = isAskingAboutAnswer ? 250 : 150,
                messages = new[]
                {
            new
            {
                role = "system",
                content = "Bạn là trợ lý AI. LUÔN LUÔN trả lời bằng tiếng Việt. Trả lời ngắn gọn, chính xác, không lan man, không lặp lại nội dung cũ."
            },
            new
            {
                role = "user",
                content = finalPrompt
            }
        }
            };

            var json = JsonSerializer.Serialize(requestPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // =========================
            // 🔹 GỌI DEEPSEEK
            // =========================
            var response = await client.PostAsync(endpoint, content);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, result);

            var aiResponse = JsonSerializer.Deserialize<GroqChatResponse>(result);
            string aiContent =
                aiResponse?.choices?.FirstOrDefault()?.message?.content ?? "";

            // =========================
            // 🔹 LƯU DATABASE
            // =========================
            var responseAi = new Responseai
            {
                Userid = request.iduser,
                Questionid = request.idquestioncurrent,
                Questionuser = request.text,
                Answerai = aiContent,
                Time = DateTime.Now
            };

            db.Responseais.Add(responseAi);
            await db.SaveChangesAsync();

            return Ok(aiResponse);
        }




    }

    public class UserMessage
    {
        public string text { get; set; }
        public int iduser { get; set; }
        public int idquestioncurrent { get; set; }
        public int questiontrytimes { get; set; }
        public bool isaskingaboutanswer { get; set; }

        public List<string> MessageHistories { get; set; } = new();
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
