namespace AnnaWebweJWTandHashingPassword.Domain.Models
{
    public class User
    {
        public string Username { get; set; } = string.Empty;
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }
        public byte[] AnswerHash { get; set; }
        public byte[] AnswerSalt { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime TokenCreated { get; set; }
        public DateTime TokenExpires { get; set; }
        public string Answer { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
    }
}
