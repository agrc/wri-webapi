using System.Threading.Tasks;
using Microsoft.SqlServer.Types;
using wri_shared.Models.Response;

namespace wri_webapi.Services
{
    public interface ISoeService
    {
        Task<ResponseContainer<IntersectResponse>> QueryIntersectionsAsync(SqlGeometry geometry, string category);
        Task<ResponseContainer<SizeResponse>> QueryAreasAndLengthsAsync(SqlGeometry geometry);
    }
}