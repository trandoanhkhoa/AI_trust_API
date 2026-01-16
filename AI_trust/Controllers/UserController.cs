using AI_trust.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AI_trust.DTOs;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;


namespace AI_trust.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly AiTrustContext _db;
        

        public UserController(AiTrustContext db)
        {
            _db = db;
           
        }   
        [HttpPost("checklogin")]
        public async Task<IActionResult> CheckLogin([FromBody] AccountUser acc)
        {
            var user = await _db.Users.SingleOrDefaultAsync(u =>
                (u.Username == acc.username || u.Email == acc.username) &&
                u.Password == acc.password
            );

            if (user == null)
                return Ok(new { status = false, message="Tài khoản hoặc mật khẩu không đúng" });
            return Ok(new { status = true, token = user.Name, userid = user.Id, role = user.Role, doTest = user.Dotest,typeOfTest =user.Typeoftest });
        }

        [HttpPost("submittest")]
        public async Task<IActionResult> SubmitTest([FromBody] SubmitTestRequest request)
        {
            if (request == null || request.Answers == null || !request.Answers.Any())
                return BadRequest("Request không hợp lệ");

            var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == request.UserId);
            if (user == null)
                return BadRequest("User không tồn tại");

            foreach (var a in request.Answers)
            {
                var usans = new Useranswer
                {
                    Userid = request.UserId,
                    Questionid = a.QuestionId,
                    Useranswer1 = a.UserAnswer ?? "",
                    Trytimes = a.TryTimes,
                    Usetime = a.useTime,
                    Startedat = DateTime.SpecifyKind(
                        a.StartedAt == DateTime.MinValue ? DateTime.Now : a.StartedAt,
                        DateTimeKind.Unspecified
                    ),

                    Submittedat = DateTime.SpecifyKind(
                        a.SubmittedAt == DateTime.MinValue ? DateTime.Now : a.SubmittedAt,
                        DateTimeKind.Unspecified
                    )

                };
                _db.Useranswers.Add(usans);
            }

            user.Dotest = true;
            await _db.SaveChangesAsync();

            return Ok(new { status = true });
        }


        [HttpPost("getuser/{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _db.Users.SingleOrDefaultAsync(x => x.Id == id);
            if (user == null)
                return NotFound("User not found");
            return Ok(new
            {
                user.Id,
                user.Name,
                user.Email,
                user.Major,
                user.Gender,
                user.Yearofbirth,
                user.Gpa,
                user.StudyYear,
                user.Typeoftest,
                user.Dotest

            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            try
            {
                var existingUser = await _db.Users.AnyAsync(
                    u => u.Email == req.Email
                );

                if (existingUser)
                    return Ok(new { status = false, message = "Email này đã đăng ký" });

                Random rnd = new Random();
                int rdnumber = rnd.Next(1000, 9999);

                var newUser = new User
                {
                    Name = req.Name,
                    Email = req.Email,
                    Username = req.Email,
                    Gender = req.Gender,
                    Major = req.Major,
                    StudyYear = req.StudyYear,
                    Gpa = req.Gpa,
                    Yearofbirth = req.Yearofbirth,
                    Role = "user",
                    Dotest = false,
                    Typeoftest = rnd.Next(0, 2),
                    Password = $"user{req.Yearofbirth}{rdnumber}"
                };

                bool checkExist = SendAccountEmail(
                    newUser.Email,
                    newUser.Name,
                    newUser.Username,
                    newUser.Password
                );
                _db.Users.Add(newUser);
                await _db.SaveChangesAsync();

                return Ok(new { status = true, userid = newUser.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }


        public static bool SendAccountEmail(string toEmail,string name,string username,string password)
        {
            try
            {
                // ===== SMTP CONFIG =====
                using SmtpClient smtpClient = new SmtpClient("smtp.gmail.com", 587)
                {
                    Credentials = new NetworkCredential(
                        "trankhoa192837@gmail.com",
                        "gcns uizw cldd wvgs"   // App Password
                    ),
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    EnableSsl = true
                };

                // ===== MAIL =====
                using MailMessage mail = new MailMessage
                {
                    From = new MailAddress(
                        "trankhoa192837@gmail.com",
                        "CRTest"
                    ),
                    Subject = "🎉 Thông tin tài khoản khảo sát – Critical reasoning test",
                    IsBodyHtml = true,
                    Body = BuildHtmlEmail(name, username, password)
                };

                mail.To.Add(new MailAddress(toEmail));

                smtpClient.Send(mail); // ❗ Nếu lỗi → throw exception

                return true; // ✅ Gửi thành công
            }
            catch (Exception ex)
            {
                // 👉 Log lỗi (rất nên làm)
                //Console.WriteLine("Send email failed: " + ex.Message);

                return false; // ❌ Gửi thất bại
            }
        }

        private static string BuildHtmlEmail(string name,string username,string password)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='UTF-8'>
</head>
<body style='font-family:Arial; background:#f4f6f8; padding:20px'>
  <div style='max-width:600px; margin:auto; background:#ffffff;
              padding:24px; border-radius:12px'>

    <h2 style='color:#2563eb'>🎉 Đăng ký khảo sát thành công</h2>

    <p>Thân gửi <strong>{name}</strong>,</p>

    <p>
      Cảm ơn bạn đã đăng ký tham gia khảo sát trong dự án
      <strong>CRTest</strong>.
    </p>

    <p>Thông tin tài khoản của bạn:</p>

    <div style='background:#f1f5f9; padding:16px; border-radius:8px'>
      <p><strong>Tài khoản:</strong> {username}</p>
      <p><strong>Mật khẩu:</strong> {password}</p>
    </div>

    <p style='margin-top:16px'>
      👉 Nhấn vào nút bên dưới để đăng nhập và bắt đầu khảo sát:
        Vui lòng sử dụng tài khoản và mật khẩu đã cung cấp để đăng nhập vào hệ thống khảo sát.
    </p>
     <div style=""
    margin-top:20px;
    padding:12px;
    background-color:#fff3cd;
    border-left:5px solid #f59e0b;
    color:#92400e;
    font-weight:600;
"">
    ⚠️ <b>Chú ý:</b><br/>
    Vui lòng <u>không cung cấp</u> thông tin tài khoản này cho bất kỳ ai khác!
</div>
    <a href='https://cr-test-ai.vercel.app/login'
       style='display:inline-block; margin-top:12px;
              padding:12px 20px; background:#2563eb;
              color:#ffffff; text-decoration:none;
              border-radius:8px'>
      Đăng nhập hệ thống
    </a>

    <p style='margin-top:20px'>
      Mọi thông tin bạn cung cấp sẽ được bảo mật và chỉ phục vụ
      cho mục đích nghiên cứu.
    </p>

    <hr style='margin:24px 0'/>

    <p style='font-size:13px; color:#64748b'>
      AI Trust Platform<br/>
      Trân trọng cảm ơn sự đóng góp của bạn 💙
    </p>
  </div>
</body>
</html>";
        }

        [HttpPut("edituser")]
        public IActionResult EditUser([FromBody] User editUser)
        {
            var user = _db.Users.Find(editUser.Id);
            if (user == null)
            {
                return NotFound(new { status = false, message = "User not found" });
            }
            user.Name = editUser.Name;
            user.Email = editUser.Email;
            user.Gender = editUser.Gender;
            user.Gpa = editUser.Gpa;
            user.StudyYear = editUser.StudyYear;
            user.Major = editUser.Major;
            user.Yearofbirth = editUser.Yearofbirth;
            _db.SaveChanges();
            return Ok(new { status = true, message = "Edit user successfully" });

        }


    }


}
