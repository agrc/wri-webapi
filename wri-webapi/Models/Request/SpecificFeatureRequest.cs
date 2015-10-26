namespace wri_webapi.Models.Request
{
    public class SpecificFeatureRequest : UserDetailsRequest
    {
        public int Id { get; set; }
        public int FeatureId { get; set; }
        public string FeatureCategory { get; set; }
    }
}