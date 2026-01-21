using PharmaDistributionApp.Models;

namespace PharmaDistributionApp.Services
{
    public static class UserSession
    {
        public static Employee CurrentUser { get; set; }
        public static bool IsLoggedIn { get; set; } = false;
        public static void Clear()
        {
            CurrentUser = null;
            IsLoggedIn = false;
        }
    }
}