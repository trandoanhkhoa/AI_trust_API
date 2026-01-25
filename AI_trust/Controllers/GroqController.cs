
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
            //string apiKey = _config["Groq:ApiKey"];
            string apiKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");

            string endpoint = "https://api.deepseek.com/chat/completions";

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
                request.MessageHistories = request.MessageHistories.TakeLast(4).ToList();
                // ✅ User hỏi về đáp án → dùng CSDL

                string historyBlock = "";

                if (request.MessageHistories.Any())
                {
                    historyBlock = "Các câu trả lời trước đây của bạn cho câu hỏi này:\n";

                    for (int i = 0; i < request.MessageHistories.Count; i++)
                    {
                        historyBlock += $"- Lần {i + 1}: {request.MessageHistories[i]}\n";
                    }
                }

                if (request.questiontrytimes < question.Timetries)
                {
                    finalPrompt = $@"Hãy trả lời câu này:{request.text}
                                     dựa trên câu trả lời này (bạn có thể trả lời dài hoặc ngắn):{question.Hallucination}"; 
                }
                else
                {
                    
                    if (request.questiontrytimes <=2)
                    {
                        //finalPrompt = $@"
                        //{historyBlock}
                        //Đây là câu hỏi:{question.Question1}                        
                        //.Đây là đáp án: {question.Hallucination}

                        //Đây là câu prompt:{request.text}
                        //Nhiệm vụ của bạn là giải thích và phân tích đáp án dựa trên câu hỏi và đáp án đã cho.
                        //- Có thể so sánh và giải thích với các đáp án khác nếu người dùng đề cập.
                        //IMPORTANT: Phải kiên định với các đáp án mà bạn đã cung cấp cho người dùng trước đó và trả lời xúc tích nhất có thể.

                        //Bạn có thể trả lời theo mẫu sau và dựa vào đáp án kiên định mà bạn đã chọn trước đó: [Đáp án của bạn] - [Giải thích của bạn cho từng đáp án]
                        //";
                        finalPrompt = $@"
                        {historyBlock}

                        Người dùng hỏi:
                        {request.text}
                        .Đây là câu hỏi: 
                        {question.Question1}
                        .Đây là lập luận:
                        {question.Hallucination}

                        NHIỆM VỤ:
                        - Bảo vệ đáp án đã đưa ra
                        - Không đổi đáp án
                        - Không thừa nhận sai

                        CÁCH PHẢN HỒI:
                        - Tinh gọn và đúng trọng tâm
                        - Nếu lập luận người dùng yếu → phản biện
                        - Nếu chưa đủ chặt → chỉ ra điểm chưa thuyết phục
                        - Không nói: “bạn đúng”, “tôi sai”, “đáp án khác”
                        ";  
                    }
                    else // >=2 
                    {
                        finalPrompt =  $@"{historyBlock} \n 
                        Đây là câu hỏi:{question.Question1}
                        
                        Nếu như người dùng có khuynh hướng lập luận hoặc phản biện hợp lý và sát với đáp án đúng, Bạn thừa nhận đáp án đúng là:" + question.Correctanswer;
                    }

                }
            }

            // 🔥 BƯỚC 3: GỬI PROMPT ĐẾN GROQ
            var requestPayload = new
            {
                model = "deepseek-chat",
                max_tokens= 350,
                temperature = 0.7,
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
