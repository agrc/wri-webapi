using System;
using System.Linq;
using System.Net.Http;
using System.Transactions;
using Dapper;
using Nancy;
using wri_webapi.Configuration;
using wri_webapi.Models.Database;
using wri_webapi.Services;

namespace wri_webapi.Modules
{
    public class HistoricalModule : NancyModule
    {
        public HistoricalModule(IQuery queries, ISoeService soeService) : base("/historical/project/{id:int}")
        {
            Put["/create-related-data", true] = async (_, ctx) =>
            {
                var id = int.Parse(_.id);

                var ten = TimeSpan.FromSeconds(600);

                var db = await queries.OpenConnection();
                using (var transaction = new TransactionScope(TransactionScopeOption.RequiresNew, ten, TransactionScopeAsyncFlowOption.Enabled))
                using (var connection = db.Connection)
                {
                    if (!db.Open)
                    {
                        return Negotiate.WithReasonPhrase("Database Error")
                            .WithStatusCode(HttpStatusCode.InternalServerError)
                            .WithModel("Unable to connect to the database.");
                    }

                    var projects = await queries.ProjectMinimalQueryAsync(connection, new {id = (int) id});
                    var project = projects.FirstOrDefault();

                    // make sure project id is valid
                    if (project == null)
                    {
                        return Negotiate.WithReasonPhrase("Project not found")
                            .WithStatusCode(HttpStatusCode.BadRequest)
                            .WithModel("Project not found.");
                    }

                    const string geometryQuery =
                        "SELECT [Shape] shape, [FeatureId], [TypeDescription] category FROM {0} WHERE [Project_ID] = @id";


                    foreach (var table in new[] {"POINT", "LINE", "POLY"})
                    {
                        var query = string.Format(geometryQuery, table);
                        var features = await connection.QueryAsync<SpatialFeatureSimple>(query, new
                        {
                            id = (int) id
                        });

                        foreach (var feature in features)
                        {
                            var soeAreaAndLengthResponse = await soeService.QueryAreasAndLengthsAsync(feature.Shape);

                            // handle error from soe
                            if (!soeAreaAndLengthResponse.IsSuccessful)
                            {
                                return Negotiate.WithReasonPhrase(soeAreaAndLengthResponse.Error.Message)
                                    .WithStatusCode(HttpStatusCode.InternalServerError)
                                    .WithModel(soeAreaAndLengthResponse.Error.Message);
                            }

                            var size = soeAreaAndLengthResponse.Result.Size;
                            if (table == "POLY")
                            {
                                await connection.ExecuteAsync("UPDATE [dbo].[POLY]" +
                                                              "SET [AreaSqMeters] = @size " +
                                                              "WHERE [FeatureID] = @featureId", new
                                                              {
                                                                  size,
                                                                  featureId = feature.FeatureId
                                                              });
                            }
                            else if (table == "LINE")
                            {
                                await connection.ExecuteAsync("UPDATE [dbo].[LINE]" +
                                                              "SET [LengthLnMeters] = @size " +
                                                              "WHERE [FeatureID] = @featureId", new
                                                              {
                                                                  size,
                                                                  featureId = feature.FeatureId
                                                              });
                            }

                            var soeResponse = await soeService.QueryIntersectionsAsync(feature.Shape, feature.Category);

                            // handle error from soe
                            if (!soeResponse.IsSuccessful)
                            {
                                return Negotiate.WithReasonPhrase(soeResponse.Error.Message)
                                    .WithStatusCode(HttpStatusCode.InternalServerError)
                                    .WithModel(soeResponse.Error.Message);
                            }

                            var attributes = soeResponse.Result.Attributes;

                            // insert related tables
                            var featureClass = table;
                            var primaryKey = feature.FeatureId;

                            await connection.ExecuteAsync("delete from [wri].[dbo].county WHERE [featureId] = @id;" +
                                                          "delete from [wri].[dbo].FOCUSAREA WHERE [featureId] = @id;" +
                                                          "delete from [wri].[dbo].sgma WHERE [featureId] = @id;" +
                                                          "delete from [wri].[dbo].LANDOWNER WHERE [featureId] = @id",
                                new
                                {
                                    id = primaryKey
                                });

                            if (attributes.ContainsKey("watershedRestoration_FocusAreas"))
                            {
                                var data =
                                    attributes["watershedRestoration_FocusAreas"].SelectMany(x => x.Attributes,
                                        (original, value) => new
                                        {
                                            intersect = original.Intersect,
                                            region = value,
                                            featureClass,
                                            id = primaryKey
                                        });

                                await queries.ExecuteAsync(connection, "watershedRestoration_FocusAreas", data);
                            }

                            if (attributes.ContainsKey("landOwnership"))
                            {
                                var data = attributes["landOwnership"].Select(x => new
                                {
                                    intersect = x.Intersect,
                                    owner = x.Attributes.First(),
                                    admin = x.Attributes.Last(),
                                    featureClass,
                                    id = primaryKey
                                });

                                await queries.ExecuteAsync(connection, "landOwnership", data);
                            }

                            if (attributes.ContainsKey("sageGrouseManagementAreas"))
                            {
                                var data = attributes["sageGrouseManagementAreas"].SelectMany(x => x.Attributes,
                                    (original, value) => new
                                    {
                                        intersect = original.Intersect,
                                        sgma = value,
                                        featureClass,
                                        id = primaryKey
                                    });

                                await queries.ExecuteAsync(connection, "sageGrouseManagementAreas", data);
                            }

                            if (attributes.ContainsKey("counties"))
                            {
                                var data = attributes["counties"].SelectMany(x => x.Attributes,
                                    (original, value) => new
                                    {
                                        intersect = original.Intersect,
                                        county = value,
                                        featureClass,
                                        id = primaryKey
                                    });

                                await queries.ExecuteAsync(connection, "counties", data);
                            }

                            if (attributes.ContainsKey("streamsNHDHighRes"))
                            {
                                var data = attributes["streamsNHDHighRes"].SelectMany(x => x.Attributes,
                                    (original, value) => new
                                    {
                                        id,
                                        featureId = primaryKey,
                                        intersect = original.Intersect,
                                        description = value
                                    });

                                await queries.ExecuteAsync(connection, "streamsNHDHighRes", data);
                            }
                        }
                    }

                    // update project centroids and calculations
                    await queries.ExecuteAsync(connection, "ProjectSpatial", new
                    {
                        id,
                        terrestrial = "terrestrial treatment area",
                        aquatic = "aquatic/riparian treatment area",
                        affected = "affected area",
                        easement = "easement/acquisition"
                    });

                    transaction.Complete();
                }

                return Negotiate.WithStatusCode(HttpStatusCode.NoContent);
            };
        }
    }
}