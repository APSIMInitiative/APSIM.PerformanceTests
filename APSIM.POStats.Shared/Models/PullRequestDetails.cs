using System;
using System.Collections.Generic;

namespace APSIM.POStats.Shared.Models
{
    public class PullRequestDetails
    {
        public int Id { get; set; }
        public int PullRequest { get; set; }
        public string Commit { get; set; }
        public string Author { get; set; }
        public DateTime DateRun { get; set; }
        public DateTime? DateStatsAccepted { get; set; }
        public virtual List<ApsimFile> Files { get; set; }
        public int CountTotal { get; set; }
        public int CountReturned { get; set; }
        public virtual List<StdOutput> Output { get; set; }
        public int? AcceptedPullRequestId { get; set; }
        public virtual PullRequestDetails AcceptedPullRequest { get; set; }
    }
}