using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.SqlServer.Types;
using wri_webapi.Configuration;
using wri_webapi.Models.DTO;
using wri_webapi.Models.Request;

namespace wri_webapi.Actions
{
    public static class Update
    {
        public static async Task<bool> ProjectStats(IDbConnection connection, IQuery queries, int id)
        {
            await queries.ExecuteAsync(connection, "ProjectSpatial", new
            {
                id,
                terrestrial = "terrestrial treatment area",
                aquatic = "aquatic/riparian treatment area",
                affected = "affected area",
                easement = "easement/acquisition"
            });

            return await Task.Factory.StartNew(() => true);
        }

        public static async Task<MessageWithStatus> SpatialRow(IDbConnection connection, IQuery queries, int id,
            FeatureActions[] actions, char retreatment, SqlGeometry geometry, string table, double size)
        {
            if (table == "POLY")
            {
                await connection.ExecuteAsync("UPDATE [dbo].[POLY]" +
                                              "SET [Shape] = @shape," +
                                              "[AreaSqMeters] = @size," +
                                              "[Retreatment] = @retreatment " +
                                              "WHERE [FeatureID] = @featureId", new
                                              {
                                                  shape = geometry,
                                                  retreatment,
                                                  featureId = id,
                                                  size
                                              });

                return await Task.Factory.StartNew(() => new MessageWithStatus
                {
                    Successful = true
                });
            }

            var action = actions.FirstOrDefault();
            if (action == null)
            {
                return await Task.Factory.StartNew(() => new MessageWithStatus()
                {
                    Successful = false,
                    Message = "Count not find action attributes."
                });
            }

            if (table == "LINE")
            {
                await connection.ExecuteAsync(string.Format("UPDATE [dbo].[{0}]", table) +
                                              "SET [FeatureSubTypeDescription] = @subtype," +
                                              "[FeatureSubTypeID] = (SELECT [FeatureSubTypeID] FROM [dbo].[LU_FEATURESUBTYPE] WHERE [FeatureSubTypeDescription] = @subType)," +
                                              "[ActionDescription] = @action," +
                                              "[ActionID] = (SELECT [ActionID] FROM [dbo].[LU_ACTION] WHERE [ActionDescription] = @action)," +
                                              "[Description] = @description," +
                                              "[LengthLnMeters] = @size," +
                                              "[Shape] = @shape " +
                                              "WHERE [FeatureID] = @featureId", new
                                              {
                                                  subType = action.Type,
                                                  action = action.Action,
                                                  shape = geometry,
                                                  size,
                                                  description = action.Description,
                                                  featureId = id
                                              });
            }
            else
            {
                await connection.ExecuteAsync(string.Format("UPDATE [dbo].[{0}]", table) +
                                               "SET [FeatureSubTypeDescription] = @subtype," +
                                               "[FeatureSubTypeID] = (SELECT [FeatureSubTypeID] FROM [dbo].[LU_FEATURESUBTYPE] WHERE [FeatureSubTypeDescription] = @subType)," +
                                               "[ActionDescription] = @action," +
                                               "[ActionID] = (SELECT [ActionID] FROM [dbo].[LU_ACTION] WHERE [ActionDescription] = @action)," +
                                               "[Description] = @description," +
                                               "[Shape] = @shape " +
                                               "WHERE [FeatureID] = @featureId", new
                                               {
                                                   subType = action.Type,
                                                   action = action.Action,
                                                   shape = geometry,
                                                   description = action.Description,
                                                   featureId = id
                                               }); 
            }

            return await Task.Factory.StartNew(() => new MessageWithStatus()
            {
                Successful = true
            });
        }
    }
}