using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.SqlServer.Types;
using Newtonsoft.Json;
using wri_shared.Models.Response;
using wri_webapi.Extensions;
using wri_webapi.MediaTypes;
using wri_webapi.Properties;

namespace wri_webapi.Services
{
    public interface IIntersectionService
    {
        Task<ResponseContainer<IntersectResponse>> QuerySoeAsync(SqlGeometry geometry, string category);
    }

    public class IntersectionService : IIntersectionService
    {
       Dictionary<string, string[]> _criteria = new Dictionary<string, string[]>
                    {
                        {"0", new[] {"region"}}, // wri focus areas
                        {"4", new[] {"owner", "admin"}}, // land ownership
                        {"5", new[] {"area_name"}}, // sage grouse
                        {"14", new[] {"name"}} // county
                    };

        public async Task<ResponseContainer<IntersectResponse>> QuerySoeAsync(SqlGeometry geometry, string category)
        {
            category = category.ToLower();
            // include stream miles because it's aquatic
            if (category == "aquatic/riparian treatment area")
            {
                _criteria["15"] = new[] { "fcode_text" }; // nhd
            }

            // send geometry to soe for calculations
            var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMilliseconds(-1.0)
            };
            var url = string.Format(
                "http://{0}/Reference/MapServer/exts/wri_soe/ExtractIntersections",
                Settings.Default.gisServerBaseUrl);

            var uri = new Uri(url);
            var base64Geometry = Convert.ToBase64String(geometry.STAsBinary().Value);
            var formContent = new[]
                    {
                        new KeyValuePair<string, string>("geometry", base64Geometry),
                        new KeyValuePair<string, string>("criteria",
                            JsonConvert.SerializeObject(_criteria)),
                        new KeyValuePair<string, string>("f", "json")
                    }.AsFormContent();

            var request = await httpClient.PostAsync(uri, formContent);

            return await request.Content.ReadAsAsync<ResponseContainer<IntersectResponse>>(
                new[]
                        {
                            new TextPlainResponseFormatter()
                        });
        }
    }
}