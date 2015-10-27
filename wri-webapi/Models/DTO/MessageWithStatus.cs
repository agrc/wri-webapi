using Nancy;

namespace wri_webapi.Models.DTO
{
    public class MessageWithStatus
    {
        public bool Successful { get; set; }
        public HttpStatusCode Status { get; set; }
        public string Message { get; set; }
    }
}