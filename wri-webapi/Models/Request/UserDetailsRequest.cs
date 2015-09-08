namespace wri_webapi.Models.Request
{
    public class UserDetailsRequest
    {
        // the key of the user sending the feature for save
        public string Key { get; set; }
        // the token of the user
        public string Token { get; set; }
    }
}