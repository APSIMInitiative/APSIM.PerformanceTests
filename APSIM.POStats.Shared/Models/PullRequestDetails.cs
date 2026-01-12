using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace APSIM.POStats.Shared.Models
{
    [Index(nameof(PullRequest), IsUnique = true)]
    public class PullRequestDetails
    {
        public int Id { get; set; }
        public int PullRequest { get; set; }
        public string Commit { get; set; }
        public string Author { get; set; }
        public DateTime DateRun { get; set; }

        /// <summary>Number of nodes that should return</summary>
        public int CountTotal { get; set; }

        public DateTime? DateStatsAccepted { get; set; }
        public int? AcceptedPullRequestId { get; set; }

        public virtual PullRequestDetails AcceptedPullRequest { get; set; }

        public virtual List<ApsimFile> Files { get; set; }

        /// <summary>Statuses of the nodes that ran the tests. True = success, false = failure.</summary>
        public virtual List<bool> Status { get; set; }

        /// <summary>Log outputs from validation that are returned</summary>
        public virtual List<string> Outputs { get; set; }

        /// <summary>The name of the Azure Batch pool that was used to run tests for this pull request.</summary>
        public string Pool { get; set; }
    }
}