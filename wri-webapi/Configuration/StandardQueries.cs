using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using wri_webapi.Models.Database;

namespace wri_webapi.Configuration
{
    public class StandardQueries : IQuery
    {
        private readonly Dictionary<string, string> _sql = new Dictionary<string, string>
        {
            {
                "POINT", "INSERT INTO [WRI].[dbo].[POINT] " +
                         "([TypeDescription], [FeatureSubTypeDescription], [ActionDescription], [Description], [Shape], " +
                         "[Project_ID], [TypeCode], [FeatureSubTypeID], [ActionID], [StatusDescription], [StatusCode]) " +
                         "VALUES (@featureType, @subType, @action, @description, @shape, @id, " +
                         "(SELECT [FeatureTypeID] FROM [WRI].[dbo].[LU_FEATURETYPE] WHERE [FeatureTypeDescription] = @featureType)," +
                         "(SELECT [FeatureSubTypeID] FROM [WRI].[dbo].[LU_FEATURESUBTYPE] WHERE [FeatureSubTypeDescription] = @subType)," +
                         "(SELECT [ActionID] FROM [WRI].[dbo].[LU_ACTION] WHERE [ActionDescription] = @action)," +
                         "(SELECT [Status] FROM [WRI].[dbo].[PROJECT] WHERE [Project_ID] = @id)," +
                         "(SELECT [StatusID] FROM [WRI].[dbo].[PROJECT] WHERE [Project_ID] = @id)); " +
                         "SELECT CAST(SCOPE_IDENTITY() as int)"
            },
            {
                "POLY", "INSERT INTO [WRI].[dbo].[POLY] " +
                        "(TypeDescription, Retreatment, Project_ID, Shape, " +
                        "TypeCode, StatusDescription, StatusCode, AreaAcres) " +
                        "VALUES (@featureType, @retreatment, @id, @shape, " +
                        "(SELECT [FeatureTypeID] FROM [WRI].[dbo].[LU_FEATURETYPE] WHERE [FeatureTypeDescription] = @featureType)," +
                        "(SELECT [Status] FROM [WRI].[dbo].[PROJECT] WHERE [Project_ID] = @id), " +
                        "(SELECT [StatusID] FROM [WRI].[dbo].[PROJECT] WHERE [Project_ID] = @id), " +
                        "@shape.STArea());" +
                        "SELECT CAST(SCOPE_IDENTITY() as int)"
            },
            {
                "Action", "INSERT INTO [WRI].[dbo].[AREAACTION] " +
                          "([FeatureID],[ActionDescription],[ActionID]) " +
                          "VALUES (@id, @action, (SELECT [ActionID] FROM [WRI].[dbo].[LU_ACTION] WHERE [ActionDescription] = @action));" +
                          "SELECT CAST(SCOPE_IDENTITY() as int)"
            },
            {
                "Treatment", "INSERT INTO [WRI].[dbo].[AREATREATMENT] " +
                             "([AreaActionID], [TreatmentTypeDescription], [TreatmentTypeID]) " +
                             "VALUES (@id, @treatment, " +
                             "(SELECT [TreatmentTypeID] FROM [WRI].[dbo].[LU_TREATMENTTYPE] WHERE [TreatmentTypeDescription] = @treatment));" +
                             "SELECT CAST(SCOPE_IDENTITY() as int)"
            },
            {
                "Herbicide", "INSERT INTO [WRI].[dbo].[AREAHERBICIDE] " +
                             "([AreaTreatmentID],[HerbicideDescription],[HerbicideID]) " +
                             "VALUES(@id, @herbicide, " +
                             "(SELECT [HerbicideID] FROM [WRI].[dbo].[LU_HERBICIDE] WHERE [HerbicideDescription] = @herbicide))"
            },
            {
                "LINE", "INSERT INTO [WRI].[dbo].[LINE] " +
                        "([TypeDescription], [FeatureSubTypeDescription], [ActionDescription], [Description], [Shape], " +
                        "[Project_ID], [TypeCode], [FeatureSubTypeID], [ActionID], [StatusDescription], [StatusCode], [LengthFeet]) " +
                        "VALUES (@featureType, @subType, @action, @description, @shape, @id, " +
                        "(SELECT [FeatureTypeID] FROM [WRI].[dbo].[LU_FEATURETYPE] WHERE [FeatureTypeDescription] = @featureType)," +
                        "(SELECT [FeatureSubTypeID] FROM [WRI].[dbo].[LU_FEATURESUBTYPE] WHERE [FeatureSubTypeDescription] = @subType)," +
                        "(SELECT [ActionID] FROM [WRI].[dbo].[LU_ACTION] WHERE [ActionDescription] = @action)," +
                        "(SELECT [Status] FROM [WRI].[dbo].[PROJECT] WHERE [Project_ID] = @id)," +
                        "(SELECT [StatusID] FROM [WRI].[dbo].[PROJECT] WHERE [Project_ID] = @id)," +
                        "@shape.STLength()); " +
                        "SELECT CAST(SCOPE_IDENTITY() as int)"
            },
            {
                "Project", "SELECT TOP 1 p.Project_ID as projectId," +
                           "p.ProjectManager_ID as projectManagerId," +
                           "p.ProjectManagerName as projectManagerName," +
                           "p.LeadAgencyOrg as leadAgency," +
                           "p.title as title," +
                           "p.Status as status," +
                           "p.Description as description," +
                           "p.ProjRegion as region, " +
                           "p.AffectedArea as affectedArea, " +
                           "p.TerrestrialAcres as terrestrialAcres, " +
                           "p.AqRipAcres as aquaticAcres, " +
                           "p.EasementAcquisitionAcres as easementAcres, " +
                           "p.StreamMiles as streamMiles " +
                           "FROM PROJECT p WHERE p.Project_ID = @id"
            },
            {
                "ProjectMinimal",
                "SELECT TOP 1 Project_ID projectid, ProjectManager_ID ProjectManagerId, Status, Features " +
                "FROM [WRI].[dbo].[PROJECT] WHERE Project_ID = @id"
            },
            {
                "ProjectSpatial", "UPDATE [WRI].[dbo].[PROJECT] " +
                                  "SET [TerrestrialAcres] = (SELECT SUM(poly.Shape.STArea()) FROM [WRI].[dbo].[POLY] poly where poly.[Project_ID] = @id AND poly.TypeDescription = @terrestrial), " +
                                  "[AqRipAcres] = (SELECT SUM(poly.Shape.STArea()) FROM [WRI].[dbo].[POLY] poly where poly.[Project_ID] = @id AND LOWER(poly.TypeDescription) = @aquatic), " +
                                  "[StreamMiles] = @stream, " +
                                  "[AffectedArea] = (SELECT SUM(poly.Shape.STArea()) FROM [WRI].[dbo].[POLY] poly where poly.[Project_ID] = @id AND LOWER(poly.TypeDescription) = @affected), " +
                                  "[EasementAcquisitionAcres] = (SELECT SUM(poly.Shape.STArea()) FROM [WRI].[dbo].[POLY] poly where poly.[Project_ID] = @id AND LOWER(poly.TypeDescription) = @easement), " +
                                  "[Centroid] = (SELECT geometry::ConvexHullAggregate(polygons.shape).STCentroid() FROM " +
                                  "(SELECT geometry::ConvexHullAggregate(poly.Shape) AS shape FROM [wri].[dbo].[POLY] poly WHERE poly.Project_ID = @id UNION ALL " +
                                  "SELECT geometry::ConvexHullAggregate(line.Shape) FROM [wri].[dbo].[LINE] line WHERE line.Project_ID = @id UNION ALL " +
                                  "SELECT geometry::ConvexHullAggregate(point.Shape) FROM [wri].[dbo].[POINT] point WHERE point.Project_ID = @id) polygons) " +
                                  "WHERE project.[Project_ID] = @id"
            },
            {
                "Features", "SELECT 'point' as origin," +
                            "pt.FeatureID as featureId," +
                            "pt.TypeDescription as type," +
                            "pt.FeatureSubTypeDescription as subtype," +
                            "pt.ActionDescription as action," +
                            "pt.description," +
                            "null as size " +
                            "FROM POINT pt WHERE pt.Project_ID = @id " +
                            "UNION SELECT 'line' as origin," +
                            "l.FeatureID as id," +
                            "l.TypeDescription as type," +
                            "l.FeatureSubTypeDescription as subtype," +
                            "l.ActionDescription as action," +
                            "null as description," +
                            "l.Shape.STLength() as size " +
                            "FROM line L WHERE l.Project_ID = @id " +
                            "UNION SELECT 'poly' as origin," +
                            "p.FeatureID as featureId," +
                            "p.TypeDescription as Type," +
                            "a.ActionDescription as SubType," +
                            "t.TreatmentTypeDescription as Action," +
                            "null as description," +
                            "p.Shape.STArea() as size " +
                            "FROM POLY p " +
                            "LEFT OUTER JOIN dbo.AreaACTION a ON p.FeatureID = a.FeatureId " +
                            "LEFT OUTER JOIN dbo.AreaTreatment t ON a.AreaActionId = t.AreaActionId " +
                            "WHERE Project_ID = @id"
            },
            {
                "User", "SELECT TOP 1 FirstName + ' ' + LastName Name, user_group Role, user_id id " +
                        "FROM [WRI].[dbo].[USERS] WHERE userKey = @key AND token = @token AND Active = 'YES'"
            },
            {
                "Contributor", "SELECT COUNT(c.User_FK) contributor " +
                               "FROM [WRI].[dbo].[CONTRIBUTOR] c " +
                               "WHERE c.Project_FK = @id AND c.User_FK = @userId"
            },
            {
                "Overlap", "SELECT SUM(CONVERT(INT, " +
                           "p.Shape.STIntersects(geometry::STGeomFromText(@wkt, 3857)))) " +
                           "FROM [WRI].[dbo].[POLY] p " +
                           "WHERE p.TypeDescription = @category AND p.Project_Id = @id"
            },
            {
                "landOwnership", "INSERT INTO [WRI].[dbo].[LANDOWNER] " +
                                 "(FeatureID, FeatureClass, Owner, Admin, [Intersect]) " +
                                 "VALUES (@id, @featureClass, @owner, @admin, @intersect)"
            },
            {
                "watershedRestoration_FocusAreas", "INSERT INTO [WRI].[dbo].[FOCUSAREA] " +
                                                   "(FeatureID, FeatureClass, Region, [Intersect]) " +
                                                   "VALUES (@id, @featureClass, @region, @intersect)"
            },
            {
                "sageGrouseManagementAreas", "INSERT INTO [WRI].[dbo].[SGMA] " +
                                             "(FeatureID, FeatureClass, SGMA, [Intersect]) " +
                                             "VALUES (@id, @featureClass, @sgma, @intersect)"
            },
            {
                "counties", "INSERT INTO [WRI].[dbo].[COUNTY] " +
                            "(FeatureID, FeatureClass, County, [Intersect], County_ID) " +
                            "VALUES (@id, @featureClass, @county, @intersect, " +
                            "(SELECT Code from [WRI].[dbo].[LU_COUNTY] WHERE LOWER(Value) = LOWER(@county)))"
            }
        };

