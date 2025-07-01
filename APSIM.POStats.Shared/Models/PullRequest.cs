using System;
using System.Collections.Generic;

namespace APSIM.POStats.Shared.Models
{
    public class PullRequest
    {
        public int Id { get; set; }
        public int Number { get; set; }
        public string LastCommit { get; set; }
        public string Author { get; set; }
        public DateTime DateRun { get; set; }
        public DateTime? DateStatsAccepted { get; set; }
        public virtual List<ApsimFile> Files { get; set; }
        public int Count { get; set; }
        public virtual List<StdOutput> Output { get; set; }
        public int? AcceptedPullRequestId { get; set; }
        public virtual PullRequest AcceptedPullRequest { get; set; }
    }
}