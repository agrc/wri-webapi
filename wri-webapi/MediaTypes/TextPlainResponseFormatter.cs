using System.Net.Http.Formatting;
using System.Net.Http.Headers;

namespace wri_webapi.MediaTypes
{
    public class TextPlainResponseFormatter : JsonMediaTypeFormatter
    {
        public TextPlainResponseFormatter()
        {
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/plain"));
        }
    }
}