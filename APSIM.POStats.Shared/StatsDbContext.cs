using APSIM.POStats.Shared.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace APSIM.POStats.Shared
{
    /// <summary>
    /// Stats database context.
    /// </summary>
    public class StatsDbContext : DbContext
    {
        public DbSet<PullRequestDetails> PullRequests { get; set; }
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
        public void OpenPullRequest(int pullRequestNumber, string commitNumber, string author, int count)
        {
            // Try and locate the pull request. If it doesn't exist, create a new pull request instance.
            // If it does exist, delete the old data.
            var pr = GetPullRequest(pullRequestNumber);
            if (pr == null)
            {
                pr = new PullRequestDetails() { PullRequest = pullRequestNumber };
                PullRequests.Add(pr);
            }
            pr.Commit = commitNumber;
            pr.Author = author;
            pr.CountTotal = count;
            pr.CountReturned = 0;
            pr.Output = "";

            pr.Files ??= new();
            pr.Files.Clear();
            pr.DateRun = DateTime.Now;
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
        /// <returns>Reference to stored PullRequest</returns>
        public async Task<PullRequestDetails> AddDataToPullRequest(PullRequestDetails fromPullRequest, int retryCount = 0)
        {

            var pr = new PullRequestDetails();
            try
            {
                // Find the pull request. Should always exist if OpenPullRequest has been called.
                pr = PullRequests.FirstOrDefault(pr => pr.PullRequest == fromPullRequest.PullRequest)
                        ?? throw new Exception($"Cannot find POStats pull request number: {fromPullRequest.PullRequest}");

                pr.CountReturned += 1;

                foreach (ApsimFile file in fromPullRequest.Files)
                    Console.WriteLine($"File \"{file.Name}\" added to PR {fromPullRequest.PullRequest}");

                if (fromPullRequest.Files != null)
                    pr.Files.AddRange(fromPullRequest.Files);

                pr.Output += fromPullRequest.Output;

                await SaveChangesAsync();

                return pr;
            }
            catch (Exception ex)
            {
                // Console.WriteLine($"Retry Number: {retryCount} An issue was encountered while adding data to the pull request: " + ex.ToString());
                // if (retryCount < 5)
                // {
                //     // Wait for a random amount of time before retrying
                //     Console.WriteLine("Retrying to add data to pull request...");
                //     Thread.Sleep(new Random().Next(100, 1000));
                //     AddDataToPullRequest(fromPullRequest, retryCount + 1);
                // }
                // Console.WriteLine("Add data retry attempts exceeded: " + ex.ToString());
                // throw;
                try
                {
                    var wait = new Random().Next(5000, 10000);
                    Thread.Sleep(wait);
                    Console.WriteLine("Unable to add data to pull request, retrying in " + wait + "ms");
                    await SaveChangesAsync();
                    return pr;
                }
                catch (Exception)
                {
                    try
                    {
                        var wait = new Random().Next(100, 1000);
                        Thread.Sleep(wait);
                        Console.WriteLine("Unable to add data again to pull request, retrying in " + wait + "ms");
                        await SaveChangesAsync();
                        return pr;
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Unable to add data to pull request after retries: " + ex.ToString());
                        throw new Exception("Unable to add data to pull request after two retries", ex);
                    }
                }
                throw new Exception("Unable to add data: " + ex.ToString());
            }
        }

        /// <summary>
        /// Close the pull request, indicating that no more data will be sent to it.
        /// </summary>
        /// <remarks>
        /// Use case: A collector calls this method to indicate that it has finished sending data to the pr.
        /// </remarks>
        /// <param name="pullRequestNumber">The pull request number.</param>
        public PullRequestDetails ClosePullRequest(int pullRequestNumber)
        {
            // Find the pull request. Should always exist if OpenPullRequest has been called.
            var pr = PullRequests.FirstOrDefault(pr => pr.PullRequest == pullRequestNumber)
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
        public PullRequestDetails GetMostRecentAcceptedPullRequest()
        {
            return PullRequests.Where(pr => pr.DateStatsAccepted != null)
                               .OrderBy(pr => pr.DateStatsAccepted)
                               .LastOrDefault();
        }

        /// <summary>
        /// Returns if a PullRequest with the given commit is in the collection
        /// </summary>
        /// <param name="pullRequestNumber">The pull request number.</param>
        /// <param name="commitNumber">The commit number.</param>
        public bool PullRequestWithCommitExists(int pullRequestNumber, string commitNumber)
        {
            // Try and locate the pull request
            PullRequestDetails pr = GetPullRequest(pullRequestNumber);
            if (pr == null)
                return false;
            else if (pr.Commit == commitNumber)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Get a PullRequest data from the stored collection
        /// </summary>
        /// <param name="pullRequestNumber">The pull request number.</param>
        /// <param name="commitNumber">The commit number.</param>
        public PullRequestDetails GetPullRequest(int pullRequestNumber)
        {
            // Try and locate the pull request
            return PullRequests.FirstOrDefault(pr => pr.PullRequest == pullRequestNumber);
        }

        /// <summary>
        /// Override model creating event so that we can convert the plural property
        /// names (e.g. PullRequests) into singular DB table names (e.g. PullRequest)
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PullRequestDetails>().ToTable("PullRequest");
            modelBuilder.Entity<ApsimFile>().ToTable("ApsimFile");
            modelBuilder.Entity<Table>().ToTable("Table");
            modelBuilder.Entity<Variable>().ToTable("Variable");
        }
    }
}
