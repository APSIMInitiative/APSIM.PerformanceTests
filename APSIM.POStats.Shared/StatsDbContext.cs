using APSIM.POStats.Shared.Models;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using SQLitePCL;

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
        public void OpenPullRequest(int pullRequestNumber, string commitNumber, string author, int count, string pool)
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
            pr.DateRun = DateTime.Now;
            pr.CountTotal = count;

            pr.DateStatsAccepted = null;
            pr.AcceptedPullRequestId = null;
            pr.AcceptedPullRequest = null;

            pr.Files ??= new();
            pr.Files.Clear();

            pr.Outputs ??= new();
            pr.Outputs.Clear();

            pr.Status ??= new();
            pr.Status.Clear();

            pr.Pool = pool;

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
        public PullRequestDetails AddDataToPullRequest(PullRequestDetails fromPullRequest)
        {
            var pr = new PullRequestDetails();

            // Find the pull request. Should always exist if OpenPullRequest has been called.
            pr = PullRequests.FirstOrDefault(pr => pr.PullRequest == fromPullRequest.PullRequest && pr.Commit == fromPullRequest.Commit);
            if (pr == null)
                throw new Exception($"Cannot find POStats pull request number: {fromPullRequest.PullRequest}");

            foreach (ApsimFile file in fromPullRequest.Files)
                Console.WriteLine($"File \"{file.Name}\" added to PR {fromPullRequest.PullRequest}");

            if (fromPullRequest.Files != null)
                pr.Files.AddRange(fromPullRequest.Files);

            if (fromPullRequest.Outputs != null)
                pr.Outputs.AddRange(fromPullRequest.Outputs);

            if (fromPullRequest.Status != null)
                pr.Status.AddRange(fromPullRequest.Status);

            SaveChanges();

            return pr;
        }

        /// <summary>Get a list of all files for a pull request.</summary>
        /// <param name="pullRequest">The pull request.</param>
        public static void MergeSplitFiles(PullRequestDetails pullRequest, string prefix, string newName)
        {
            //Merge back together any files that have been split
            List<ApsimFile> wheatFiles = new List<ApsimFile>();

            //Merge Wheat back together
            ApsimFile wheatFile = new ApsimFile();
            wheatFile.Name = newName;
            wheatFile.Tables = new List<Table>();

            foreach (ApsimFile currentFile in pullRequest.Files)
            {
                if (currentFile.Name.Contains(prefix))
                {
                    wheatFiles.Add(currentFile);

                    foreach (Table table in currentFile.Tables)
                    {
                        Table wheatTable = null;
                        foreach (Table t in wheatFile.Tables)
                            if (t.Name == table.Name)
                                wheatTable = t;

                        if (wheatTable == null)
                        {
                            wheatTable = new Table();
                            wheatTable.Name = table.Name;
                            wheatTable.Variables = new List<Variable>();
                            wheatFile.Tables.Add(wheatTable);
                        }

                        foreach (Variable variable in table.Variables)
                        {
                            Variable wheatVar = null;
                            foreach (Variable v in wheatTable.Variables)
                                if (v.Name == variable.Name)
                                    wheatVar = v;
                            if (wheatVar == null)
                            {
                                wheatVar = new Variable();
                                wheatVar.Name = variable.Name;
                                wheatVar.N = variable.N;
                                wheatVar.RMSE = variable.RMSE;
                                wheatVar.NSE = variable.NSE;
                                wheatVar.RSR = variable.RSR;
                                wheatVar.Data = new List<VariableData>();
                                wheatTable.Variables.Add(wheatVar);
                            }
                            foreach (VariableData data in variable.Data)
                            {
                                VariableData wheatData = new VariableData();
                                wheatData.Label = data.Label;
                                wheatData.Predicted = data.Predicted;
                                wheatData.Observed = data.Observed;
                                wheatVar.Data.Add(wheatData);
                            }
                        }
                    }
                }
            }

            pullRequest.Files.Add(wheatFile);
            //foreach (ApsimFile file in wheatFiles)
            //{
            //    pullRequest.Files.Remove(file);
            //}
            
            // Calculate stats for each variable in each table in each file.
            foreach (var file in pullRequest.Files)
            {
                Console.WriteLine(file.Name);
                foreach (var table in file.Tables)
                {
                    foreach (var variable in table.Variables)
                    {
                        VariableFunctions.EnsureStatsAreCalculated(variable);
                    }
                }
            }

            //SaveChanges();
        }

        /// <summary>
        /// Add file data to a pull request.
        /// </summary>
        /// <remarks>
        /// Use case: A collector calls this method to add data to a pull request.
        /// </remarks>
        /// <param name="pullRequest">The pull request to copy the data from..</param>
        /// <returns>Reference to stored PullRequest</returns>
        public int GetNumberOfCompletesInPullRequest(int pullrequestnumber, string commitid)
        {
            var pr = new PullRequestDetails();

            // Find the pull request. Should always exist if OpenPullRequest has been called.
            pr = PullRequests.FirstOrDefault(pr => pr.PullRequest == pullrequestnumber && pr.Commit == commitid);
            if (pr == null)
                throw new Exception($"Cannot find POStats pull request number: {pullrequestnumber}");

            return pr.Status.Count;
        }

        public bool SaveChangesMultipleTries(int retries = 0)
        {
            try
            {
                SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                if (retries < 5)
                {
                    var wait = new Random().Next(1000, 5000);
                    Console.WriteLine("Unable to add data to pull request, retrying in " + wait + "ms");
                    Thread.Sleep(wait);
                    return SaveChangesMultipleTries(retries + 1);
                }
                else
                {
                    throw new Exception(ex.Message);
                }
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
            {
                Console.WriteLine(file.Name);
                foreach (var table in file.Tables)
                {
                    foreach (var variable in table.Variables)
                    {
                        VariableFunctions.EnsureStatsAreCalculated(variable);
                    }
                }
            }

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
