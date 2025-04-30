namespace APSIM.POStats.Shared.GitHub
{
    /// <summary>Class encapsulating pull request details.</summary>
    public class GitHubStatusDetails
    {
        /// <summary>State to set status to (success, pending, failure)</summary>
        public string state;

        /// <summary>Url to link check to (the POStats page for that PR)</summary>
        public string target_url;

        /// <summary>Description of status (Same, Different)</summary>
        public string description;

        /// <summary>Name of Check (APSIM.POStats)</summary>
        public string context;

        /// <summary>Constructor</summary>
        public GitHubStatusDetails(string state, string target_url, string description, string context)
        {
            this.state = state;
            this.target_url = target_url;
            this.description = description;
            this.context = context;
        }
    }
}
