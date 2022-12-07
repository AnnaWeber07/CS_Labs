using AnnaWebweJWTandHashingPassword.Domain.DTO;
using AnnaWebweJWTandHashingPassword.Domain.Models;
using AnnaWebweJWTandHashingPassword.Services.TokenRefreshService;
using AnnaWebweJWTandHashingPassword.Services.UserService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;

namespace AnnaWebweJWTandHashingPassword.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        public static User user = new User();
        public static GetMessageDTO message = new GetMessageDTO();
        private readonly IConfiguration _configuration;
        private readonly IUserService _userService;

        public AuthController(IConfiguration configuration, IUserService userService)
        {
            _configuration = configuration;
            _userService = userService;
        }



        [HttpPost("register")]
        public async Task<ActionResult<User>> Register(UserDto request)
        {
            CreatePasswordHash(request.Password, out byte[] passwordHash, out byte[] passwordSalt);

            user.Username = request.Username;
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;

            return Ok("You are registered");
        }
        [HttpPost("login")]
        public async Task<ActionResult<string>> Login(UserDto request)
        {
            if (user.Username != request.Username)
            {
                return BadRequest("User not found.");
            }

            if (!VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt))
            {
                return BadRequest("Wrong password.");
            }

            string token = CreateToken(user);

            var refreshToken = GenerateRefreshToken();
            CreateUpdateToken(refreshToken);
            user.RefreshToken = token;

            return Ok(token);
        }

        [HttpPost("getinput")]
        public async Task<ActionResult<User>> Getinput(WriteMessageDTO request)
        {
            if (user.RefreshToken != request.token)
            {
                return BadRequest("Token not found.");
            }
            if (user.Username != request.Username)
            {
                return BadRequest("User not found.");
            }
            CreateMessageHash(request.Message, out byte[] messageHash, out byte[] messageSalt);

            user.MessageHash = messageHash;
            user.MessageSalt = messageSalt;
            user.Message = request.Message;

            return Ok(user);
        }
        [HttpPost("checkmessage")]
        public async Task<ActionResult<User>> CheckMessage(GetMessageDTO request)
        {
            if (user.RefreshToken != request.token)
            {
                return BadRequest("Token not found.");
            }
            if (user.Username != request.Username)
            {
                return BadRequest("User not found.");
            }
            if (!VerifyMessageHash(request.Message, user.MessageHash, user.MessageSalt))
            {
                return BadRequest("Wrong Message");
            }
            return Ok(user);

        }
        [HttpPost("refresh-token")]
        public async Task<ActionResult<string>> UpdateToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];

            if (!user.RefreshToken.Equals(refreshToken))
            {
                return Unauthorized("Invalid Refresh Token.");
            }
            else if (user.TokenExpires < DateTime.Now)
            {
                return Unauthorized("Token expired.");
            }

            string token = CreateToken(user);
            var newRefreshToken = GenerateRefreshToken();
            CreateUpdateToken(newRefreshToken);

            return Ok(token);
        }



        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        private void CreateMessageHash(string message, out byte[] messageHash, out byte[] messageSalt)
        {
            using (var hmac = new HMACSHA512())
            {
                messageSalt = hmac.Key;
                messageHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(message));
            }
        }
        private bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512(passwordSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return computedHash.SequenceEqual(passwordHash);
            }
        }
        private bool VerifyMessageHash(string message, byte[] messageHash, byte[] messageSalt)
        {

            using (var hmac = new HMACSHA512(messageSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(message));
                return computedHash.SequenceEqual(messageHash);
            }
        }

        private string CreateToken(User user)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(
                _configuration.GetSection("AppSettings:Token").Value));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds);

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;
        }
        private void CreateUpdateToken(UpdateToken newUpdateToken)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = newUpdateToken.Expires
            };
            Response.Cookies.Append("refreshToken", newUpdateToken.Token, cookieOptions);

            user.RefreshToken = newUpdateToken.Token;
            user.TokenCreated = newUpdateToken.Created;
            user.TokenExpires = newUpdateToken.Expires;
        }
        private UpdateToken GenerateRefreshToken()
        {
            var refreshToken = new UpdateToken
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                Expires = DateTime.Now.AddDays(7),
                Created = DateTime.Now
            };

            return refreshToken;
        }
    }
}
