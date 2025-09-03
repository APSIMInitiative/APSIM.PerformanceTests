using System;

namespace APSIM.POStats.Shared
{
    public class PullRequestTimer : System.Timers.Timer
    {
        /// <summary>Pull Request Number to be closed when the timeout is reached</summary>
        public int PullRequestNumber;

        /// <summary>Commit id to be checked when the timeout is reached</summary>
        public string CommitId;

        /// <summary>Start Time</summary>
        public DateTime StartTime;
    }
}