        public SqlConnection OpenConnection()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["db"].ConnectionString;

            return new SqlConnection(connectionString);
        }

        public async Task<IEnumerable<Project>> ProjectQueryAsync(IDbConnection connection, object param = null)
        {
            return await connection.QueryAsync<Project>(_sql["Project"], param);
        }

        public async Task<IEnumerable<Project>> ProjectMinimalQueryAsync(IDbConnection connection, object param = null)
        {
            return await connection.QueryAsync<Project>(_sql["ProjectMinimal"], param);
        }

        public async Task<IEnumerable<SpatialFeature>> FeatureQueryAsync(IDbConnection connection, object param = null)
        {
            return await connection.QueryAsync<SpatialFeature>(_sql["Features"], param);
        }

        public async Task<IEnumerable<User>> UserQueryAsync(IDbConnection connection, object param = null)
        {
            return await connection.QueryAsync<User>(_sql["User"], param);
        }

        public async Task<IEnumerable<int>> ContributorQueryAsync(IDbConnection connection, object param = null)
        {
            return await connection.QueryAsync<int>(_sql["Contributor"], param);
        }

        public async Task<IEnumerable<int?>> OverlapQueryAsync(IDbConnection connection, object param = null)
        {
            return await connection.QueryAsync<int?>(_sql["Overlap"], param);
        }

        public async Task<IEnumerable<int?>> FeatureClassQueryAsync(IDbConnection connection, string featureClass,
            object param = null)
        {
            return await connection.QueryAsync<int?>(_sql[featureClass], param);
        }

        public async Task<IEnumerable<int?>> ActionQueryAsync(IDbConnection connection, object param = null)
        {
            return await connection.QueryAsync<int?>(_sql["Action"], param);
        }

        public async Task<IEnumerable<int?>> TreatmentQueryAsync(IDbConnection connection, object param = null)
        {
            return await connection.QueryAsync<int?>(_sql["Treatment"], param);
        }

        public async Task<int?> ExecuteAsync(IDbConnection connection, string type, object param = null)
        {
            return await connection.ExecuteAsync(_sql[type], param);
        }
    }
}