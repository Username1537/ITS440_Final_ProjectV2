namespace ITS440_Final_ProjectV2.Models
{
    public class AuthenticationUser
    {
        public string SteamId { get; set; }
        public string Username { get; set; }
        public string AvatarUrl { get; set; }
        public DateTime AuthenticationTime { get; set; }
    }
}