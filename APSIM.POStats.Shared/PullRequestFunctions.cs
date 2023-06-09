using APSIM.POStats.Shared.Comparison;
using APSIM.POStats.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace APSIM.POStats.Shared
{
    public class PullRequestFunctions
    {
        /// <summary>
        /// Does the specified pull request pass?
        /// </summary>
        /// <param name="pullRequest"></param>
        /// <returns></returns>
        public static VariableComparison.Status GetStatus(PullRequest pullRequest)
        {
            bool allBetterOrSame = true;
            bool allSame = true;
            foreach (var file in GetFileComparisons(pullRequest))
            {
                if (file.Status != ApsimFileComparison.StatusType.NoChange)
                    return VariableComparison.Status.Different;
                
                foreach (var table in file.Tables)
                {
                    if (table.Status != ApsimFileComparison.StatusType.NoChange)
                        return VariableComparison.Status.Different;

                    allBetterOrSame = allBetterOrSame && table.VariableComparisons.All(v => v.IsBetterOrSame);
                    allSame = allSame && table.VariableComparisons.All(v => v.IsSame);
                }
            }
            if (allSame)
                return VariableComparison.Status.Same; 
            else if (allBetterOrSame)
                return VariableComparison.Status.Better;
            else
                return VariableComparison.Status.Different;
        }

        /// <summary>Get a list of all files for a pull request.</summary>
        /// <param name="pullRequest">The pull request.</param>
        public static List<ApsimFileComparison> GetFileComparisons(PullRequest pullRequest)
        {
            var files = new List<ApsimFileComparison>();
            foreach (var currentFile in pullRequest.Files)
            {
                var acceptedFile = pullRequest.AcceptedPullRequest?.Files.Find(f => f.Name == currentFile.Name);
                files.Add(new ApsimFileComparison(currentFile, acceptedFile));
            }

            // Add in files that are in the accepted PR but not in the current PR.
            if (pullRequest.AcceptedPullRequest != null)
            {
                var filesNotInCurrent = pullRequest.AcceptedPullRequest.Files.Except(files.Select(f => f.Accepted));
                foreach (var acceptedFile in filesNotInCurrent)
                    files.Add(new ApsimFileComparison(null, acceptedFile));
            }

            return files.OrderBy(f => f.Name).ToList();
        }

        /// <summary>
        /// Remove all files, tables and variables that are the same as the accepted ones.
        /// </summary>
        /// <param name="files"></param>
        public static void RemoveSame(List<ApsimFileComparison> files)
        {
            foreach (var file in files)
            {
                // Remove variables that are the same.
                foreach (var table in file.Tables)
                    table.VariableComparisons.RemoveAll(v => v.IsSame);

                // Remove empty tables.
                file.Tables.RemoveAll(t => t.IsSame && t.VariableComparisons.Count == 0);
            }

            files.RemoveAll(f => f.Tables.Count == 0);
        }

        /// <summary>Update the stats in the specified pull request.</summary>
        /// <param name="pullRequest"></param>
        public static void UpdateStats(PullRequest pullRequest)
        {
            foreach (var file in pullRequest.Files)
                foreach (var table in file.Tables)
                    foreach (var variable in table.Variables)
                        VariableFunctions.EnsureStatsAreCalculated(variable, forceRecalculate: true);
        }
    }
}
