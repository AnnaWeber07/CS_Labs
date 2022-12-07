using AnnaWebweJWTandHashingPassword.Domain.Models;

namespace AnnaWebweJWTandHashingPassword.Domain.DTO
{
    public class GetMessageDTO
    {
        public string token { get; set; } = string.Empty;
        public string Username { get; set; }
        public string Message { get; set; }
        
    }
}
