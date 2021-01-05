namespace Feature.Auth.Rendering.Models
{
    public class Login
    {
        public string Username { get; set; }

        public string Password { get; set; }

        public string Message { get; set; }

        public bool IsValid { get; set; }
    }
}