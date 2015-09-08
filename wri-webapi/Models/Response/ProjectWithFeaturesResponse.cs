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
                // add unique id # to items
                var items = value.OrderBy(x => x.FeatureId).Select((x, iter) => new SpatialFeature(x, iter)
                {
                    HasChildren = false
                }).ToList();

                if (!items.Any(x => new[] {"Terrestrial", "Aquatic"}.Contains(x.Type)))
                {
                    _features = items;
                }

                // If there are aquatic or terrestrial features that can have 1>* relationship
                // format them for use in the grid
                var features = items.GroupBy(x => x.FeatureId);
                foreach (var group in features)
                {
                    var localGroup = group;

                    if (localGroup.Count() < 2)
                    {
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