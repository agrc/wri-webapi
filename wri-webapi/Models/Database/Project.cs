using wri_webapi.Extensions;

namespace wri_webapi.Models.Database
{
    public class Project
    {
        private string _terrestrial;
        private string _affectedArea;
        private string _aquatic;
        private string _easement;
        private string _stream;
        private string _county;
        private string _sgma;
        private string _focus;
        private string _landown;
        public int ProjectId { get; set; }
        public int ProjectManagerId { get; set; }
        public string ProjectManagerName { get; set; }
        public string LeadAgency { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Region { get; set; }
        public string Status { get; set; }
        public string Features { get; set; }

        public string TerrestrialSqMeters
        {
            get { return _terrestrial.InAcres(); }
            set { _terrestrial = value; }
        }

        public string AffectedAreaSqMeters
        {
            get { return _affectedArea.InAcres(); }
            set { _affectedArea = value; }
        }

        public string AquaticSqMeters
        {
            get { return _aquatic.InAcres(); }
            set { _aquatic = value; }
        }

        public string EasementSqMeters
        {
            get { return _easement.InAcres(); }
            set { _easement = value; }
        }

        public string StreamLnMeters
        {
            get { return _stream.InMiles(); }
            set { _stream = value; }
        }

        public string CountySqMeters
        {
            get { return _county.InAcres(); }
            set { _county = value; }
        }

        public string FocusSqMeters
        {
            get { return _focus.InAcres(); }
            set { _focus = value; }
        }
        
        public string SgmaSqMeters
        {
            get { return _sgma.InAcres(); }
            set { _sgma = value; }
        }

        public string LandOwnerSqMeters
        {
            get { return _landown.InAcres(); }
            set { _landown = value; }
        }
    }
}