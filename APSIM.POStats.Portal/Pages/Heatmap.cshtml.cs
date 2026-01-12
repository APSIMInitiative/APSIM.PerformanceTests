using APSIM.POStats.Shared;
using APSIM.POStats.Shared.Models;
using Google.DataTable.Net.Wrapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Linq;

namespace APSIM.POStats.Portal.Pages
{
    public class HeatmapModel : PageModel
    {
        /// <summary>The database context.</summary>
        private StatsDbContext statsDb;

        /// <summary>Constructor.</summary>
        /// <param name="stats">The database context.</param>
        public HeatmapModel(StatsDbContext stats)
        {
            statsDb = stats;
        }

        /// <summary>The pull request being analysed.</summary>
        public PullRequestDetails PullRequest { get; private set; }

        public string BaseUrl {  get { return $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}"; } }

        /// <summary>Invoked when page is first loaded.</summary>
        /// <param name="pullRequestNumber">The pull request id to work with.</param>
        public void OnGet(int id)
        {
            PullRequest = statsDb.PullRequests.Find(id);
            if (PullRequest == null)
                throw new Exception($"Cannot find pull request #{id} in stats database");
        }

        /// <summary>Invoked by the view to get data for the chart.</summary>
        /// <param name="id">The id of the pull request to work with.</param>
        /// <returns>A 'context' containing google chart data as json to go back to javascript.</returns>
        public ActionResult OnGetChartData(int id)
        {
            // Create a pull request instance from the id.
            var pullRequest = statsDb.PullRequests.Find(id);

            // Create a Google DataTable.
            DataTable gdt = new DataTable();
            gdt.AddColumn(new Column(ColumnType.String, "ID"));
            gdt.AddColumn(new Column(ColumnType.String, "Parent"));
            gdt.AddColumn(new Column(ColumnType.Number, "Scale"));

            // Add a root node called 'stats'
            AddRow(gdt, "Stats", null, 0);

            foreach (var file in pullRequest.Files.OrderBy(f => f.Name))
            {
                AddRow(gdt, file.Name, "Stats", 0);

                foreach (var table in file.Tables.OrderBy(t => t.Name))
                {
                    foreach (var current in table.Variables.OrderBy(v => v.Name))
                    {
                        var accepted = VariableFunctions.GetAccepted(current);
                        var comparison = VariableFunctions.Compare(accepted, current);
                        AddRow(gdt, $"{file.Name}.{table.Name}.{current.Name}.N {current.Id}", file.Name,    CalculateHeatmapScale(comparison.NPercentDifference));
                        AddRow(gdt, $"{file.Name}.{table.Name}.{current.Name}.RMSE {current.Id}", file.Name, CalculateHeatmapScale(comparison.RMSEPercentDifference));
                        AddRow(gdt, $"{file.Name}.{table.Name}.{current.Name}.NSE {current.Id}", file.Name,  CalculateHeatmapScale(comparison.NSEPercentDifference));
                        AddRow(gdt, $"{file.Name}.{table.Name}.{current.Name}.RSR {current.Id}", file.Name,  CalculateHeatmapScale(comparison.RSRPercentDifference));
                    }
                }
            }

            return Content(gdt.GetJson());
        }

        /// <summary>
        /// Add a row to a google data table.
        /// </summary>
        /// <param name="gdt">The data table.</param>
        /// <param name="id">The value for the id column.</param>
        /// <param name="parent">The value for the parent column.</param>
        /// <param name="value">The value.</param>
        private static void AddRow(DataTable gdt, string id, string parent, double value)
        {
            var r = gdt.NewRow();
            r.AddCell(new Cell(id));
            r.AddCell(new Cell(parent));
            r.AddCell(new Cell(value));
            gdt.AddRow(r);
        }

        /// <summary>
        /// Calculate a heatmap scale.
        /// </summary>
        /// <param name="percentDifference"></param>
        /// <returns></returns>
        private static double CalculateHeatmapScale(double percentDifference)
        {
            if (double.IsNaN(percentDifference))
                return -100;
            else
                return percentDifference;
        }
    }
}
