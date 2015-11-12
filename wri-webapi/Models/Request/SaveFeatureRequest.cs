namespace wri_webapi.Models.Request
{
    public class SaveFeatureRequest : UserDetailsRequest
    {
        // the feature category, Terrestiral, Aquatic
        public string Category { get; set; }
        // the json geometry from the esri/geometry graphic
        public string Geometry { get; set; }
        // the attributes json encoded  
        public string Actions { get; set; }
        // is the category a retreatment. not an action
        public char Retreatment { get; set; }
    }
}