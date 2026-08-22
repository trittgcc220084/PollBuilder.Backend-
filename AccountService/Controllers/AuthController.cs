using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AccountService.Data;
using AccountService.Models;

namespace AccountService.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AccountDbContext _context;

        public AuthController(AccountDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
                {
                    return BadRequest(new { message = "Email và Mật khẩu không được để trống." });
                }

                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email.Trim());
                if (existingUser != null)
                {
                    return BadRequest(new { message = "Email này đã được đăng ký." });
                }

                var user = new User
                {
                    Email = model.Email.Trim(),
                    PasswordHash = model.Password, // Đang lưu chuỗi thô
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var token = GenerateJwtToken(user);
                return Ok(new { token = token, Token = token, userId = user.Id, email = user.Email });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi Đăng ký: {ex.Message}");
                return StatusCode(500, new { message = "Lỗi hệ thống khi đăng ký.", detail = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
                {
                    return BadRequest(new { message = "Email và Mật khẩu không được để trống." });
                }

                string inputEmail = model.Email.Trim();
                string inputPassword = model.Password;

                // Tìm User theo Email trước để tránh lỗi so sánh null trực tiếp trong câu lệnh EF
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == inputEmail);

                if (user == null || string.IsNullOrEmpty(user.PasswordHash) || user.PasswordHash != inputPassword)
                {
                    return Unauthorized(new { message = "Email hoặc Mật khẩu không chính xác." });
                }

                var token = GenerateJwtToken(user);
                return Ok(new { token = token, Token = token, userId = user.Id, email = user.Email });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi Đăng nhập: {ex.Message}");
                return StatusCode(500, new { message = "Lỗi hệ thống khi đăng nhập.", detail = ex.Message });
            }
        }

        private string GenerateJwtToken(User user)
        {
            string jwtKey = "MotDoanMaBaoMatRatDaiVaKhoDoanChoPollBuilder123!@#";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("sub", user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim("email", user.Email ?? "")
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class RegisterRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
