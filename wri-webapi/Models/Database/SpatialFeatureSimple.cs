using Microsoft.SqlServer.Types;

namespace wri_webapi.Models.Database
{
    public class SpatialFeatureSimple
    {
        public string Wkt { get; set; }
        public SqlGeometry Shape { get; set; }
        public int FeatureId { get; set; }
        public string Category { get; set; }
    }
}