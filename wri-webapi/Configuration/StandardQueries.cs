using System;
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
                "POINT", "INSERT INTO [dbo].[POINT] " +
                         "([TypeDescription], [FeatureSubTypeDescription], [ActionDescription], [Description], [Shape], " +
                         "[Project_ID], [TypeCode], [FeatureSubTypeID], [ActionID], [StatusDescription], [StatusCode]) " +
                         "VALUES (@featureType, @subType, @action, @description, @shape, @id, " +
                         "(SELECT [FeatureTypeID] FROM [dbo].[LU_FEATURETYPE] WHERE [FeatureTypeDescription] = @featureType)," +
                         "(SELECT [FeatureSubTypeID] FROM [dbo].[LU_FEATURESUBTYPE] WHERE [FeatureSubTypeDescription] = @subType)," +
                         "(SELECT [ActionID] FROM [dbo].[LU_ACTION] WHERE [ActionDescription] = @action)," +
                         "(SELECT [Status] FROM [dbo].[PROJECT] WHERE [Project_ID] = @id)," +
                         "(SELECT [StatusID] FROM [dbo].[PROJECT] WHERE [Project_ID] = @id)); " +
                         "SELECT CAST(SCOPE_IDENTITY() as int)"
            },
            {
                "POLY", "INSERT INTO [dbo].[POLY] " +
                        "(TypeDescription, Retreatment, Project_ID, Shape, " +
                        "TypeCode, StatusDescription, StatusCode, AreaAcres) " +
                        "VALUES (@featureType, @retreatment, @id, @shape, " +
                        "(SELECT [FeatureTypeID] FROM [dbo].[LU_FEATURETYPE] WHERE [FeatureTypeDescription] = @featureType)," +
                        "(SELECT [Status] FROM [dbo].[PROJECT] WHERE [Project_ID] = @id), " +
                        "(SELECT [StatusID] FROM [dbo].[PROJECT] WHERE [Project_ID] = @id), " +
                        "@shape.STArea());" +
                        "SELECT CAST(SCOPE_IDENTITY() as int)"
            },
            {
                "Action", "INSERT INTO [dbo].[AREAACTION] " +
                          "([FeatureID],[ActionDescription],[ActionID]) " +
                          "VALUES (@id, @action, (SELECT [ActionID] FROM [dbo].[LU_ACTION] WHERE [ActionDescription] = @action));" +
                          "SELECT CAST(SCOPE_IDENTITY() as int)"
            },
            {
                "Treatment", "INSERT INTO [dbo].[AREATREATMENT] " +
                             "([AreaActionID], [TreatmentTypeDescription], [TreatmentTypeID]) " +
                             "VALUES (@id, @treatment, " +
                             "(SELECT [TreatmentTypeID] FROM [dbo].[LU_TREATMENTTYPE] WHERE [TreatmentTypeDescription] = @treatment));" +
                             "SELECT CAST(SCOPE_IDENTITY() as int)"
            },
            {
                "Herbicide", "INSERT INTO [dbo].[AREAHERBICIDE] " +
                             "([AreaTreatmentID],[HerbicideDescription],[HerbicideID]) " +
                             "VALUES(@id, @herbicide, " +
                             "(SELECT [HerbicideID] FROM [dbo].[LU_HERBICIDE] WHERE [HerbicideDescription] = @herbicide))"
            },
            {
                "LINE", "INSERT INTO [dbo].[LINE] " +
                        "([TypeDescription], [FeatureSubTypeDescription], [ActionDescription], [Description], [Shape], " +
                        "[Project_ID], [TypeCode], [FeatureSubTypeID], [ActionID], [StatusDescription], [StatusCode], [LengthFeet]) " +
                        "VALUES (@featureType, @subType, @action, @description, @shape, @id, " +
                        "(SELECT [FeatureTypeID] FROM [dbo].[LU_FEATURETYPE] WHERE [FeatureTypeDescription] = @featureType)," +
                        "(SELECT [FeatureSubTypeID] FROM [dbo].[LU_FEATURESUBTYPE] WHERE [FeatureSubTypeDescription] = @subType)," +
                        "(SELECT [ActionID] FROM [dbo].[LU_ACTION] WHERE [ActionDescription] = @action)," +
                        "(SELECT [Status] FROM [dbo].[PROJECT] WHERE [Project_ID] = @id)," +
                        "(SELECT [StatusID] FROM [dbo].[PROJECT] WHERE [Project_ID] = @id)," +
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
                "FROM [dbo].[PROJECT] WHERE Project_ID = @id"
            },
            {
                "ProjectSpatial", "UPDATE [dbo].[PROJECT] " +
                                  "SET [TerrestrialAcres] = (SELECT SUM(poly.Shape.STArea()) FROM [dbo].[POLY] poly where poly.[Project_ID] = @id AND poly.TypeDescription = @terrestrial), " +
                                  "[AqRipAcres] = (SELECT SUM(poly.Shape.STArea()) FROM [dbo].[POLY] poly where poly.[Project_ID] = @id AND LOWER(poly.TypeDescription) = @aquatic), " +
                                  "[StreamMiles] = (SELECT SUM([Intersect]) FROM [dbo].[STREAM] s WHERE s.[ProjectID] = @id), " +
                                  "[AffectedArea] = (SELECT SUM(poly.Shape.STArea()) FROM [dbo].[POLY] poly where poly.[Project_ID] = @id AND LOWER(poly.TypeDescription) = @affected), " +
                                  "[EasementAcquisitionAcres] = (SELECT SUM(poly.Shape.STArea()) FROM [dbo].[POLY] poly where poly.[Project_ID] = @id AND LOWER(poly.TypeDescription) = @easement), " +
                                  "[Centroid] = (SELECT geometry::ConvexHullAggregate(polygons.shape).STCentroid() FROM " +
                                  "(SELECT geometry::ConvexHullAggregate(poly.Shape) AS shape FROM [dbo].[POLY] poly WHERE poly.Project_ID = @id UNION ALL " +
                                  "SELECT geometry::ConvexHullAggregate(line.Shape) FROM [dbo].[LINE] line WHERE line.Project_ID = @id UNION ALL " +
                                  "SELECT geometry::ConvexHullAggregate(point.Shape) FROM [dbo].[POINT] point WHERE point.Project_ID = @id) polygons) " +
                                  "WHERE project.[Project_ID] = @id"
            },
            {
                "Features", "SELECT 'point' as origin," +
                            "pt.FeatureID as featureId," +
                            "pt.TypeDescription as type," +
                            "pt.FeatureSubTypeDescription as subtype," +
                            "pt.ActionDescription as action," +
                            "pt.description," +
                            "pt.Shape.STNumPoints() as size " +
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
                            "p.TypeDescription as type," +
                            "t.TreatmentTypeDescription as subtype," +
                            "a.ActionDescription as action," +
                            "null as description," +
                            "p.Shape.STArea() as size " +
                            "FROM POLY p " +
                            "LEFT OUTER JOIN dbo.AreaACTION a ON p.FeatureID = a.FeatureId " +
                            "LEFT OUTER JOIN dbo.AreaTreatment t ON a.AreaActionId = t.AreaActionId " +
                            "WHERE Project_ID = @id"
            },
            {
                "RelatedData", "SELECT 'county' as origin, @table as [table], " +
                               "c.County as name, null as extra, c.[Intersect] as [space] " +
                               "FROM COUNTY c " +
                               "WHERE c.FeatureID = @featureId AND c.FeatureClass = @table " +
                               "UNION SELECT 'focus' as origin, @table as [table], " +
                               "f.Region as name, null as extra, f.[Intersect] as [space] " +
                               "FROM FOCUSAREA f " +
                               "WHERE f.FeatureID = @featureId AND f.FeatureClass = @table " +
                               "UNION SELECT 'sgma' as origin, @table as [table], " +
                               "s.SGMA as name, null as extra, s.[Intersect] as [space] " +
                               "FROM SGMA s " +
                               "WHERE s.FeatureID = @featureId AND s.FeatureClass = @table " +
                               "UNION SELECT 'owner' as origin, @table as [table], " +
                               "l.Owner as name, l.Admin as extra, l.[Intersect] as [space] " +
                               "FROM LANDOWNER l " +
                               "WHERE l.FeatureID = @featureId AND l.FeatureClass = @table " +
                               "UNION SELECT 'nhd' as origin, @table as [table], " +
                               "n.StreamDescription as name, null as extra, n.[Intersect] as [space] " +
                               "FROM STREAM n " +
                               "WHERE n.FeatureID = @featureId"
            },
            {
                "User", "SELECT TOP 1 FirstName + ' ' + LastName Name, user_group Role, user_id id " +
                        "FROM [dbo].[USERS] WHERE userKey = @key AND token = @token AND Active = 'YES'"
            },
            {
                "Contributor", "SELECT COUNT(c.User_FK) contributor " +
                               "FROM [dbo].[CONTRIBUTOR] c " +
                               "WHERE c.Project_FK = @id AND c.User_FK = @userId"
            },
            {
                "Overlap", "SELECT SUM(CONVERT(INT, " +
                           "p.Shape.STIntersects(geometry::STGeomFromText(@wkt, 3857)))) " +
                           "FROM [dbo].[POLY] p " +
                           "WHERE p.TypeDescription = @category AND p.Project_Id = @id"
            },
            {
                "Overlap2", "SELECT SUM(CONVERT(INT, " +
                           "p.Shape.STIntersects(@wkt))) " +
                           "FROM [dbo].[POLY] p " +
                           "WHERE p.TypeDescription = @category AND p.Project_Id = @id"
            },
            {
                "landOwnership", "INSERT INTO [dbo].[LANDOWNER] " +
                                 "(FeatureID, FeatureClass, Owner, Admin, [Intersect]) " +
                                 "VALUES (@id, @featureClass, @owner, @admin, @intersect)"
            },
            {
                "watershedRestoration_FocusAreas", "INSERT INTO [dbo].[FOCUSAREA] " +
                                                   "(FeatureID, FeatureClass, Region, [Intersect]) " +
                                                   "VALUES (@id, @featureClass, @region, @intersect)"
            },
            {
                "sageGrouseManagementAreas", "INSERT INTO [dbo].[SGMA] " +
                                             "(FeatureID, FeatureClass, SGMA, [Intersect]) " +
                                             "VALUES (@id, @featureClass, @sgma, @intersect)"
            },
            {
                "counties", "INSERT INTO [dbo].[COUNTY] " +
                            "(FeatureID, FeatureClass, County, [Intersect], County_ID) " +
                            "VALUES (@id, @featureClass, @county, @intersect, " +
                            "(SELECT Code from [dbo].[LU_COUNTY] WHERE LOWER(Value) = LOWER(@county)))"
            },
            {
                "streamsNHDHighRes", "INSERT INTO [dbo].[STREAM] " +
                                     "(ProjectID, FeatureID, StreamDescription, [Intersect]) " +
                                     "VALUES (@id, @featureId, @description, @intersect)"
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
            return await connection.QueryAsync<int?>(_sql["Overlap2"], param);
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
            return await connection.ExecuteAsync(_sql[type], param, null, 240);
        }

        public async Task<IEnumerable<RelatedDetails>> RelatedDataQueryAsync(IDbConnection connection,
            object param = null)
        {
            return await connection.QueryAsync<RelatedDetails>(_sql["RelatedData"], param);
        }
    }
}