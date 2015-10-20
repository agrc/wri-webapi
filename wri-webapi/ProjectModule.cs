using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Transactions;
using Microsoft.SqlServer.Types;
using Nancy;
using Nancy.ModelBinding;
using Newtonsoft.Json;
using wri_shared.Models.Response;
using wri_webapi.Configuration;
using wri_webapi.Extensions;
using wri_webapi.Lookup;
using wri_webapi.MediaTypes;
using wri_webapi.Models.Database;
using wri_webapi.Models.Request;
using wri_webapi.Models.Response;
using wri_webapi.Properties;

namespace wri_webapi
{
    public class ProjectModule : NancyModule
    {
        public ProjectModule(IQuery queries, IAttributeValidator validator)
        {
            Get["/project/{id:int}", true] = async (_, ctx) =>
            {
                var id = int.Parse(_.id);
                var model = this.Bind<UserDetailsRequest>();
                var response = new ProjectWithFeaturesResponse();

                var incompleteAttributes = string.IsNullOrEmpty(model.Token) || string.IsNullOrEmpty(model.Key);
                response.AllowEdits = !incompleteAttributes;

                using (var connection = queries.OpenConnection())
                {
                    var projects = await queries.ProjectQueryAsync(connection, new {id});
                    response.Project = projects.FirstOrDefault();

                    var features = await queries.FeatureQueryAsync(connection, new {id});
                    response.Features = features;
                    var reason = "";
                    if (incompleteAttributes)
                    {
                        return Negotiate.WithModel(response)
                            .WithStatusCode(HttpStatusCode.OK)
                            .WithHeader("reason", "incomplete attributes");
                    }

                    var users = await queries.UserQueryAsync(connection, new {key = model.Key, token = model.Token});
                    var user = users.FirstOrDefault();

                    // make sure user is valid
                    if (user == null)
                    {
                        response.AllowEdits = false;
                        reason = "User is null. ";
                    }

                    // anonymous and public users cannot create features
                    else if (new[] {"GROUP_ANONYMOUS", "GROUP_PUBLIC"}.Contains(user.Role))
                    {
                        response.AllowEdits = false;
                        reason += "Roles is anonymous or public. ";
                    }

                    // if project has features no or null, block feature creation
                    else if (response.Project.Features == "No" && user.Role != "GROUP_ADMIN")
                    {
                        response.AllowEdits = false;
                        reason += "Project is designated to have no features. ";
                    }

                    // cancelled and completed projects cannot be edited
                    else if (new[] {"Cancelled", "Completed"}.Contains(response.Project.Status) &&
                             user.Role != "GROUP_ADMIN")
                    {
                        response.AllowEdits = false;
                    }

                    // check if a user is a contributor
                    else if (response.Project.ProjectManagerId != user.Id && user.Role != "GROUP_ADMIN")
                    {
                        var counts = await queries.ContributorQueryAsync(connection, new {id, userId = user.Id});
                        var count = counts.FirstOrDefault();

                        if (count == 0)
                        {
                            response.AllowEdits = false;
                            reason += "User is not a contributor. ";
                        }
                    }

                    return Negotiate.WithModel(response)
                            .WithHeader("reason", reason);
                }
            };

            Get["/project/{id:int}/feature/{featureId:int}", true] = async (_, ctx) =>
            {
                var featureId = int.Parse(_.featureId);
                var featureCategory = (string)Request.Query.featureCategory;

                // make sure feature type is valid
                if (featureCategory == null || !FeatureCategoryToTable.Contains(featureCategory.ToLower()))
                {
                    return Negotiate.WithReasonPhrase("Incomplete request")
                        .WithStatusCode(HttpStatusCode.BadRequest)
                        .WithModel("Category not found.");
                }

                IEnumerable<RelatedDetails> records;

                var connectionString = ConfigurationManager.ConnectionStrings["db"].ConnectionString;
                using (var connection = new SqlConnection(connectionString))
                {
                    try
                    {
                        await connection.OpenAsync();
                    }
                    catch (SqlException)
                    {
                        return Negotiate.WithReasonPhrase("Database Error")
                            .WithStatusCode(HttpStatusCode.InternalServerError)
                            .WithModel("Unable to connect to the database.");
                    }
                    
                    // get the database table to use
                    var table = FeatureCategoryToTable.GetTableFrom(featureCategory);

                    records = await queries.RelatedDataQueryAsync(connection, new
                    {
                        table,
                        featureId
                    });
                }

                var response = new SpatialFeatureResponse
                {
                    County = records.Where(x=>x.Origin == "county"),
                    FocusArea = records.Where(x=>x.Origin == "focus"),
                    SageGrouse = records.Where(x=>x.Origin == "sgma"),
                    LandOwnership = records.Where(x=>x.Origin == "owner")
                };

                return response;
            };

            Post["/project/{id:int}/feature/create", true] = async (_, ctx) =>
            {
                var id = int.Parse(_.id);
                var model = this.Bind<SaveFeatureRequest>();

                // make sure we have all the user information.
                if (string.IsNullOrEmpty(model.Token) || string.IsNullOrEmpty(model.Key))
                {
                    return Negotiate.WithReasonPhrase("User not found")
                        .WithStatusCode(HttpStatusCode.BadRequest)
                        .WithModel("User not found.");
                }

                // make sure we have most of the attributes.
                if (new[] {model.Category, model.Geometry}.Any(string.IsNullOrEmpty))
                {
                    return Negotiate.WithReasonPhrase("Incomplete request")
                        .WithStatusCode(HttpStatusCode.BadRequest)
                        .WithModel("Missing required parameters. Category, geometry, or actions.");
                }

                // make sure feature type is valid
                if (!FeatureCategoryToTable.Contains(model.Category.ToLower()))
                {
                    return Negotiate.WithReasonPhrase("Incomplete request")
                        .WithStatusCode(HttpStatusCode.BadRequest)
                        .WithModel("Category not found.");
                }

                // get the database table to use
                var featureClass = FeatureCategoryToTable.GetTableFrom(model.Category);

                var connectionString = ConfigurationManager.ConnectionStrings["db"].ConnectionString;
                using (var connection = new SqlConnection(connectionString))
                {
                    var projects = await queries.ProjectMinimalQueryAsync(connection, new {id});
                    var project = projects.FirstOrDefault();

                    // make sure project id is valid
                    if (project == null)
                    {
                        return Negotiate.WithReasonPhrase("Project not found")
                            .WithStatusCode(HttpStatusCode.BadRequest)
                            .WithModel("Project not found.");
                    }

                    var users = await queries.UserQueryAsync(connection, new {key = model.Key, token = model.Token});
                    var user = users.FirstOrDefault();

                    // make sure user is valid
                    if (user == null)
                    {
                        return Negotiate.WithReasonPhrase("User not found")
                            .WithStatusCode(HttpStatusCode.BadRequest)
                            .WithModel("User not found.");
                    }

                    // cancelled and completed projects cannot be edited
                    if (new[] {"Cancelled", "Completed"}.Contains(project.Status) && user.Role != "GROUP_ADMIN")
                    {
                        return Negotiate.WithReasonPhrase("Project Status")
                            .WithStatusCode(HttpStatusCode.PreconditionFailed)
                            .WithModel("A cancelled or completed project cannot be modified.");
                    }

                    // anonymous and public users cannot create features
                    if (new[] {"GROUP_ANONYMOUS", "GROUP_PUBLIC"}.Contains(user.Role))
                    {
                        return Negotiate.WithReasonPhrase("Role")
                            .WithStatusCode(HttpStatusCode.Unauthorized)
                            .WithModel("Project manager and contributors are only allowed to modify this project.");
                    }

                    // if project has features no or null, block feature creation
                    if (project.Features == "No" && user.Role != "GROUP_ADMIN")
                    {
                        return Negotiate.WithReasonPhrase("Project settings")
                            .WithStatusCode(HttpStatusCode.PreconditionFailed)
                            .WithModel(
                                "Project is marked to have no features. Therefore, features are not allowed to be created.");
                    }

                    // check if a user is a contributor
                    if (project.ProjectManagerId != user.Id && user.Role != "GROUP_ADMIN")
                    {
                        var counts = await queries.ContributorQueryAsync(connection, new {id, userId = user.Id});
                        var count = counts.FirstOrDefault();

                        if (count == 0)
                        {
                            return Negotiate.WithReasonPhrase("Not contributor")
                                .WithStatusCode(HttpStatusCode.Unauthorized)
                                .WithModel(
                                    "You are not the project owner or a contributer. Therefore, you are not allowed to modify this project.");
                        }
                    }

                    SqlGeometry geometry;
                    try
                    {
                        geometry = SqlGeometry.Parse(model.Geometry);
                        geometry.STSrid = 3857;
                        geometry = geometry.MakeValid();
                    }
                    catch (Exception ex)
                    {
                        return Negotiate.WithReasonPhrase("Invalid geometry")
                            .WithStatusCode(HttpStatusCode.BadRequest)
                            .WithModel(ex.Message);
                    }

                    // check if polygons overlap
                    if (featureClass == "POLY")
                    {
                        var counts = await queries.OverlapQueryAsync(connection, new
                        {
                            id,
                            wkt = model.Geometry,
                            category = model.Category
                        });
                        var count = counts.FirstOrDefault();

                        if (count.HasValue && count.Value > 0)
                        {
                            return Negotiate.WithReasonPhrase("Overlapping geometry")
                                .WithStatusCode(HttpStatusCode.BadRequest)
                                .WithModel(
                                    "Overlapping features of the same type are not allowed. Add more actions to the existing feature.");
                        }
                    }

                    var criteria = new Dictionary<string, string[]>
                    {
                        {"0", new[] {"region"}}, // wri focus areas
                        {"4", new[] {"owner", "admin"}}, // land ownership
                        {"5", new[] {"area_name"}}, // sage grouse
                        {"14", new[] {"name"}} // county
                    };

                    // include stream miles because it's aquatic
                    if (model.Category.ToLower() == "aquatic/riparian treatment area")
                    {
                        criteria["15"] = new[] {"fccode_text"}; // nhd
                    }

                    // send geometry to soe for calculations
                    var httpClient = new HttpClient();
                    var url = string.Format(
                        "http://{0}/Reference/MapServer/exts/wri_soe/ExtractIntersections",
                        Settings.Default.gisServerBaseUrl); 
                    
                    var uri = new Uri(url);
                    var base64Geometry = Convert.ToBase64String(geometry.STAsBinary().Value); 
                    var request = await httpClient.PostAsync(uri,
                        new FormUrlEncodedContent(new[]
                        {
                            new KeyValuePair<string, string>("geometry", base64Geometry),
                            new KeyValuePair<string, string>("criteria", JsonConvert.SerializeObject(criteria)),
                            new KeyValuePair<string, string>("f", "json")
                        }));

                    var soeResponse = await request.Content.ReadAsAsync<ResponseContainer<IntersectResponse>>(
                        new[]
                        {
                            new TextPlainResponseFormatter()
                        });

                    // handle error from soe
                    if (!soeResponse.IsSuccessful)
                    {
                        return Negotiate.WithReasonPhrase(soeResponse.Error.Message)
                            .WithStatusCode(HttpStatusCode.InternalServerError)
                            .WithModel(soeResponse.Error.Message);
                    }

                    var attributes = soeResponse.Result.Attributes;
                    int? primaryKey = null;
                    // create transaction for new feature
                    using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                    {
                        FeatureActions[] actions;
                        try
                        {
                            actions = JsonConvert.DeserializeObject<FeatureActions[]>(model.Actions);
                        }
                        catch (Exception ex)
                        {
                            return Negotiate.WithReasonPhrase("Feature Actions")
                                         .WithStatusCode(HttpStatusCode.InternalServerError)
                                         .WithModel("There was a problem deserializing the feature actions. " + ex.Message);
                        }

                        if (!validator.ValidAttributesFor(featureClass, model.Category, actions))
                        {
                            return Negotiate.WithReasonPhrase("Feature Actions")
                                          .WithStatusCode(HttpStatusCode.InternalServerError)
                                          .WithModel("The actions are not valid for the feature type."); 
                        }

                        switch (featureClass.ToLower())
                        {
                            case "poly":
                                var primaryKeys = await queries.FeatureClassQueryAsync(connection, featureClass, new
                                {
                                    featureType = model.Category,
                                    retreatment = model.Retreatment,
                                    shape = geometry,
                                    id
                                });

                                primaryKey = primaryKeys.FirstOrDefault();

                                if (!primaryKey.HasValue)
                                {
                                    return Negotiate.WithReasonPhrase("Database")
                                        .WithStatusCode(HttpStatusCode.InternalServerError)
                                        .WithModel("Problem getting primary key from poly insert.");
                                }

                                if (actions == null)
                                {
                                    break;
                                }

                                foreach (var polyAction in actions)
                                {
                                    // insert top level action
                                    var actionIds = await queries.ActionQueryAsync(connection, new
                                    {
                                        id = primaryKey,
                                        action = polyAction.Action
                                    });

                                    var actionId = actionIds.FirstOrDefault();

                                    if (!actionId.HasValue)
                                    {
                                        return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError)
                                            .WithReasonPhrase("Database")
                                            .WithModel("Problem getting primary key from action insert.");
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
                                            return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError)
                                                .WithReasonPhrase("Database")
                                                .WithModel("Problem getting primary key from treatment insert.");
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

                                break;
                            case "point":
                            case "line":
                                var action = actions.FirstOrDefault();
                                if (action == null)
                                {
                                    return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError)
                                        .WithReasonPhrase("Action")
                                        .WithModel("Could not find action attributes.");
                                }

                                primaryKeys = await queries.FeatureClassQueryAsync(connection, featureClass, new
                                {
                                    featureType = model.Category,
                                    subType = action.Type,
                                    action = action.Action,
                                    description = action.Description,
                                    id,
                                    shape = geometry
                                });
                                primaryKey = primaryKeys.FirstOrDefault();

                                if (!primaryKey.HasValue)
                                {
                                    return Negotiate.WithReasonPhrase("Database")
                                        .WithStatusCode(HttpStatusCode.InternalServerError)
                                        .WithModel("Problem getting primary key from point or line.");
                                }

                                break;
                        }

                        if (attributes.ContainsKey("watershedRestoration_FocusAreas"))
                        {
                            var data = attributes["watershedRestoration_FocusAreas"].SelectMany(x => x.Attributes,
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
                            var data = attributes["counties"].SelectMany(x => x.Attributes, (original, value) => new
                            {
                                intersect = original.Intersect,
                                county = value,
                                featureClass,
                                id = primaryKey
                            });

                            await queries.ExecuteAsync(connection, "counties", data);
                        }

                        await queries.ExecuteAsync(connection, "ProjectSpatial", new
                        {
                            id
                        });

                        transaction.Complete();
                    }

                    switch (featureClass.ToLower())
                    {
                        case "poly":
                            return
                                Negotiate.WithModel(string.Format("Successfully created a new {0} covering {1}.",
                                    model.Category,
                                    geometry.STArea().Value.ToString(CultureInfo.CurrentCulture).InAcres()))
                                    .WithHeader("FeatureId", primaryKey.ToString());
                        case "line":
                            return
                                Negotiate.WithModel(string.Format("Successfully created a new {0} stretching {1}.",
                                    model.Category,
                                    geometry.STLength().Value.ToString(CultureInfo.CurrentCulture).InAcres()))
                                    .WithHeader("FeatureId", primaryKey.ToString());
                    }

                    return Negotiate.WithModel(string.Format("Successfully created a new {0}.", model.Category))
                        .WithHeader("FeatureId", primaryKey.ToString());
                }
            };
        }
    }
}