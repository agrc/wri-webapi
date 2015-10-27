using System;
using System.Collections.Generic;
using System.Linq;
using wri_webapi.Models.Database;

namespace wri_webapi.Models.Response
{
    public class ProjectWithFeaturesResponse
    {
        private IEnumerable<SpatialFeature> _features;
        // The details about the project
        public Project Project { get; set; }
        // The spatial features for the project
        public IEnumerable<SpatialFeature> Features
        {
            get { return _features; }
            set
            {
                var items = new List<SpatialFeature>();

                // combine herbicides
                var herbicideDuplicates = value.GroupBy(x => x.FeatureId + " " + x.SubType + " " + x.Action);
                foreach (var group in herbicideDuplicates)
                {
                    if (group.Count() > 1)
                    {
                        var dupe = group.First();
                        dupe.Herbicides = group.Select(x => x.Herbicide).ToArray();
                        dupe.Herbicide = null;
                        items.Add(dupe);

                        continue;
                    }

                    var item = group.First();
                    if (!string.IsNullOrEmpty(item.Herbicide))
                    {
                        item.Herbicides = new[] {item.Herbicide};
                        item.Herbicide = null;
                    }

                    items.Add(item);
                }

                // add unique id # to items
                items = items.OrderBy(x => x.FeatureId).Select((x, iter) => new SpatialFeature(x, iter)
                {
                    HasChildren = false
                }).ToList();

                try
                {
                    if (!items.Any(x => new[]
                    {
                        "terrestrial treatment area",
                        "aquatic/riparian treatment area"
                    }.Contains(x.Type.ToLower())))
                    {
                        _features = items;
                    }
                }
                catch(NullReferenceException)
                { }

                // If there are aquatic or terrestrial features that can have 1>* relationship
                // format them for use in the grid
                var features = items.GroupBy(x => x.FeatureId);
                foreach (var group in features)
                {
                    var localGroup = group;

                    if (localGroup.Count() < 2)
                    {
                        // if there are less than 2 items in a group they don't have children
                        foreach (var item in localGroup)
                        {
                            item.HasChildren = false;
                        }

                        continue;
                    }

                    var parent = localGroup.First();
                    var parentId = items.Max(x => x.Id) + 1;

                    items.Add(new SpatialFeature(parent, parentId, true));

                    foreach (var item in items.Where(x => x.FeatureId == localGroup.Key)
                                              .OrderByDescending(x => x.Id)
                                              .Skip(1))
                    {
                        item.Parent = parentId;
                        item.Type = "";
                        item.HasChildren = false;
                    }
                }

                _features = items;
            }
        }
        // Can the current user edit the project
        public bool AllowEdits { get; set; }
    }
}