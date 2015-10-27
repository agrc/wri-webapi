using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Nancy;
using wri_shared.Models.Response;
using wri_webapi.Configuration;
using wri_webapi.Models.DTO;
using wri_webapi.Models.Request;

namespace wri_webapi.Actions
{
    public static class Create
    {
        public static async Task<MessageWithStatus> Actions(IDbConnection connection, IQuery queries, int id, FeatureActions[] actions, string table)
        {
            if (actions == null)
            {
                return await Task.Factory.StartNew(() => new MessageWithStatus()
                {
                    Successful = true
                });
            }

            table = table.ToUpper();

            if (table != "POLY")
            {
                return await Task.Factory.StartNew(() => new MessageWithStatus()
                {
                    Successful = true
                });
            }

            foreach (var polyAction in actions)
            {
                // insert top level action
                var actionIds = await queries.ActionQueryAsync(connection, new
                {
                    id,
                    action = polyAction.Action
                });

                var actionId = actionIds.FirstOrDefault();

                if (!actionId.HasValue)
                {
                    return await Task.Factory.StartNew(() => new MessageWithStatus()
                    {
                        Successful = false,
                        Status = HttpStatusCode.InternalServerError,
                        Message = "Problem getting scope identity from actions insert."
                    });
                }

                // insert second level treatment
                foreach (var treatment in polyAction.Treatments)
                {
                    var treatmentIds = await queries.TreatmentQueryAsync(connection, new
                    {
                        id = actionId,
                        treatment = treatment.Treatment
                    });

                    var treatmentId = treatmentIds.FirstOrDefault();

                    if (!treatmentId.HasValue)
                    {
                        return await Task.Factory.StartNew(() => new MessageWithStatus()
                        {
                            Successful = false,
                            Status = HttpStatusCode.InternalServerError,
                            Message = "Problem getting scope identity from treatment insert."
                        });
                    }

                    // move on if no herbicides
                    if (treatment.Herbicides == null)
                    {
                        continue;
                    }

                    // insert third level herbicide
                    foreach (var herbicide in treatment.Herbicides)
                    {
                        await queries.ExecuteAsync(connection, "Herbicide", new
                        {
                            id = treatmentId,
                            herbicide
                        });
                    }
                }
            }

            return await Task.Factory.StartNew(() => new MessageWithStatus()
            {
                Successful = true
            });
        }

        public static async Task<bool> ExtractedGis(IDbConnection connection, IQuery queries,
            int id, int primaryKey, IDictionary<string, IList<IntersectAttributes>> attributes, string table)
        {
            // insert related tables
            if (attributes.ContainsKey("watershedRestoration_FocusAreas"))
            {
                var data = attributes["watershedRestoration_FocusAreas"].SelectMany(x => x.Attributes,
                    (original, value) => new
                    {
                        intersect = original.Intersect,
                        region = value,
                        featureClass = table,
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
                    featureClass = table,
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
                        featureClass = table,
                        id = primaryKey
                    });

                await queries.ExecuteAsync(connection, "sageGrouseManagementAreas", data);
            }

            if (attributes.ContainsKey("counties"))
            {
                var data = attributes["counties"].SelectMany(x => x.Attributes, (original, value) => new
                {
                    intersect = original.Intersect,
                    county = value,
                    featureClass = table,
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

            return await Task.Factory.StartNew((() => true));
        }
    }
}