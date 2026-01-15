using AI_trust.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AI_trust.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SettingController : ControllerBase
    {
        private readonly AiTrustContext db;
        public SettingController(AiTrustContext db)
        {
            this.db = db;
        }
        [HttpGet("getsetting")]
        public async Task<IActionResult> GetSetting()
        {
            var setting = db.Settings.FirstOrDefault();
            return Ok(setting);
        }
    }
}
