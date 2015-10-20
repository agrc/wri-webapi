using System;
using System.Collections.Generic;

namespace wri_webapi.Lookup
{
    public static class FeatureCategoryToTable
    {
        readonly static Dictionary<string, string> Categories = new Dictionary<string, string>
        {
            {"terrestrial treatment area", "POLY"},
            {"aquatic/riparian treatment area", "POLY"},
            {"affected area", "POLY"},
            {"easement/acquisition", "POLY"},
            {"guzzler", "POINT"},
            {"trough", "POINT"},
            {"water control structure", "POINT"},
            {"other point feature", "POINT"},
            {"fish passage structure", "POINT"},
            {"fence", "LINE"},
            {"pipeline", "LINE"},
            {"dam", "LINE"}
        };

        public static string GetTableFrom(string category)
        {
            if (string.IsNullOrEmpty(category))
            {
                throw new ArgumentNullException("category", "category is not found");
            }

            category = category.ToLower();

            if (!Categories.ContainsKey(category))
            {
                throw new KeyNotFoundException(string.Format("{0} not found.", category));
            }

            return Categories[category];
        }

        public static bool Contains(string category)
        {
            if (string.IsNullOrEmpty(category))
            {
                return false;
            }

            category = category.ToLower();

            return Categories.ContainsKey(category);
        }
    }
}