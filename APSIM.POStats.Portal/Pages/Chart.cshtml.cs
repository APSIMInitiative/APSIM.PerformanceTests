using APSIM.POStats.Portal.Data;
using APSIM.POStats.Shared;
using APSIM.POStats.Shared.Models;
using APSIM.Shared.Utilities;
using Google.DataTable.Net.Wrapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;

namespace APSIM.POStats.Portal.Pages
{
    /// <summary>
    /// The model for the chart page that displays a predicted/observed chart for a variable.
    /// </summary>
    public class ChartModel : PageModel
    {
        /// <summary>The database context.</summary>
        private readonly StatsDbContext statsDb;

        /// <summary>The current variable.</summary>
        private Variable current;

        /// <summary>The accepted variable.</summary>
        private Variable accepted;

        /// <summary>Constructor.</summary>
        /// <param name="stats">The database context</param>
        public ChartModel(StatsDbContext stats)
        {
            statsDb = stats;
        }

        /// <summary>The variable name to chart.</summary>
        public string Name { get; private set; }

        public int Id { get; private set; }

        /// <summary>The variable comparision.</summary>
        public VariableComparison Variable { get; private set; }

        /// <summary>Invoked when page is first loaded.</summary>
        /// <param name="id">The id of the variable to work with.</param>
        public void OnGet(int id)
        {
            Id = id;
            FindCurrentAndAccepted(id);
        }

        /// <summary>Invoked by the view to get data for the chart.</summary>
        /// <param name="id">The id of the variable to work with.</param>
        /// <returns>A 'context' containing google chart data as json to go back to javascript.</returns>
        public ActionResult OnGetChartData(int id)
        {
            FindCurrentAndAccepted(id);

            DataTable gdt = new DataTable();
            gdt.AddColumn(new Column(ColumnType.Number, "Observed", "Observed"));       // X
            gdt.AddColumn(new Column(ColumnType.Number, "Accepted", "Accepted"));       // Accepted
            gdt.AddColumn(new Column(ColumnType.Number, "Predicted", "Current"));       // Y

            // Add in accepted values.
            VariableFunctions.GetData(accepted, out double[] acceptedPredicted, out double[] acceptedObserved, out string[] acceptedLabels);
            if (acceptedPredicted != null && acceptedObserved != null)
            {
                if (acceptedPredicted.Length != acceptedObserved.Length)
                    throw new Exception("The number of predicted data points does not equal the number of observed data points.");
                if (acceptedPredicted.Length > 0)
                {
                    for (int i = 0; i < acceptedPredicted.Length; i++)
                    {
                        var r = gdt.NewRow();
                        r.AddCell(new Cell(acceptedObserved[i], acceptedObserved[i].ToString("f3")));    // observed
                        r.AddCell(new Cell(acceptedPredicted[i], $"{acceptedPredicted[i]:f3} ({acceptedLabels[i]})"));  // accepted
                        r.AddCell(new Cell(null, null));    // current
                        gdt.AddRow(r);
                    }
                }
            }

            // Add current values to google data table
            VariableFunctions.GetData(current, out double[] predicted, out double[] observed, out string[] labels);
            if (predicted.Length != observed.Length)
                throw new Exception("The number of predicted data points does not equal the number of observed data points.");
            for (int i = 0; i < predicted.Length; i++)
            {
                var r = gdt.NewRow();
                r.AddCell(new Cell(observed[i], observed[i].ToString("f3")));           // observed
                r.AddCell(new Cell(null, null));    // accepted
                r.AddCell(new Cell(predicted[i], $"{predicted[i]:f3} ({labels[i]})"));  // current
                gdt.AddRow(r);
            }

            // Add a 1:1 line
            double maxScale = Math.Max(MathUtilities.Max(predicted), MathUtilities.Max(observed));
            gdt.AddColumn(new Column(ColumnType.Number, "1:1", "1:1"));
            var r2 = gdt.NewRow();
            r2.AddCell(new Cell(0, "1:1 line"));  // X
            r2.AddCell(new Cell(null, null));     // Y
            r2.AddCell(new Cell(null, null));     // Y
            r2.AddCell(new Cell(0, "1:1 line"));  // Y
            gdt.AddRow(r2);

            r2 = gdt.NewRow();
            r2.AddCell(new Cell(maxScale, "1:1 line")); // X
            r2.AddCell(new Cell(null, null));           // Y
            r2.AddCell(new Cell(null, null));           // Y
            r2.AddCell(new Cell(maxScale, "1:1 line")); // Y
            gdt.AddRow(r2);
            return Content(gdt.GetJson());
        }

        /// <summary>
        /// Find current and accepted variables.
        /// </summary>
        /// <param name="id">ID of current variable.</param>
        private void FindCurrentAndAccepted(int id)
        {
            current = statsDb.Variables.Find(id);
            if (current == null)
                throw new Exception($"Cannot find variable with id {id}");
            Name = current.Name;
            accepted = VariableFunctions.GetAccepted(current);
            Variable = new VariableComparison(current, accepted);
        }
    }
}
