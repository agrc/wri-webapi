using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using wri_webapi.Models.Database;

namespace wri_webapi.Configuration
{
    public interface IQuery
    {
        SqlConnection OpenConnection();
        Task<IEnumerable<Project>> ProjectQueryAsync(IDbConnection connection, object param = null);
        Task<IEnumerable<Project>> ProjectMinimalQueryAsync(IDbConnection connection, object param = null);
        Task<IEnumerable<SpatialFeature>> FeatureQueryAsync(IDbConnection connection, object param = null);
        Task<IEnumerable<User>> UserQueryAsync(IDbConnection connection, object param = null);
        Task<IEnumerable<int>> ContributorQueryAsync(IDbConnection connection, object param = null);
        Task<IEnumerable<int?>> OverlapQueryAsync(IDbConnection connection, object param = null);
        Task<IEnumerable<int?>> FeatureClassQueryAsync(IDbConnection connection, string featureClass, object param = null); 
        Task<IEnumerable<int?>> ActionQueryAsync(IDbConnection connection, object param = null); 
        Task<IEnumerable<int?>> TreatmentQueryAsync(IDbConnection connection, object param = null); 
        Task<int?> ExecuteAsync(IDbConnection connection, string type, object param = null);
        Task<IEnumerable<RelatedDetails>> RelatedDataQueryAsync(IDbConnection connection, object param = null);
    }
}