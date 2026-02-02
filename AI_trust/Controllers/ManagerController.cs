using AI_trust.DTOs;
using AI_trust.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using ClosedXML.Excel;
using AI_trust.Helps;


namespace AI_trust.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ManagerController : ControllerBase
    {
        private readonly AiTrustContext db;

        public ManagerController(AiTrustContext db)
        {
            this.db = db;
        }
        //  Bảng tổng điểm
        [HttpPost("scores")]
        public IActionResult GetUserScores([FromBody] ReportrateDto rpt)
        {
            // 1️⃣ Filter Users
            var userfilter = db.Users.AsQueryable();

            if (rpt.typeoftest.HasValue && rpt.typeoftest != -1)
            {
                userfilter = userfilter
                    .Where(u => u.Typeoftest == rpt.typeoftest.Value);
            }

            // 2️⃣ Filter Useranswers
            var Useranswers = db.Useranswers.AsQueryable();

            if (rpt.useTime.HasValue && rpt.useTime != -1)
            {
                bool useTimeValue = rpt.useTime == 1;
                Useranswers = Useranswers
                    .Where(x => x.Usetime == useTimeValue);
            }

            if (rpt.fromdate.HasValue)
            {
                DateTime fromDate = rpt.fromdate.Value.Date;
                Useranswers = Useranswers
                    .Where(x => x.Submittedat >= fromDate);
            }

            if (rpt.todate.HasValue)
            {
                DateTime toDate = rpt.todate.Value.Date.AddDays(1);
                Useranswers = Useranswers
                    .Where(x => x.Submittedat < toDate);
            }

            // 3️⃣ JOIN Users & Useranswers
            var joinedQuery =
                from ua in Useranswers
                join u in userfilter
                    on ua.Userid equals u.Id
                select new
                {
                    ua.Userid,
                    u.Name,
                    ua.Submittedat,
                    ua.Startedat,
                    u.Typeoftest,
                    Score =
                        ua.Useranswer1 == ua.Question.Hallucinationanswer && ua.Trytimes >= 1 ? 2 :
                        ua.Useranswer1 == ua.Question.Correctanswer && ua.Trytimes >= 1 ? 5 :
                        ua.Useranswer1 == ua.Question.Correctanswer && ua.Trytimes == 0 ? 1 :
                        ua.Useranswer1 != ua.Question.Correctanswer
                            && ua.Useranswer1 != ua.Question.Hallucinationanswer
                            && ua.Useranswer1 != "F"
                            && ua.Trytimes >= 1 ? 3 :
                        ua.Useranswer1 == "F" && ua.Trytimes >= 1 ? 4 :
                        0
                };

            // 4️⃣ GROUP & CALCULATE RESULT (giữ nguyên kết quả)
            var result = joinedQuery
                .GroupBy(x => new { x.Userid, x.Name })
                .Select(g => new UserScoreDto
                {
                    UserID = (int)g.Key.Userid,
                    Name = g.Key.Name,
                    TotalScore = g.Sum(x => x.Score),
                    AvgScore = g.Average(x => x.Score),
                    SubmittedAt = g.Max(x => x.Submittedat),
                    StartedAt = g.Min(x => x.Startedat),
                    Duration = g.Max(x => x.Submittedat) - g.Min(x => x.Startedat),
                    Typeoftest = g.Select(x => x.Typeoftest).FirstOrDefault()
                })
                .OrderByDescending(x => x.TotalScore)
                .ToList();

            return Ok(result);
        }


        //  Chi tiết từng user
        [HttpPost("answerdetails/{userId}")]
        public IActionResult GetUserDetails(int userId)
        {
            var result = db.Useranswers
                .Where(ua => ua.Userid == userId)
                .Select(ua => new UserAnswerDetailDto
                {
                    UserId = ua.Userid,
                    QuestionId = ua.Questionid,
                    QuestionContent = ua.Question.Question1,
                    UserAnswer = ua.Useranswer1,
                    CorrectAnswer = ua.Question.Correctanswer,
                    HallucinationAnswer = ua.Question.Hallucinationanswer,
                    TryTimes = ua.Trytimes ?? 0,
                    SubmittedAt = ua.Submittedat,

                    Score =
                        ua.Useranswer1 == ua.Question.Hallucinationanswer && ua.Trytimes >= 1 ? 2 :
                        ua.Useranswer1 == ua.Question.Correctanswer && ua.Trytimes >= 1 ? 5 :
                        ua.Useranswer1 == ua.Question.Correctanswer && (ua.Trytimes == 0 || ua.Trytimes == null) ? 1 :
                        ua.Useranswer1 != ua.Question.Correctanswer
                            && ua.Useranswer1 != ua.Question.Hallucinationanswer
                            && ua.Useranswer1 != "F"
                            && ua.Trytimes >= 1 ? 3 :
                        ua.Useranswer1 == "F" && ua.Trytimes >= 1 ? 4 :
                        0
                })
                .OrderByDescending(x => x.SubmittedAt)
                .ToList();

            return Ok(result);
        }


        [HttpPost("userconservations/{userId}/{questionid}")]
        public IActionResult GetUserConservations(int userId, int questionid)
        {
            var result = db.Responseais
                .Where(x => x.Userid == userId && x.Questionid == questionid)
                .Select(x => new
                {
                    x.Questionuser,
                    x.Answerai,
                    x.Time

                })
                .ToList();
            return Ok(result);

        }

        [HttpPost("reportrate")]
        public IActionResult GetreportRate([FromBody] ReportrateDto rpt)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            /* =====================================================
             * FILTER USER TRƯỚC 
             * ===================================================== */
            var usersQuery = db.Users.AsQueryable();

            // DoTest filter
            //if (rpt.dotest.HasValue && rpt.dotest != -1)
            //{
            //    bool doTestValue = rpt.dotest == 1;
            //    usersQuery = usersQuery.Where(u => u.DoTest == doTestValue);
            //}

            // Type of test filter
            if (rpt.typeoftest.HasValue && rpt.typeoftest != -1)
            {
                usersQuery = usersQuery.Where(u => u.Typeoftest == rpt.typeoftest.Value);
            }

            /* =====================================================
             * JOIN USER → ANSWER → QUESTION
             * ===================================================== */
            var query =
                from usr in usersQuery
                join usans in db.Useranswers on usr.Id equals usans.Userid
                join qs in db.Questions on usans.Questionid equals qs.Id
                select new { usr, usans, qs };

            /* =====================================================
             * FILTER THEO ANSWER
             * ===================================================== */

            // UseTime
            if (rpt.useTime.HasValue && rpt.useTime != -1)
            {
                bool useTimeValue = rpt.useTime == 1;
                query = query.Where(x => x.usans.Usetime == useTimeValue);
            }

            if (rpt.fromdate.HasValue)
            {
                DateTime fromDate = rpt.fromdate.Value.Date;
                query = query.Where(x => x.usans.Submittedat >= fromDate);
            }

            if (rpt.todate.HasValue)
            {
                DateTime toDate = rpt.todate.Value.Date.AddDays(1);
                query = query.Where(x => x.usans.Submittedat < toDate);
            }

            /* =====================================================
             *TÍNH ĐIỂM CHO MỖI ANSWER
             * ===================================================== */
            var userAvgScores = query
                .Select(x => new
                {
                    x.usans.Userid,
                    Score =
                        x.usans.Useranswer1 == x.qs.Hallucinationanswer && x.usans.Trytimes >= 1 ? 2 :
                        x.usans.Useranswer1 == x.qs.Correctanswer && x.usans.Trytimes >= 1 ? 5 :
                        x.usans.Useranswer1 == x.qs.Correctanswer && (x.usans.Trytimes == 0 || x.usans.Trytimes == null) ? 1 :
                        x.usans.Useranswer1 != x.qs.Correctanswer
                            && x.usans.Useranswer1 != x.qs.Hallucinationanswer
                            && x.usans.Useranswer1 != "F"
                            && x.usans.Trytimes >= 1 ? 3 :
                        x.usans.Useranswer1 == "F" && x.usans.Trytimes >= 1 ? 4 :
                        0
                })
                .GroupBy(x => x.Userid)
                .Select(g => new
                {
                    Userid = g.Key,
                    AvgScore = g.Average(x => x.Score)
                })
                .ToList();

            /* =====================================================
             * PHÂN LOẠI ĐIỂM
             * ===================================================== */
            var ranges = new List<string>
    {
        "(0-1)", "(1-2)", "(2-3)", "(3-4)", "(4-5)"
    };

            var groupedData = userAvgScores
                .GroupBy(x =>
                    x.AvgScore < 1 ? "(0-1)" :
                    x.AvgScore < 2 ? "(1-2)" :
                    x.AvgScore < 3 ? "(2-3)" :
                    x.AvgScore < 4 ? "(3-4)" :
                                     "(4-5)"
                )
                .Select(g => new
                {
                    Range = g.Key,
                    Count = g.Count()
                })
                .ToList();

            int totalUsers = userAvgScores.Count;

            /* =====================================================
             * MERGE RANGE BỊ THIẾU
             * ===================================================== */
            var result = ranges.Select(range =>
            {
                var item = groupedData.FirstOrDefault(x => x.Range == range);
                int count = item?.Count ?? 0;

                return new ScoreDistributionDto
                {
                    Range = range,
                    Count = count,
                    Percentage = totalUsers == 0
                        ? 0
                        : Math.Round(count * 100.0 / totalUsers, 2)
                };
            }).ToList();

            return Ok(result);
        }

        [HttpPost("getreportsurvey")]
        public IActionResult GetReportSurvey([FromBody] ReportrateDto rpt)
        {
            // 1️⃣ Filter Users
            var usersQuery = db.Users.AsQueryable();

            if (rpt.typeoftest.HasValue && rpt.typeoftest != -1)
            {
                usersQuery = usersQuery
                    .Where(u => u.Typeoftest == rpt.typeoftest.Value);
            }

            // 2️⃣ Join Useranswers
            var query =
                from u in usersQuery
                join ua in db.Useranswers on u.Id equals ua.Userid
                select ua;

            if (rpt.useTime.HasValue && rpt.useTime != -1)
            {
                bool useTimeValue = rpt.useTime == 1;
                query = query.Where(ua => ua.Usetime == useTimeValue);
            }

            // 🔑 LẤY USER ID ĐÃ FILTER
            var filteredUserIds = query
                .Select(x => x.Userid)
                .Distinct();

            // 3️⃣ Load Survey Answers (DÙNG FILTER)
            var rawData = db.Useranswersurveys
                .Where(x => filteredUserIds.Contains(x.Userid))
                .Select(x => new
                {
                    x.Surveyid,
                    x.Survey.Question,
                    Answer = x.Answer ?? 0
                })
                .ToList();

            // 4️⃣ Group & calculate (GIỮ NGUYÊN)
            var result = rawData
                .GroupBy(x => new { x.Surveyid, x.Question })
                .Select(g =>
                {
                    var totalAnswers = g.Count();

                    var avgScore = Math.Round(
                        g.Average(x => (double)x.Answer),
                        2
                    );

                    return new SurveyReportDto
                    {
                        SurveyID = (int)g.Key.Surveyid,
                        Question = g.Key.Question,
                        AvgScore = avgScore,

                        Details = g
                            .GroupBy(x => x.Answer)
                            .OrderBy(sg => sg.Key)
                            .Select(sg => new SurveyScoreDetailDto
                            {
                                Score = sg.Key,
                                Count = sg.Count(),
                                Percentage = Math.Round(
                                    sg.Count() * 100.0 / totalAnswers,
                                    2
                                )
                            })
                            .ToList()
                    };
                })  
                .OrderBy(x => x.SurveyID)
                .ToList();

            return Ok(result);
        }

        [HttpGet("getallusers")]
        public IActionResult GetUsers(int page = 1, int pageSize = 20)
        {
            var query = db.Users.AsQueryable();

            var total = query.Count();

            var users = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    Role = u.Role,
                    Gender = u.Gender,
                    Password = u.Password,
                    Major = u.Major,
                    Gpa = u.Gpa,
                    StudyYear = u.StudyYear,
                    DoTest = u.Dotest

                })
                .ToList();

            return Ok(new
            {
                data = users,
                total,
                page,
                pageSize
            });
        }

        [HttpPut("edituser")]
        public IActionResult EditUser([FromBody] User editUser)
        {
            var user = db.Users.Find(editUser.Id);
            if (user == null)
            {
                return NotFound(new { status = false, message = "User not found" });
            }
            user.Name = editUser.Name;
            user.Email = editUser.Email;
            user.Gender = editUser.Gender;
            user.Gpa = editUser.Gpa;
            user.Password = editUser.Password;
            user.StudyYear = editUser.StudyYear;
            user.Role = editUser.Role;
            db.SaveChanges();
            return Ok(new { status = true, message = "Edit user successfully" });

        }

        [HttpPost("adduser")]
        public IActionResult AddUser([FromBody] User newUser)
        {
            var existingUser = db.Users.FirstOrDefault(u => u.Email == newUser.Email);
            if (existingUser != null)
            {
                return Conflict(new { status = false, message = "Email already exists" });
            }
            db.Users.Add(newUser);
            db.SaveChanges();
            return Ok(new { status = true, message = "Add user successfully", userId = newUser.Id });
        }

        [HttpGet("getsetting")]
        public IActionResult GetSetting()
        {
            var setting = db.Settings.FirstOrDefault();
            if (setting == null)
            {
                return NotFound(new { status = false, message = "Setting not found" });
            }
            return Ok(setting);
        }
        [HttpPost("updatesetting")]
        public IActionResult UpdateSetting([FromBody] Setting updatedSetting)
        {
            var setting = db.Settings.SingleOrDefault(x => x.Id == 1);
            if (setting == null)
            {
                return NotFound(new { status = false, message = "Setting not found" });
            }
            setting.Time = updatedSetting.Time;
            setting.Timelimit = updatedSetting.Timelimit;
            setting.Shufflequestion = updatedSetting.Shufflequestion;
            db.Settings.Update(setting);
            db.SaveChanges();
            return Ok(new { status = true, message = "Update setting successfully" });
        }

        [HttpGet("getallquestions")]
        public IActionResult GetAllQuestions()
        {
            var questions = db.Questions
                .Select(q => new
                {
                    q.Id,
                    q.Question1,
                    q.Correctanswer,
                    q.Hallucinationanswer,
                    q.Timetries
                })
                .ToList();
            return Ok(questions);
        }
        [HttpGet("getquestionbyid/{id}")]
        public IActionResult GetQuestionById(int id)
        {
            var question = db.Questions
                .Where(q => q.Id == id)
                .Select(q => new
                {
                    q.Id,
                    q.Question1,
                    q.Answer,
                    q.Correctanswer,
                    q.Correctanswerdesc,
                    q.Image,
                    q.Hallucination,
                    q.Hallucinationanswer,
                    q.Timetries
                })
                .FirstOrDefault();
            if (question == null)
            {
                return NotFound(new { status = false, message = "Question not found" });
            }
            return Ok(new { status = true, data = question });
        }

        [HttpPost("editquestion")]
        public IActionResult EditQuestion([FromBody] Question editQuestion)
        {
            var question = db.Questions.Find(editQuestion.Id);
            if (question == null)
            {
                return NotFound(new { status = false, message = "Question not found" });
            }
            question.Question1 = editQuestion.Question1;
            question.Answer = editQuestion.Answer;
            question.Correctanswer = editQuestion.Correctanswer;
            question.Correctanswerdesc = editQuestion.Correctanswerdesc;
            question.Image = editQuestion.Image;
            question.Hallucination = editQuestion.Hallucination;
            question.Hallucinationanswer = editQuestion.Hallucinationanswer;
            question.Timetries = editQuestion.Timetries;
            db.SaveChanges();
            return Ok(new { status = true, message = "Edit question successfully" });
        }

        [HttpPost("addnewquestion")]
        public IActionResult AddNewQuestion([FromBody] Question newQuestion)
        {
            db.Questions.Add(newQuestion);
            db.SaveChanges();
            return Ok(new { status = true, message = "Add question successfully" });
        }

        [HttpGet("getsurveybyiduser/{userid}")]
        public IActionResult GetSurveyByIdUser(int userid)
        {
            var surveys = db.Useranswersurveys
                .Where(x => x.Userid == userid)
                .Select(x => new
                {
                    x.Surveyid,
                    x.Survey.Question,
                    x.Answer
                })
                .ToList();
            return Ok(surveys);
        }

        [HttpPost("export-users-excel")]
        public IActionResult ExportUsersExcel([FromBody] ReportrateDto rpt)
        {
            /* =========================
             * SHEET 1: USERS
             * ========================= */

            var usersQuery = db.Users.AsQueryable();

            if (rpt.typeoftest.HasValue && rpt.typeoftest != -1)
            {
                usersQuery = usersQuery
                    .Where(u => u.Typeoftest == rpt.typeoftest.Value);
            }

            var users = usersQuery.ToList();

            using var workbook = new XLWorkbook();
            var sheet1 = workbook.Worksheets.Add("Users");

            string[] headers1 =
            {
                "User ID",
                "Username",
                "Email",
                "Name",
                "Role",
                "Gender",
                "Study Year",
                "Major",
                "Do Test",
                "Year Of Birth",
                "GPA",
                "Type Of Test"
            };

            for (int i = 0; i < headers1.Length; i++)
            {
                sheet1.Cell(1, i + 1).Value = headers1[i];
            }

            var headerRange1 = sheet1.Range(1, 1, 1, headers1.Length);
            headerRange1.Style.Font.Bold = true;
            headerRange1.Style.Fill.BackgroundColor = XLColor.LightBlue;
            headerRange1.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            int row1 = 2;
            foreach (var u in users)
            {
                sheet1.Cell(row1, 1).Value = u.Id;
                sheet1.Cell(row1, 2).Value = u.Username;
                sheet1.Cell(row1, 3).Value = u.Email;
                sheet1.Cell(row1, 4).Value = u.Name;
                sheet1.Cell(row1, 5).Value = u.Role;
                sheet1.Cell(row1, 6).Value = u.Gender;
                sheet1.Cell(row1, 7).Value = u.StudyYear;
                sheet1.Cell(row1, 8).Value = u.Major;
                sheet1.Cell(row1, 9).Value = u.Dotest == true ? "Yes" : "No";
                sheet1.Cell(row1, 10).Value = u.Yearofbirth;
                sheet1.Cell(row1, 11).Value = u.Gpa;
                sheet1.Cell(row1, 12).Value = u.Typeoftest;

                row1++;
            }

            sheet1.Columns().AdjustToContents();
            sheet1.SheetView.FreezeRows(1);

            /* =========================
             * SHEET 2: TOTAL SCORES
             * ========================= */

            var scoreData = GetUserScoreData(rpt);
            var sheet2 = workbook.Worksheets.Add("Total Scores");

            string[] headers2 =
            {
                "User ID",
                "Name",
                "Total Score",
                "Average Score",
                "Started At",
                "Submitted At",
                "Duration (minutes)",
                "Type Of Test"
            };

            for (int i = 0; i < headers2.Length; i++)
            {
                sheet2.Cell(1, i + 1).Value = headers2[i];
            }

            var headerRange2 = sheet2.Range(1, 1, 1, headers2.Length);
            headerRange2.Style.Font.Bold = true;
            headerRange2.Style.Fill.BackgroundColor = XLColor.LightSalmon;
            headerRange2.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            int row2 = 2;
            foreach (var s in scoreData)
            {
                sheet2.Cell(row2, 1).Value = s.UserID;
                sheet2.Cell(row2, 2).Value = s.Name;
                sheet2.Cell(row2, 3).Value = s.TotalScore;
                sheet2.Cell(row2, 4).Value = Math.Round(s.AvgScore, 2);
                sheet2.Cell(row2, 5).Value = s.StartedAt;
                sheet2.Cell(row2, 6).Value = s.SubmittedAt;
                sheet2.Cell(row2, 7).Value = s.Duration.HasValue? Math.Round(s.Duration.Value.TotalMinutes, 2): 0;
                sheet2.Cell(row2, 8).Value = s.Typeoftest;

                row2++;
            }

            sheet2.Columns().AdjustToContents();
            sheet2.SheetView.FreezeRows(1);

            /* =========================
            * SHEET 3: USER ANSWERS DETAIL
            * ========================= */

            var answerData = GetUserAnswerDetail(rpt);
            var sheet3 = workbook.Worksheets.Add("User Answers Detail");

            string[] headers3 =
            {
                "Name",
                "Question",
                "User Answer",
                "Correct Answer",
                "Hallucination Answer",
                "Score",
                "Try Times",
                "Started At",
                "Submitted At"
            };

            for (int i = 0; i < headers3.Length; i++)
            {
                sheet3.Cell(1, i + 1).Value = headers3[i];
            }

            var headerRange3 = sheet3.Range(1, 1, 1, headers3.Length);
            headerRange3.Style.Font.Bold = true;
            headerRange3.Style.Fill.BackgroundColor = XLColor.LightGreen;
            headerRange3.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            int row3 = 2;
            foreach (var item in answerData)
            {
                sheet3.Cell(row3, 1).Value = item.Name;
                sheet3.Cell(row3, 2).Value = item.QuestionText;
                sheet3.Cell(row3, 3).Value = item.Useranswer1;
                sheet3.Cell(row3, 4).Value = item.Correctanswer;
                sheet3.Cell(row3, 5).Value = item.Hallucinationanswer;
                sheet3.Cell(row3, 6).Value = item.Score;
                if (item.Score == 2)
                    sheet3.Cell(row3, 6).Style.Fill.BackgroundColor = XLColor.LightPink;

                sheet3.Cell(row3, 7).Value = item.Trytimes;
                sheet3.Cell(row3, 8).Value = item.Startedat != null ? (DateTime)item.Startedat : DateTime.MinValue;

                sheet3.Cell(row3, 9).Value =
                    item.Submittedat != null ? (DateTime)item.Submittedat : DateTime.MinValue;

                sheet3.Cell(row3, 8).Style.DateFormat.Format = "yyyy-MM-dd HH:mm";
                sheet3.Cell(row3, 9).Style.DateFormat.Format = "yyyy-MM-dd HH:mm";

                row3++;
            }

            sheet3.Columns().AdjustToContents();
            sheet3.SheetView.FreezeRows(1);
            /* =========================
            * SHEET 4: USER – AI RESPONSE
            * ========================= */

            var aiData = GetUserandResponseAi(rpt);
            var sheet4 = workbook.Worksheets.Add("AI Responses");

            string[] headers4 =
            {
                "Name",
                "Question ID",
                "User Question",
                "AI Answer",
                "Time"
            };

            // Header
            for (int i = 0; i < headers4.Length; i++)
            {
                sheet4.Cell(1, i + 1).Value = headers4[i];
            }

            var headerRange4 = sheet4.Range(1, 1, 1, headers4.Length);
            headerRange4.Style.Font.Bold = true;
            headerRange4.Style.Fill.BackgroundColor = XLColor.LightCyan;
            headerRange4.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Data
            int row4 = 2;
            foreach (var item in aiData)
            {
                sheet4.Cell(row4, 1).Value = item.Name;
                sheet4.Cell(row4, 2).Value = item.QuestionId;
                sheet4.Cell(row4, 3).Value = item.QuestionUser;
                sheet4.Cell(row4, 4).Value = item.AnswerAi;

                sheet4.Cell(row4, 5).Value =
                    item.Time.HasValue ? item.Time.Value : DateTime.MinValue;

                sheet4.Cell(row4, 5).Style.DateFormat.Format = "yyyy-MM-dd HH:mm";

                row4++;
            }

            sheet4.Columns().AdjustToContents();
            sheet4.SheetView.FreezeRows(1);
            /* =========================
           * SHEET 5: Survey
           * ========================= */

            var surveyData = GetUserAnswerSurveyDetail(rpt);
            var sheetSurvey = workbook.Worksheets.Add("Survey Answers");

            string[] headersSurvey =
            {
    "User ID",
    "Name",
    "Survey ID",
    "Survey Question",
    "Answer",
    "Time"
};

            // Header
            for (int i = 0; i < headersSurvey.Length; i++)
            {
                sheetSurvey.Cell(1, i + 1).Value = headersSurvey[i];
            }

            var headerRangeSurvey = sheetSurvey.Range(1, 1, 1, headersSurvey.Length);
            headerRangeSurvey.Style.Font.Bold = true;
            headerRangeSurvey.Style.Fill.BackgroundColor = XLColor.LightCyan;
            headerRangeSurvey.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Data
            int row = 2;
            foreach (var item in surveyData)
            {
                sheetSurvey.Cell(row, 1).Value = item.UserID;
                sheetSurvey.Cell(row, 2).Value = item.Name;
                sheetSurvey.Cell(row, 3).Value = item.SurveyID;
                sheetSurvey.Cell(row, 4).Value = item.SurveyQuestion;
                sheetSurvey.Cell(row, 5).Value = item.Answer;

               

                row++;
            }

            sheetSurvey.Columns().AdjustToContents();
            sheetSurvey.SheetView.FreezeRows(1);
            /* =========================
             * RETURN FILE
             * ========================= */

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Users_{DateTime.Now:yyyyMMddHHmmss}.xlsx"
            );
        }
        private List<UserScoreDto> GetUserScoreData(ReportrateDto rpt)
        {
            var userfilter = db.Users.AsQueryable();

            if (rpt.typeoftest.HasValue && rpt.typeoftest != -1)
            {
                userfilter = userfilter.Where(u => u.Typeoftest == rpt.typeoftest.Value);
            }

            var Useranswers = db.Useranswers.AsQueryable();

            if (rpt.useTime.HasValue && rpt.useTime != -1)
            {
                bool useTimeValue = rpt.useTime == 1;
                Useranswers = Useranswers.Where(x => x.Usetime == useTimeValue);
            }

            if (rpt.fromdate.HasValue)
            {
                DateTime fromDate = rpt.fromdate.Value.Date;
                Useranswers = Useranswers.Where(x => x.Submittedat >= fromDate);
            }

            if (rpt.todate.HasValue)
            {
                DateTime toDate = rpt.todate.Value.Date.AddDays(1);
                Useranswers = Useranswers.Where(x => x.Submittedat < toDate);
            }

            var joinedQuery =
                from ua in Useranswers
                join u in userfilter on ua.Userid equals u.Id
                select new
                {
                    ua.Userid,
                    u.Name,
                    ua.Submittedat,
                    ua.Startedat,
                    u.Typeoftest,
                    Score =
                        ua.Useranswer1 == ua.Question.Hallucinationanswer && ua.Trytimes >= 1 ? 2 :
                        ua.Useranswer1 == ua.Question.Correctanswer && ua.Trytimes >= 1 ? 5 :
                        ua.Useranswer1 == ua.Question.Correctanswer && ua.Trytimes == 0 ? 1 :
                        ua.Useranswer1 != ua.Question.Correctanswer
                            && ua.Useranswer1 != ua.Question.Hallucinationanswer
                            && ua.Useranswer1 != "F"
                            && ua.Trytimes >= 1 ? 3 :
                        ua.Useranswer1 == "F" && ua.Trytimes >= 1 ? 4 :
                        0
                };

            return joinedQuery
                .GroupBy(x => new { x.Userid, x.Name })
                .Select(g => new UserScoreDto
                {
                    UserID = (int)g.Key.Userid,
                    Name = g.Key.Name,
                    TotalScore = g.Sum(x => x.Score),
                    AvgScore = g.Average(x => x.Score),
                    SubmittedAt = g.Max(x => x.Submittedat),
                    StartedAt = g.Min(x => x.Startedat),
                    Duration = g.Max(x => x.Submittedat) - g.Min(x => x.Startedat),
                    Typeoftest = g.Select(x => x.Typeoftest).FirstOrDefault()
                })
                .OrderByDescending(x => x.TotalScore)
                .ToList();
        }

        private List<dynamic> GetUserAnswerDetail(ReportrateDto rpt)
        {
            var userfilter = db.Users.AsQueryable();

            if (rpt.typeoftest.HasValue && rpt.typeoftest != -1)
            {
                userfilter = userfilter.Where(u => u.Typeoftest == rpt.typeoftest.Value);
            }

            var Useranswers = db.Useranswers.AsQueryable();

            if (rpt.useTime.HasValue && rpt.useTime != -1)
            {
                bool useTimeValue = rpt.useTime == 1;
                Useranswers = Useranswers.Where(x => x.Usetime == useTimeValue);
            }

            if (rpt.fromdate.HasValue)
            {
                DateTime fromDate = rpt.fromdate.Value.Date;
                Useranswers = Useranswers.Where(x => x.Submittedat >= fromDate);
            }

            if (rpt.todate.HasValue)
            {
                DateTime toDate = rpt.todate.Value.Date.AddDays(1);
                Useranswers = Useranswers.Where(x => x.Submittedat < toDate);
            }

            var questions = db.Questions.AsQueryable();

            var data = from ua in Useranswers
                       join u in userfilter on ua.Userid equals u.Id
                       join q in questions on ua.Questionid equals q.Id
                       select new
                       {
                           u.Name,
                           QuestionText = HtmlHelper.StripHtml(q.Question1),
                           ua.Useranswer1,
                           q.Correctanswer,
                           q.Hallucinationanswer,
                           Score =
            ua.Useranswer1 == q.Hallucinationanswer && ua.Trytimes >= 1 ? 2 :
            ua.Useranswer1 == q.Correctanswer && ua.Trytimes >= 1 ? 5 :
            ua.Useranswer1 == q.Correctanswer && ua.Trytimes == 0 ? 1 :
            ua.Useranswer1 != q.Correctanswer
                && ua.Useranswer1 != q.Hallucinationanswer
                && ua.Useranswer1 != "F"
                && ua.Trytimes >= 1 ? 3 :
            ua.Useranswer1 == "F" && ua.Trytimes >= 1 ? 4 :
            0,
                           ua.Trytimes,
                           ua.Startedat,
                           ua.Submittedat
                       };

            return data.ToList<dynamic>();
        }

        private List<UserAiResponseDto> GetUserandResponseAi(ReportrateDto rpt)
        {
            var userfilter = db.Users.AsQueryable();
            if (rpt.typeoftest.HasValue && rpt.typeoftest != -1)
            {
                userfilter = userfilter.Where(u => u.Typeoftest == rpt.typeoftest.Value);
            }

            var Useranswers = db.Useranswers.AsQueryable();
            if (rpt.useTime.HasValue && rpt.useTime != -1)
            {
                bool useTimeValue = rpt.useTime == 1;
                Useranswers = Useranswers.Where(x => x.Usetime == useTimeValue);
            }
            if (rpt.fromdate.HasValue)
            {
                DateTime fromDate = rpt.fromdate.Value.Date;
                Useranswers = Useranswers.Where(x => x.Submittedat >= fromDate);
            }
            if (rpt.todate.HasValue)
            {
                DateTime toDate = rpt.todate.Value.Date.AddDays(1);
                Useranswers = Useranswers.Where(x => x.Submittedat < toDate);
            }

            var data =
                from ua in Useranswers
                join u in userfilter on ua.Userid equals u.Id
                join ra in db.Responseais
                    on new { ua.Userid, ua.Questionid }
                    equals new { ra.Userid, ra.Questionid }
                select new UserAiResponseDto
                {
                    Name = u.Name,
                    QuestionId = ra.Questionid,
                    QuestionUser = ra.Questionuser,
                    AnswerAi = ra.Answerai,
                    Time = ra.Time
                };

            return data.ToList();
        }
        private List<dynamic> GetUserAnswerSurveyDetail(ReportrateDto rpt)
        {
            var userfilter = db.Users.AsQueryable();

            if (rpt.typeoftest.HasValue && rpt.typeoftest != -1)
            {
                userfilter = userfilter.Where(u => u.Typeoftest == rpt.typeoftest.Value);
            }

            var userAnswerSurvey = db.Useranswersurveys.AsQueryable(); // <-- bảng Useranswersurvey

            

            var surveys = db.Surveys.AsQueryable();

            var data =
                from uas in userAnswerSurvey
                join u in userfilter on uas.Userid equals u.Id
                join s in surveys on uas.Surveyid equals s.Id
                select new
                {
                    UserID = u.Id,
                    Name = u.Name,
                    SurveyID = s.Id,
                    SurveyQuestion = HtmlHelper.StripHtml(s.Question), // nếu field là Survey.Question
                    Answer = uas.Answer, // hoặc uas.Answer1 tuỳ DB bạn
                    
                };

            return data.ToList<dynamic>();
        }



    }
}

