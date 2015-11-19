using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Transactions;
using Microsoft.SqlServer.Types;
using Nancy;
using Nancy.ModelBinding;
using Newtonsoft.Json;
using wri_webapi.Actions;
using wri_webapi.Configuration;
using wri_webapi.Extensions;
using wri_webapi.Lookup;
using wri_webapi.Models.Database;
using wri_webapi.Models.Request;
using wri_webapi.Models.Response;
using wri_webapi.Services;

namespace wri_webapi.Modules
{
    public class FeatureModule : NancyModule
    {
        public FeatureModule(IQuery queries, IAttributeValidator validator, ISoeService soeService) : base("/project/{id:int}")
        {
            Get["/feature/{featureId:int}", true] = async (_, ctx) =>
            {
                var model = this.Bind<SpecificFeatureRequest>();

                // make sure feature type is valid
                if (model.FeatureCategory == null || !FeatureCategoryToTable.Contains(model.FeatureCategory.ToLower()))
                {
                    return Negotiate.WithReasonPhrase("Incomplete request")
                        .WithStatusCode(HttpStatusCode.BadRequest)
                        .WithModel("Category not found.");
                }

                IEnumerable<RelatedDetails> records;

                var db = await queries.OpenConnection();
                using (var connection = db.Connection)
                {
                    if (!db.Open)
                    {
                        return Negotiate.WithReasonPhrase("Database Error")
                            .WithStatusCode(HttpStatusCode.InternalServerError)
                            .WithModel("Unable to connect to the database.");
                    }

                    // get the database table to use
                    var table = FeatureCategoryToTable.GetTableFrom(model.FeatureCategory);

                    records = await queries.RelatedDataQueryAsync(connection, new
                    {
                        table,
                        model.FeatureId
                    });
                }

                var response = new SpatialFeatureResponse
                {
                    County = records.Where(x => x.Origin == "county"),
                    FocusArea = records.Where(x => x.Origin == "focus"),
                    SageGrouse = records.Where(x => x.Origin == "sgma"),
                    LandOwnership = records.Where(x => x.Origin == "owner"),
                    Nhd = records.Where(x => x.Origin == "nhd")
                };

                return response;
            };

            Post["/feature/create", true] = async (_, ctx) =>
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
                if (!FeatureCategoryToTable.Contains(model.Category))
                {
                    return Negotiate.WithReasonPhrase("Incomplete request")
                        .WithStatusCode(HttpStatusCode.BadRequest)
                        .WithModel("Category not found.");
                }

                // get the database table to use
                var table = FeatureCategoryToTable.GetTableFrom(model.Category);

                var ten = TimeSpan.FromSeconds(600);

                var db = await queries.OpenConnection();
                using (
                    var transaction = new TransactionScope(TransactionScopeOption.RequiresNew, ten,
                        TransactionScopeAsyncFlowOption.Enabled))
                using (var connection = db.Connection)
                {
                    if (!db.Open)
                    {
                        return Negotiate.WithReasonPhrase("Database Error")
                            .WithStatusCode(HttpStatusCode.InternalServerError)
                            .WithModel("Unable to connect to the database.");
                    }

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
                    if (table == "POLY")
                    {
                        var counts = await queries.OverlapQueryAsync(connection, new
                        {
                            id,
                            wkt = geometry,
                            category = model.Category,
                            featureId = -1
                        });

                        var count = counts.FirstOrDefault();

                        if (count.HasValue && count.Value > 0)
                        {
                            return Negotiate.WithReasonPhrase("Overlapping geometry")
                                .WithStatusCode(HttpStatusCode.BadRequest)
                                .WithModel("Overlapping features of the same type are not allowed. " +
                                           "Add more actions to the existing feature.");
                        }
                    }

                    var soeAreaAndLengthResponse = await soeService.QueryAreasAndLengthsAsync(geometry);

                    // handle error from soe
                    if (!soeAreaAndLengthResponse.IsSuccessful)
                    {
                        return Negotiate.WithReasonPhrase(soeAreaAndLengthResponse.Error.Message)
                            .WithStatusCode(HttpStatusCode.InternalServerError)
                            .WithModel(soeAreaAndLengthResponse.Error.Message);
                    }

                    var soeIntersectResponse = await soeService.QueryIntersectionsAsync(geometry, model.Category);
                    
                    // handle error from soe
                    if (!soeIntersectResponse.IsSuccessful)
                    {
                        return Negotiate.WithReasonPhrase(soeIntersectResponse.Error.Message)
                            .WithStatusCode(HttpStatusCode.InternalServerError)
                            .WithModel(soeIntersectResponse.Error.Message);
                    }

                    int? primaryKey = null;

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

                    if (!validator.ValidAttributesFor(table, model.Category, actions))
                    {
                        return Negotiate.WithReasonPhrase("Feature Actions")
                            .WithStatusCode(HttpStatusCode.InternalServerError)
                            .WithModel("The actions are not valid for the feature type.");
                    }

                    switch (table.ToLower())
                    {
                        case "poly":
                            var primaryKeys = await queries.FeatureClassQueryAsync(connection, table, new
                            {
                                featureType = model.Category,
                                retreatment = model.Retreatment,
                                shape = geometry,
                                size = soeAreaAndLengthResponse.Result.Size,
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

                            var actionsResult = await Create.Actions(connection, queries, primaryKey.Value, actions, table);
                            if (!actionsResult.Successful)
                            {
                                return Negotiate.WithReasonPhrase("Database")
                                    .WithStatusCode(actionsResult.Status)
                                    .WithModel(actionsResult.Message);
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

                            primaryKeys = await queries.FeatureClassQueryAsync(connection, table, new
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

                    await Create.ExtractedGis(connection, queries, id, primaryKey.Value, soeIntersectResponse.Result.Attributes, table);
                    await Update.ProjectStats(connection, queries, id);

                    transaction.Complete();

                    switch (table.ToLower())
                    {
                        case "poly":
                            return
                                Negotiate.WithModel(string.Format("Successfully created a new {0} covering {1}.",
                                    model.Category,
                                    soeAreaAndLengthResponse.Result.Size.InAcres()))
                                    .WithHeader("FeatureId", primaryKey.ToString());
                        case "line":
                            return
                                Negotiate.WithModel(string.Format("Successfully created a new {0} stretching {1}.",
                                    model.Category,
                                    soeAreaAndLengthResponse.Result.Size.InFeet()))
                                    .WithHeader("FeatureId", primaryKey.ToString());
                    }

                    var size = geometry.STNumPoints().Value;

                    return Negotiate.WithModel(string.Format("Successfully created a new {0} in {1} location{2}.", model.Category, size, size > 1 ? "s" : ""))
                        .WithHeader("FeatureId", primaryKey.ToString());
                }
            };

            Put["/feature/{featureId:int}", true] = async (_, ctx) =>
            {
                var model = this.Bind<EditSpecificFeatureRequest>();

                // make sure feature type is valid
                if (model.Category == null || !FeatureCategoryToTable.Contains(model.Category.ToLower()))
                {
                    return Negotiate.WithReasonPhrase("Incomplete request")
                        .WithStatusCode(HttpStatusCode.BadRequest)
                        .WithModel("Category not found.");
                }

                // make sure we have all the user information.
                if (string.IsNullOrEmpty(model.Token) || string.IsNullOrEmpty(model.Key))
                {
                    return Negotiate.WithReasonPhrase("User not found")
                        .WithStatusCode(HttpStatusCode.BadRequest)
                        .WithModel("User not found.");
                }

                // make sure feature type is valid
                if (!FeatureCategoryToTable.Contains(model.Category))
                {
                    return Negotiate.WithReasonPhrase("Incomplete request")
                        .WithStatusCode(HttpStatusCode.BadRequest)
                        .WithModel("Category not found.");
                }

                // get the database table to use
                var table = FeatureCategoryToTable.GetTableFrom(model.Category);

                var ten = TimeSpan.FromSeconds(600);

                var db = await queries.OpenConnection();
                using (
                    var transaction = new TransactionScope(TransactionScopeOption.RequiresNew, ten,
                        TransactionScopeAsyncFlowOption.Enabled))
                using (var connection = db.Connection)
                {
                    if (!db.Open)
                    {
                        return Negotiate.WithReasonPhrase("Database Error")
                            .WithStatusCode(HttpStatusCode.InternalServerError)
                            .WithModel("Unable to connect to the database.");
                    }

                    var projects = await queries.ProjectMinimalQueryAsync(connection, new {model.Id});
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
                        var counts = await queries.ContributorQueryAsync(connection, new {model.Id, userId = user.Id});
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
                    if (table == "POLY")
                    {
                        var counts = await queries.OverlapQueryAsync(connection, new
                        {
                            model.Id,
                            wkt = geometry,
                            category = model.Category,
                            featureId = model.FeatureId
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

                    var soeAreaAndLengthResponse = await soeService.QueryAreasAndLengthsAsync(geometry);

                    // handle error from soe
                    if (!soeAreaAndLengthResponse.IsSuccessful)
                    {
                        return Negotiate.WithReasonPhrase(soeAreaAndLengthResponse.Error.Message)
                            .WithStatusCode(HttpStatusCode.InternalServerError)
                            .WithModel(soeAreaAndLengthResponse.Error.Message);
                    }

                    var soeResponse = await soeService.QueryIntersectionsAsync(geometry, model.Category);

                    // handle error from soe
                    if (!soeResponse.IsSuccessful)
                    {
                        return Negotiate.WithReasonPhrase(soeResponse.Error.Message)
                            .WithStatusCode(HttpStatusCode.InternalServerError)
                            .WithModel(soeResponse.Error.Message);
                    }

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

                    if (!validator.ValidAttributesFor(table, model.Category, actions))
                    {
                        return Negotiate.WithReasonPhrase("Feature Actions")
                            .WithStatusCode(HttpStatusCode.InternalServerError)
                            .WithModel("The actions are not valid for the feature type.");
                    }

                    if (table == "POLY")
                    {
                        await Actions.Delete.Actions(connection, model.FeatureId);
                        var actionsResult = await Create.Actions(connection, queries, model.FeatureId, actions, table);
                        if (!actionsResult.Successful)
                        {
                            return Negotiate.WithReasonPhrase("Database")
                                .WithStatusCode(actionsResult.Status)
                                .WithModel(actionsResult.Message);
                        }
                    }

                    var spatialResult = await Update.SpatialRow(connection, queries, model.FeatureId, actions, model.Retreatment, geometry, table, soeAreaAndLengthResponse.Result.Size);
                    if (!spatialResult.Successful)
                    {
                        return Negotiate.WithReasonPhrase("Database")
                            .WithStatusCode(spatialResult.Status)
                            .WithModel(spatialResult.Message);
                    }

                    await Actions.Delete.ExtractedGis(connection, model.FeatureId, table);
                    await Create.ExtractedGis(connection, queries, model.Id, model.FeatureId, soeResponse.Result.Attributes, table);
                    await Update.ProjectStats(connection, queries, model.Id);

                    transaction.Complete();
                }

                return Negotiate.WithStatusCode(HttpStatusCode.NoContent);
            };

            Delete["/feature/{featureId:int}", true] = async (_, ctx) =>
            {
                var model = this.Bind<SpecificFeatureRequest>();

                // make sure feature type is valid
                if (model.FeatureCategory == null || !FeatureCategoryToTable.Contains(model.FeatureCategory.ToLower()))
                {
                    return Negotiate.WithReasonPhrase("Incomplete request")
                        .WithStatusCode(HttpStatusCode.BadRequest)
                        .WithModel("Category not found.");
                }

                // make sure we have all the user information.
                if (string.IsNullOrEmpty(model.Token) || string.IsNullOrEmpty(model.Key))
                {
                    return Negotiate.WithReasonPhrase("User not found")
                        .WithStatusCode(HttpStatusCode.BadRequest)
                        .WithModel("User not found.");
                }

                // make sure feature type is valid
                if (!FeatureCategoryToTable.Contains(model.FeatureCategory))
                {
                    return Negotiate.WithReasonPhrase("Incomplete request")
                        .WithStatusCode(HttpStatusCode.BadRequest)
                        .WithModel("Category not found.");
                }

                // get the database table to use
                var table = FeatureCategoryToTable.GetTableFrom(model.FeatureCategory);

                var db = await queries.OpenConnection();
                using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                using (var connection = db.Connection)
                {
                    if (!db.Open)
                    {
                        return Negotiate.WithReasonPhrase("Database Error")
                            .WithStatusCode(HttpStatusCode.InternalServerError)
                            .WithModel("Unable to connect to the database.");
                    }

                    var projects = await queries.ProjectMinimalQueryAsync(connection, new {model.Id});
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
                        var counts = await queries.ContributorQueryAsync(connection, new {model.Id, userId = user.Id});
                        var count = counts.FirstOrDefault();

                        if (count == 0)
                        {
                            return Negotiate.WithReasonPhrase("Not contributor")
                                .WithStatusCode(HttpStatusCode.Unauthorized)
                                .WithModel(
                                    "You are not the project owner or a contributer. Therefore, you are not allowed to modify this project.");
                        }
                    }

                    if (table == "POLY")
                    {
                        await Actions.Delete.Actions(connection, model.FeatureId);
                    }

                    await Actions.Delete.ExtractedGis(connection, model.FeatureId, table);
                    await Actions.Delete.SpatialFeature(connection, model.FeatureId, model.FeatureCategory, table);
                    await Update.ProjectStats(connection, queries, model.Id);

                    transaction.Complete();
                }

                return Negotiate.WithStatusCode(HttpStatusCode.Accepted);
            };
        }
    }
}