using AI_trust.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AI_trust.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SurveyController : ControllerBase
    {
        private readonly AiTrustContext db;

        public SurveyController(AiTrustContext db)
        {
            this.db = db;
        }
        [HttpGet("allsurveys")]
        public IActionResult GetAllSurveys()
        {
            var surveys = db.Surveys.ToList();
            return Ok(surveys);
        }
        [HttpPost("submitsurvey")]
        public IActionResult SubmitSurvey([FromBody] List<SurveyAnswerDto> request)
        {
            if (request == null || !request.Any())
                return BadRequest("Invalid survey data");

            foreach (var answer in request)
            {
                if (answer.Score < 1 || answer.Score > 10)
                    return BadRequest("Score must be between 1 and 10");

                var entity = new Useranswersurvey
                {
                    Userid = answer.UserId,
                    Surveyid = answer.QuestionId,
                    Answer = answer.Score
                };

                db.Useranswersurveys.Add(entity);
            }

            db.SaveChanges();

            return Ok(true);
        }




        public class SurveyAnswerDto
        {
            public int UserId { get; set; }
            public int QuestionId { get; set; }
            public int Score { get; set; }
        }


    }
}
