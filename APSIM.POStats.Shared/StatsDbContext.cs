using APSIM.POStats.Shared.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace APSIM.POStats.Shared
{
    /// <summary>
    /// Stats database context.
    /// </summary>
    public class StatsDbContext : DbContext
    {
        public DbSet<PullRequest> PullRequests { get; set; }
        public DbSet<ApsimFile> ApsimFiles { get; set; }
        public DbSet<Table> Tables { get; set; }
        public DbSet<Variable> Variables { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="options"></param>
        public StatsDbContext(DbContextOptions<StatsDbContext> options)
         : base(options)
        {
        }

        /// <summary>
        /// Open a pull request to be ready for new data.
        /// </summary>
        /// <remarks>
        /// Use case: A collector first calls OpenPullRequest to make it ready to receive new data.
        /// </remarks>
        /// <param name="pullRequestNumber">The pull request number.</param>
        /// <param name="author">The author of the pull request</param>
        public void OpenPullRequest(int pullRequestNumber, string author)
        {
            // Try and locate the pull request. If it doesn't exist, create a new pull request instance.
            // If it does exist, delete the old data.
            var pr = PullRequests.FirstOrDefault(pr => pr.Number == pullRequestNumber);
            if (pr == null)
                PullRequests.Add(new PullRequest()
                {
                    Number = pullRequestNumber,
                    Files = new()
                });

            pr.Files.Clear();
            pr.DateRun = DateTime.Now;
            pr.Author = author;
            pr.DateStatsAccepted = null;
            pr.AcceptedPullRequest = null;
            SaveChanges();
        }

        /// <summary>
        /// Add file data to a pull request.
        /// </summary>
        /// <remarks>
        /// Use case: A collector calls this method to add data to a pull request.
        /// </remarks>
        /// <param name="pullRequest">The pull request to copy the data from..</param>
        public void AddDataToPullRequest(PullRequest fromPullRequest)
        {
            // Find the pull request. Should always exist if OpenPullRequest has been called.
            var pr = PullRequests.FirstOrDefault(pr => pr.Number == fromPullRequest.Number)
                     ?? throw new Exception($"Cannot find POStats pull request number: {fromPullRequest.Number}");

            pr.Files.AddRange(fromPullRequest.Files);
            SaveChanges();
        }

        /// <summary>
        /// Close the pull request, indicating that no more data will be sent to it.
        /// </summary>
        /// <remarks>
        /// Use case: A collector calls this method to indicate that it has finished sending data to the pr.
        /// </remarks>
        /// <param name="pullRequestNumber">The pull request number.</param>
        public PullRequest ClosePullRequest(int pullRequestNumber)
        {
            // Find the pull request. Should always exist if OpenPullRequest has been called.
            var pr = PullRequests.FirstOrDefault(pr => pr.Number == pullRequestNumber)
                     ?? throw new Exception($"Cannot find POStats pull request number: {pullRequestNumber}");

            // Assign the current accepted pull request.
            pr.AcceptedPullRequest = GetMostRecentAcceptedPullRequest();

            // Calculate stats for each variable in each table in each file.
            foreach (var file in pr.Files)
                foreach (var table in file.Tables)
                    foreach (var variable in table.Variables)
                        VariableFunctions.EnsureStatsAreCalculated(variable);
            SaveChanges();
            return pr;
        }

        /// <summary>Get the most recent accepted pull request.</summary>
        public PullRequest GetMostRecentAcceptedPullRequest()
        {
            return PullRequests.Where(pr => pr.DateStatsAccepted != null)
                               .OrderBy(pr => pr.DateStatsAccepted)
                               .LastOrDefault();
        }

        /// <summary>
        /// Override model creating event so that we can convert the plural property
        /// names (e.g. PullRequests) into singular DB table names (e.g. PullRequest)
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PullRequest>().ToTable("PullRequest");
            modelBuilder.Entity<ApsimFile>().ToTable("ApsimFile");
            modelBuilder.Entity<Table>().ToTable("Table");
            modelBuilder.Entity<Variable>().ToTable("Variable");
        }
    }
}
