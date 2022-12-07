namespace AnnaWebweJWTandHashingPassword.Domain.DTO
{
    public class WriteMessageDTO
    {
        public string token { get; set; } = string.Empty;
        public string Username { get; set; }
        public string Message { get; set; } = String.Empty;
    }
}
