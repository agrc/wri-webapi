namespace wri_webapi.Models.Database
{
    public class Project
    {
        public int ProjectId { get; set; }
        public int ProjectManagerId { get; set; }
        public string ProjectManagerName { get; set; }
        public string LeadAgency { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Region { get; set; }
        public string Status { get; set; }
    }
}