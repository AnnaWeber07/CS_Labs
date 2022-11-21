namespace AnnaWebweJWTandHashingPassword.Services.TokenRefreshService
{
    public class UpdateToken
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime Expires { get; set; }
    }
}
