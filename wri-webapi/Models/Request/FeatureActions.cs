namespace wri_webapi.Models.Request
{
    public class FeatureActions
    {
        // Top level item for terrestrial/aquatic eg. Herbicide Application 
        // or Second level item for guzzler eg. maintenance
        public string Action { get; set; }
        // Second level items for terrestrial/aquatic eg. Ground, Spot Treatment
        public FeatureTreatments[] Treatments { get; set; }
        // Top level type of thing being created eg. barbed wire fence
        public string Type { get; set; }
        // The comments 
        public string Description { get; set; }
    }
}