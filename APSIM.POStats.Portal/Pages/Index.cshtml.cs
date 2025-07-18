using APSIM.POStats.Shared;
using APSIM.POStats.Shared.Comparison;
using APSIM.POStats.Shared.Models;
using APSIM.Shared.Utilities;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Linq;

namespace APSIM.POStats.Portal.Pages
{
    /// <summary>
    /// The model for the main index page.
    /// </summary>
    public class IndexModel : PageModel
    {
        /// <summary>The database context.</summary>
        private readonly StatsDbContext statsDb;

        /// <summary>Constructor.</summary>
        /// <param name="stats">The database context.</param>
        public IndexModel(StatsDbContext stats)
        {
            statsDb = stats;
        }

        /// <summary>Only show changed stats?</summary>
        public bool OnlyShowChangedStats { get; set; } = true;

        /// <summary>Show standard output logs</summary>
        public bool ShowLogs { get; set; } = true;

        /// <summary>The pull request being analysed.</summary>
        public PullRequestDetails PullRequest { get; private set; }

        /// <summary>The Url for the web site.</summary>
        public string BaseUrl { get { return $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}"; } }

        /// <summary>Invoked when page is first loaded.</summary>
        /// <param name="pullRequestNumber">The pull request to work with.</param>
        public void OnGet(int pullRequestNumber)
        {
            PullRequest = statsDb.PullRequests.FirstOrDefault(pr => pr.PullRequest == pullRequestNumber);
            if (PullRequest == null)
                throw new Exception($"Cannot find pull request #{pullRequestNumber} in stats database");

            VariableComparison.Status status = PullRequestFunctions.GetStatus(PullRequest);
        }

        public void OnPost()
        {
            int pullRequestNumber = Convert.ToInt32(Request.Form["PullRequestNumber"]);
            PullRequest = statsDb.PullRequests.FirstOrDefault(pr => pr.PullRequest == pullRequestNumber);
            if (PullRequest == null)
                throw new Exception($"Cannot find pull request #{pullRequestNumber} in stats database");

            var statsLabel = Request.Form["StatsLabel"].ToString();
            OnlyShowChangedStats = statsLabel == "Showing all stats.";

            var logsLabel = Request.Form["LogsLabel"].ToString();
            ShowLogs = logsLabel == "Show Output Logs";
        }

        /// <summary>Emit html to display tick/cross.</summary>
        /// <param name="state">The state to determing weather to display tick or cross.</param>
        /// <returns>HTML for the tick/cross.</returns>
        public static string EmitTickCross(VariableComparison.Status state)
        {
            if (state == VariableComparison.Status.Better)
                return "<span style = \"font-weight: bold; color: Green;\" >&#10004;</span>";

            else if (state == VariableComparison.Status.Different)
                return "<span style = \"font-weight: bold; color: Red;\" >&#10008;</span>";
            else
                return string.Empty;
        }

        /// <summary>Emit html to display a number.</summary>
        /// <param name="number">The number to emit.</param>
        /// <param name="numDecimalPlaces">The number of decimal places.</param>
        /// <param name="numberSignificantFigures">The number of significant figures to display.</param>
        /// <param name="isAccepted">Is this an accepted number.</param>
        /// <returns>HTML for the number.</returns>
        public static string EmitNumber(double number, int numDecimalPlaces, int numberSignificantFigures, bool isAccepted)
        {
            string st;
            if (double.IsNaN((double)number))
                st = "---";
            else if (numberSignificantFigures > 0)
                st = MathUtilities.FormatSignificantDigits(number, numberSignificantFigures);
            else
                st = number.ToString($"F{numDecimalPlaces}");

            if (isAccepted)
                return "(" + st + ")";
            else
                return st;
        }

        /// <summary>Emit html to display a number.</summary>
        /// <param name="number">The number to emit.</param>
        /// <param name="numDecimalPlaces">The number of decimal places.</param>
        /// <param name="numberSignificantFigures">The number of significant figures to display.</param>
        /// <param name="isAccepted">Is this an accepted number.</param>
        /// <returns>HTML for the number.</returns>
        public static string EmitNumber(int number, bool isAccepted)
        {
            string st;
            if (number == int.MaxValue)
                st = "---";
            else
                st = number.ToString();

            if (isAccepted)
                return "(" + st + ")";
            else
                return st;
        }


        /// <summary>Emit html to display variable name</summary>
        /// <param name="variable">The variable.</param>
        /// <returns>HTML for the variable text.</returns>
        public static string EmitVariableName(VariableComparison variable)
        {
            if (variable.NStatus == VariableComparison.Status.Missing)
                return $"{variable.Name}<span title=\"Variable missing\"style = \"font-weight: bold; color: Red;\" >&#10008 missing</span>";
            else if (variable.NStatus == VariableComparison.Status.New)
                return $"{variable.Name}<span title=\"New variable - not in accepted\" style = \"font-weight: bold; color: Red;\" >&#10008 new</span>";
            else
                return variable.Name;
        }

        /// <summary>Emit html to display table name</summary>
        /// <param name="table">The table.</param>
        /// <returns>HTML for the table text.</returns>
        public static string EmitTableName(TableComparison table)
        {
            if (table.Status == ApsimFileComparison.StatusType.Missing)
                return $"{table.Name}<span title=\"Table missing\"style = \"font-weight: bold; color: Red;\" >&#10008 missing</span>";
            else if (table.Status == ApsimFileComparison.StatusType.New)
                return $"{table.Name}<span title=\"New table - not in accepted\" style = \"font-weight: bold; color: Red;\" >&#10008 new</span>";
            else
                return table.Name;
        }

        /// <summary>Emit html to display file name</summary>
        /// <param name="file">The file.</param>
        /// <returns>HTML for the file text.</returns>
        public static string EmitFileName(ApsimFileComparison file)
        {
            if (file.Status == ApsimFileComparison.StatusType.Missing)
                return $"{file.Name}<span title=\"File missing\"style = \"font-weight: bold; color: Red;\" >&#10008 missing</span>";
            else if (file.Status == ApsimFileComparison.StatusType.New)
                return $"{file.Name}<span title=\"New file - not in accepted\" style = \"font-weight: bold; color: Red;\" >&#10008 new</span>";
            else
                return file.Name;
        }
    }
}