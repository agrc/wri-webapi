using wri_webapi.Extensions;

namespace wri_webapi.Models.Database
{
    public class SpatialFeature
    {
        private string _size;

        public SpatialFeature(SpatialFeature feature, int id, bool header)
        {
            Id = id;
            FeatureId = feature.FeatureId;
            Type = feature.Type;
            HasChildren = true;
        }

        public SpatialFeature()
        {
            HasChildren = true;
        }

        public SpatialFeature(SpatialFeature feature, int id)
        {
            Id = id;
            FeatureId = feature.FeatureId;
            Type = feature.Type;
            SubType = feature.SubType;
            Action = feature.Action;
            Origin = feature.Origin;
            Parent = feature.Parent;
            HasChildren = true;
            Size = feature.Size;
            Description = feature.Description;
        }

        // unique id for dstore
        public int Id { get; set; }
        // id of the feature in the 
        public int FeatureId { get; set; }
        // the name of the feature Fence, Pipeline
        public string Type { get; set; }
        // the subtype of the feature Barbed wire, pole top
        public string SubType { get; set; }
        // the action of the subtype construction, removal
        public string Action { get; set; }
        // the table the feature came from
        public string Origin { get; set; }
        // if a feature has related subtype and actions,
        // set the parent to the id of the first feature
        // so that nesting can take place on the client table
        public int? Parent { get; set; }
        // In the tree, if the item has children this is true
        public bool HasChildren { get; set; }
        // The area in sq/mi or length in ft of polygons and lines
        public string Size
        {
            get
            {
                if (Origin == "poly")
                {
                    return _size.InAcres();
                }
                
                if (Origin == "line")
                {
                    return _size.InFeet();
                }

                return _size;
            }
            set { _size = value; }
        }

        // The description 
        public string Description { get; set; }

        public override string ToString()
        {
            return
                string.Format(
                    "Id: {0}, FeatureId: {1}, Type: {2}, SubType: {3}, Action: {4}, Origin: {5}, Parent: {6}", Id,
                    FeatureId, Type, SubType, Action, Origin, Parent);
        }
    }
}