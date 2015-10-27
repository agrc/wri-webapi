using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;

namespace wri_webapi.Actions
{
    public static class Delete
    {
        public static async Task<bool> SpatialFeature(IDbConnection connection, int featureId, string category, string table)
        {
            await connection.ExecuteAsync(string.Format("DELETE FROM [dbo].[{0}] ", table) +
                                          "WHERE [FeatureID] = @featureId AND " +
                                          "LOWER([TypeDescription]) = @category", new
                                          {
                                              featureId,
                                              category
                                          });

            return await Task.Factory.StartNew(()=>true);
        }

        public static async Task<bool> Actions(IDbConnection connection, int featureId)
        {
            var actions = await connection.QueryAsync<int>("SELECT [AreaActionId] " +
                                                           "FROM [dbo].[AREAACTION] WHERE " +
                                                           "[FeatureID] = @featureId", new
                                                           {
                                                               featureId
                                                           });
            actions = actions.ToList();

            if (!actions.Any())
            {
                return await Task.Factory.StartNew(()=>true);
            }

            var treatments = await connection.QueryAsync<int>("SELECT [AreaTreatmentID] " +
                                                              "FROM [dbo].[AREATREATMENT] WHERE " +
                                                              "[AreaActionID] IN @actions", new
                                                              {
                                                                  actions
                                                              });

            treatments = treatments.ToList();

            if (!treatments.Any())
            {
                return await Task.Factory.StartNew(() => true);
            }

            await connection.ExecuteAsync("DELETE FROM [dbo].[AREAHERBICIDE] WHERE " +
                                          "[AreaTreatmentID] IN @treatments", new
                                          {
                                              treatments
                                          });

            await connection.ExecuteAsync("DELETE FROM [dbo].[AREATREATMENT] WHERE " +
                                          "[AreaTreatmentID] IN @treatments", new
                                          {
                                              treatments
                                          });

            await connection.ExecuteAsync("DELETE FROM [dbo].[AREAACTION] WHERE " +
                                          "[AreaActionID] IN @actions", new
                                          {
                                              actions
                                          });

            return await Task.Factory.StartNew(() => true);
        }

        public static async Task<bool> ExtractedGis(IDbConnection connection, int featureId, string table)
        {
            table = table.ToUpper();

            foreach (var relatedTable in new[] {"FOCUSAREA", "COUNTY", "LANDOWNER", "SGMA"})
            {
                await connection.ExecuteAsync(string.Format("DELETE FROM [dbo].[{0}] ", relatedTable) +
                                              "WHERE [FeatureID] = @featureId AND " +
                                              "[FeatureClass] = @table", new
                                              {
                                                  featureId,
                                                  table
                                              });
            }

            if (table == "POLY")
            {
                await connection.ExecuteAsync("DELETE FROM [dbo].[STREAM] " +
                                              "WHERE [FeatureID] = @featureId", new
                                              {
                                                  featureId
                                              });
            }

            return await Task.Factory.StartNew(() => true);
        }
    }
}