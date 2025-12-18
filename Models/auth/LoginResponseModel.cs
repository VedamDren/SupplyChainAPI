namespace SupplyChainAPI.Models.Auth
{
    public class LoginResponseModel
    {
        public int Status { get; set; }
        public string Token { get; set; }
        public string Login { get; set; }
        public int UserId { get; set; }
        public string Message { get; set; }
    }
}