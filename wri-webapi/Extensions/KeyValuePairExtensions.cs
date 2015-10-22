using System;
using System.Collections.Generic;
using System.Net.Http;

namespace wri_webapi.Extensions
{
    public static class KeyValuePairExtensions
    {
        public static HttpContent AsFormContent(
            this IEnumerable<KeyValuePair<string, string>> items)
        {
            HttpContent formContent;
            try
            {
                formContent = new FormUrlEncodedContent(items);
            }
            catch (FormatException)
            {
                var tempContent = new MultipartFormDataContent();

                foreach (var keyValuePair in items)
                {
                    tempContent.Add(new StringContent(keyValuePair.Value), keyValuePair.Key);
                }

                formContent = tempContent;
            }

            return formContent;
        }
    }
}