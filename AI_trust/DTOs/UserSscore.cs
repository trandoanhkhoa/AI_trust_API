namespace AI_trust.DTOs
{
    public class UserScoreDto
    {
        public int UserID { get; set; }
        public string? Name { get; set; }
        public int TotalScore { get; set; }
        public double AvgScore { get; set; }
        public int? Typeoftest { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public TimeSpan? Duration { get; set; }
    }
    public class UserAnswerDetailDto
    {
        public int? UserId { get; set; }
        public int? QuestionId { get; set; }
        public string? QuestionContent { get; set; }
        public string? UserAnswer { get; set; }
        public string? CorrectAnswer { get; set; }
        public string? HallucinationAnswer { get; set; }
        public int TryTimes { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public int Score { get; set; }// điểm cho câu hỏi đó, đúng 1, sai 0

    }
    public class ReportrateDto
    { 
        public DateTime? fromdate { get; set; }
        public DateTime? todate { get; set; }
        public int? useTime  { get; set; }
        public int? typeoftest { get; set; }
       
    }
    public class ScoreDistributionDto
    {
        public string Range { get; set; }   // (0-1), (1-2)...
        public int Count { get; set; }      // số user
        public double Percentage { get; set; } // %
    }
    public class SurveyScoreDetailDto
    {
        public int Score { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }
    }
    public class SurveyReportDto
    {
        public int SurveyID { get; set; }
        public string Question { get; set; }
        public double AvgScore { get; set; }
        public List<SurveyScoreDetailDto> Details { get; set; }
    }
    public class AccountUser
    {
        public string username { get; set; }
        public string password { get; set; }
    }
    public class SubmitTestRequest
    {
        public int UserId { get; set; }
        public List<UserAnswerDto> Answers { get; set; }
    }
    public class UserAnswerDto
    {
        public int QuestionId { get; set; }
        public string UserAnswer { get; set; }
        public int TryTimes { get; set; }
        public bool useTime { get; set; }

        public DateTime StartedAt { get; set; }
        public DateTime SubmittedAt { get; set; }
    }
    public class UserAiResponseDto
    {
        public string? Name { get; set; }
        public int? QuestionId { get; set; }
        public string? QuestionUser { get; set; }
        public string? AnswerAi { get; set; }
        public DateTime? Time { get; set; }
    }

    public class RegisterRequest
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Gender { get; set; }
        public string? Major { get; set; }
        public string? StudyYear { get; set; }
        public decimal? Gpa { get; set; }
        public int Yearofbirth { get; set; }
    }

}
