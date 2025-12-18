using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace SupplyChainAPI.Configuration
{
    public class AuthOptions
    {
        public const string ISSUER = "SupplyChainAPI";
        public const string AUDIENCE = "SupplyChainClient";
        const string KEY = "mysupersecret_secretkey!1234567890";
        public const int LIFETIME = 60;

        public static SymmetricSecurityKey GetSymmetricSecurityKey()
        {
            return new SymmetricSecurityKey(Encoding.ASCII.GetBytes(KEY));
        }
    }
}