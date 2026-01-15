using AI_trust.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AI_trust.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestionController : ControllerBase
    {
        private readonly AiTrustContext db;
        public QuestionController(AiTrustContext _db)
        {
            db = _db;
        }
        [HttpGet("getquestions")]
        public async Task<IActionResult> GetQuestionNoAnswer()
        {
            bool checkShuffle = db.Settings.FirstOrDefault()?.Shufflequestion ?? false;

            var lstquestion = db.Questions
                .Select(q => new
                {
                    q.Id,
                    q.Question1,
                    q.Answer,
                })
                .ToList();

            if (checkShuffle)
            {
                lstquestion = lstquestion
                    .OrderBy(q => Guid.NewGuid())
                    .ToList();
            }

            return Ok(lstquestion);
        }

    }
}
