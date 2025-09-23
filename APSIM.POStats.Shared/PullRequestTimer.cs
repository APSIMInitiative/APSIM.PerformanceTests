using System;

namespace APSIM.POStats.Shared
{
    public class PullRequestTimer : System.Timers.Timer
    {
        /// <summary>Pull Request Number to be closed when the timeout is reached</summary>
        public int PullRequest;

        /// <summary>Commit id to be checked when the timeout is reached</summary>
        public string Commit;

        /// <summary>Start Time</summary>
        public DateTime StartTime;

        /// <summary>Number of nodes that should return</summary>
        public int CountTotal;
    }
}