namespace wri_webapi.Models.Request
{
    public class EditSpecificFeatureRequest : SaveFeatureRequest
    {
        public int Id { get; set; }
        public int FeatureId { get; set; }
    }
}