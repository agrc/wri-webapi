using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using wri_webapi.Configuration;
using wri_webapi.Models.Request;
using wri_webapi.Models.Response;

namespace wri_webapi.Modules
{
    public class ProjectModule : NancyModule
    {
        public ProjectModule(IQuery queries) : base("/project")
        {
            Get["/{id:int}", true] = async (_, ctx) =>
            {
                var id = int.Parse(_.id);
                var model = this.Bind<UserDetailsRequest>();
                var response = new ProjectWithFeaturesResponse();

                var incompleteAttributes = string.IsNullOrEmpty(model.Token) || string.IsNullOrEmpty(model.Key);
                response.AllowEdits = !incompleteAttributes;

                var db = await queries.OpenConnection();
                using (var connection = db.Connection)
                {
                    if (!db.Open)
                    {
                        return Negotiate.WithReasonPhrase("Database Error")
                            .WithStatusCode(HttpStatusCode.InternalServerError)
                            .WithModel("Unable to connect to the database.");
                    }

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

                    else if (response.Project == null) 
                    {
                        response.AllowEdits = false;
                        reason = "Project not found. ";
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
        }
    }
}