using wri_webapi.Extensions;

namespace wri_webapi.Models.Database
{
    public class Project
    {
        private string _terrestrialAcres;
        private string _affectedArea;
        private string _aquaticAcres;
        private string _easementAcres;
        private string _streamMiles;
        public int ProjectId { get; set; }
        public int ProjectManagerId { get; set; }
        public string ProjectManagerName { get; set; }
        public string LeadAgency { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Region { get; set; }
        public string Status { get; set; }
        public string Features { get; set; }

        public string TerrestrialAcres
        {
            get { return _terrestrialAcres.InAcres(); }
            set { _terrestrialAcres = value; }
        }

        public string AffectedArea
        {
            get { return _affectedArea.InAcres(); }
            set { _affectedArea = value; }
        }

        public string AquaticAcres
        {
            get { return _aquaticAcres.InAcres(); }
            set { _aquaticAcres = value; }
        }

        public string EasementAcres
        {
            get { return _easementAcres.InAcres(); }
            set { _easementAcres = value; }
        }

        public string StreamMiles
        {
            get { return _streamMiles.InMiles(); }
            set { _streamMiles = value; }
        }

    }
}