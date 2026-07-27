namespace UserManagementApi.Models.AuthModels
{
    public class AccessTokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public double ExpiresIn { get; set; }
    }
}