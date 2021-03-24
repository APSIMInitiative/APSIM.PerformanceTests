using APSIM.POStats.Portal.Controllers;
using APSIM.POStats.Portal.Data;
using APSIM.POStats.Shared;
using APSIM.POStats.Shared.Models;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;

namespace APSIM.POStats.Tests
{
    public class UploadStatsTests
    {
        /// <summary>
        /// Make sure stats for a variable are calculated correctly.
        /// </summary>
        [Test]
        public void UploadStatsWhenDbEmpty()
        {
            var dbOptions = new DbContextOptionsBuilder<StatsDbContext>();
            dbOptions.UseLazyLoadingProxies().UseSqlite("Data Source=tests.db");
            var db = new StatsDbContext(dbOptions.Options);
            // Create the destination database if necessary.
            db.Database.EnsureCreated();

            var uploader = new UploadPODataController(db);

            var pr = new PullRequest()
            {
                Files = new List<ApsimFile>()
                {
                    new ApsimFile() { Name = "file2" },
                    new ApsimFile() { Name = "file3" }
                },
                AcceptedPullRequest = null
            };
            uploader.UploadPullRequest(pr);
        }
    }
}