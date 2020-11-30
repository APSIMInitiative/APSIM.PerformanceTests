﻿using APSIM.POStats.Shared.Models;
using System.Collections.Generic;
using System.Linq;

namespace APSIM.POStats.Shared.Comparison
{
    public class TableComparison
    {
        /// <summary>Constructor.</summary>
        public TableComparison(Table currentTable, Table acceptedTable)
        {
            Current = currentTable;
            Accepted = acceptedTable;
        }

        /// <summary>The current table.</summary>
        public Table Current = null;

        /// <summary>The accepted file.</summary>
        public Table Accepted = null;

        /// <summary>Name of table.</summary>
        public string Name { get { if (Current != null) return Current.Name; else return Accepted.Name; } }

        /// <summary>Is this a new table or a missing table or the same table?</summary>
        public ApsimFileComparison.StatusType Status
        {
            get
            {
                if (Current == null)
                    return ApsimFileComparison.StatusType.Missing;
                else if (Accepted == null) 
                    return ApsimFileComparison.StatusType.New;
                else
                    return ApsimFileComparison.StatusType.NoChange;
            }
        }

        /// <summary>Get a list of all variables for this table.</summary>
        public IEnumerable<VariableComparison> GetVariables()
        {
            var variables = new List<VariableComparison>();
            var matchingAcceptedVariables = new List<Variable>();
            foreach (var currentVariable in Current.Variables)
            {
                var acceptedVariable = Accepted?.Variables.Find(t => t.Name == currentVariable.Name);
                matchingAcceptedVariables.Add(acceptedVariable);
                variables.Add(new VariableComparison(currentVariable, acceptedVariable));
            }

            // Add in variables that are in the accepted table but not in the current table.
            if (Accepted != null)
            {
                var variablesNotInCurrent = Accepted.Variables.Except(matchingAcceptedVariables);
                foreach (var acceptedTable in variablesNotInCurrent)
                    variables.Add(new VariableComparison(null, acceptedTable));
            }

            return variables.OrderBy(v => v.Name);
        }
    }
}