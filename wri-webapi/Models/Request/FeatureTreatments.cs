namespace wri_webapi.Models.Request
{
    public class FeatureTreatments
    {
        // Second level items for terrestrial/aquatic eg. Ground, Spot Treatment
        public string Treatment { get; set; }
        // Thrird level items for terrestrial/aquatic eg. Roundup
        public string[] Herbicides { get; set; }
    }
}