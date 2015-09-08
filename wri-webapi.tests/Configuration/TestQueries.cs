using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using wri_webapi.Configuration;
using wri_webapi.Models.Database;

namespace wri_webapi.tests.Configuration
{
    public class TestQueries : IQuery
    {
        public SqlConnection OpenConnection()
        {
            return new SqlConnection("");
        }

        public async Task<IEnumerable<Project>> ProjectQueryAsync(IDbConnection connection, object param = null)
        {
            var p = AnonymousObjectToDictionary(param);

            var id = (int) p["id"];

            var project = new Project
            {
                ProjectId = id,
                ProjectManagerId = id
            };

            switch (id)
            {
                case 1:
                    project.Status = "Current";
                    break;
                case 2:
                    project.Status = "Current";
                    project.Features = "No";
                    break;
                case 3:
                    project.Status = "Cancelled";
                    break;
                case 4:
                    project.Status = "Completed";
                    break;
            }

            return await Task.Factory.StartNew(() => new[] {project});
        }

        public Task<IEnumerable<Project>> ProjectMinimalQueryAsync(IDbConnection connection, object param = null)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<SpatialFeature>> FeatureQueryAsync(IDbConnection connection, object param = null)
        {
            return await Task.Factory.StartNew(() => new[] {new SpatialFeature()});
        }

        public async Task<IEnumerable<User>> UserQueryAsync(IDbConnection connection, object param = null)
        {
            var p = AnonymousObjectToDictionary(param);

            if (!p.ContainsKey("key") || !p.ContainsKey("token"))
            {
                return await Task.Factory.StartNew(() => new[] {new User()});
            }

            int id;
            var user = new User();

            if (int.TryParse(p["token"].ToString(), out id))
            {
                user.Id = id;
            }
            else
            {
                user.Id = int.MaxValue;
            }

            if (p["key"].ToString() == "anonymous")
            {
                user.Role = "GROUP_ANONYMOUS";
            } else if (p["key"].ToString() == "public")
            {
                user.Role = "GROUP_PUBLIC";
            }
            else if (p["key"].ToString() == "admin")
            {
                user.Role = "GROUP_ADMIN";
            }
            else if(p["key"].ToString() == "pm")
            {
                user.Role = "GROUP_PM";
            }

            return await Task.Factory.StartNew(() => new[] {user});
        }

        public async Task<IEnumerable<int>> ContributorQueryAsync(IDbConnection connection, object param = null)
        {
            var p = AnonymousObjectToDictionary(param);

            var id = (int)p["id"];
            var userid = (int)p["userId"];
            var result = 0;

            if ((id + userid)%5 == 0)
            {
                result = 1;
            }

            return await Task.Factory.StartNew(() => new[] {result});
        }

        public Task<IEnumerable<int?>> OverlapQueryAsync(IDbConnection connection, object param = null)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<int?>> FeatureClassQueryAsync(IDbConnection connection, string featureClass,
            object param = null)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<int?>> ActionQueryAsync(IDbConnection connection, object param = null)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<int?>> TreatmentQueryAsync(IDbConnection connection, object param = null)
        {
            throw new NotImplementedException();
        }

        public Task<int?> ExecuteAsync(IDbConnection connection, string type, object param = null)
        {
            throw new NotImplementedException();
        }

        private static IDictionary<string, object> AnonymousObjectToDictionary(object propertyBag)
        {
            var result = new Dictionary<string, object>();
            if (propertyBag == null) return result;
            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(propertyBag))
            {
                result.Add(property.Name, property.GetValue(propertyBag));
            }
            return result;
        }
    }
}