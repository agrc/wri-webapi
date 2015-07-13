using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using Nancy;
using Nancy.Responses;
using Nancy.Responses.Negotiation;
using wri_webapi.Models.Database;
using wri_webapi.Models.Response;

namespace wri_webapi
{
    public class ProjectModule : NancyModule
    {
        public ProjectModule()
        {
            Get["/project/{id:int}", true] = async (_, ctx) =>
            {
                const string projectQuery = "select top 1 p.Project_ID as projectId," +
                                            "p.ProjectManager_ID as projectManagerId," +
                                            "p.ProjectManagerName as projectManagerName," +
                                            "p.LeadAgencyOrg as leadAgency," +
                                            "p.title as title," +
                                            "p.Status as status," +
                                            "p.Description as description," +
                                            "p.ProjRegion as region " +
                                            "from PROJECT p where p.Project_ID = @id";

                const string spatialQuery = "select 'point' as origin," +
                                            "pt.FeatureID as featureId," +
                                            "pt.TypeDescription as type," +
                                            "pt.FeatureSubTypeDescription,pt.ActionDescription,null as size " +
                                            "from POINT pt where pt.Project_ID = @id " +
                                            "union select 'line' as origin," +
                                            "l.FeatureID as id," +
                                            "l.TypeDescription as type," +
                                            "l.FeatureSubTypeDescription,l.ActionDescription," +
                                            "cast(round(l.Shape.STLength() * 3.28084,2) as varchar) + ' ft' as size " +
                                            "from LINE l where l.Project_ID = @id " +
                                            "union select 'poly' as origin," +
                                            "p.FeatureID as featureId," +
                                            "p.TypeDescription as Type," +
                                            "a.ActionDescription as SubType," +
                                            "t.TreatmentTypeDescription as Action," +
                                            "cast(round(p.Shape.STArea() * 0.0015625,2) as varchar) + ' mi²' as size " +
                                            "from POLY p " +
                                            "left outer join dbo.AreaACTION a on p.FeatureID = a.FeatureId " +
                                            "left outer join dbo.AreaTreatment t on a.AreaActionId = t.AreaActionId " +
                                            "where Project_ID = @id";

                var id = int.Parse(_.id);

                var response = new ProjectWithFeaturesResponse();

                var connectionString = ConfigurationManager.ConnectionStrings["db"].ConnectionString;
                using (var connection = new SqlConnection(connectionString))
                {
                    var projects = await connection.QueryAsync<Project>(projectQuery, new {id});
                    response.Project = projects.FirstOrDefault();

                    var pointLineFeatures = await connection.QueryAsync<SpatialFeature>(spatialQuery, new {id});
                    response.Features = pointLineFeatures;

                    return Negotiate.WithModel(response)
                        .WithMediaRangeModel(new MediaRange("text/html"),
                            new JsonResponse(response, new DefaultJsonSerializer()));
                }
            };
        }
    }
}