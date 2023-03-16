using APSIM.POStats.Shared.Models;
using System.Collections.Generic;
using System.Linq;

namespace APSIM.POStats.Shared.Comparison
{
    public class ApsimFileComparison
    {
        /// <summary>Constructor.</summary>
        public ApsimFileComparison(ApsimFile currentFile, ApsimFile acceptedFile)
        {
            Current = currentFile;
            Accepted = acceptedFile;
            Tables = GetTables();
        }

        /// <summary>The current file.</summary>
        public ApsimFile Current { get; private set; } = null;

        /// <summary>The accepted file.</summary>
        public ApsimFile Accepted { get; private set; } = null;

        /// <summary>Name of file.</summary>
        public string Name { get { if (Current != null) return Current.Name; else return Accepted.Name; } }

        public enum StatusType
        {
            /// <summary>This is a new file (not in accepted).</summary>
            New,

            /// <summary>This is a missing file in current (is in accepted).</summary>
            Missing,

            /// <summary>File exists in both current and excepted.</summary>
            NoChange
        }

        /// <summary>Is this a new file or a missing file or the same file?</summary>
        public StatusType Status 
        { 
            get 
            { 
                if (Current == null) 
                    return StatusType.Missing; 
                else if (Accepted == null) 
                    return StatusType.New; 
                else 
                    return StatusType.NoChange; 
            } 
        }

        /// <summary>Is this file the same as the accepted file?</summary>
        public bool IsSame
        {
            get
            {
                if (Current == null || Accepted == null)
                    return false;

                foreach (var table in Tables)
                    if (!table.IsSame)
                        return false;

                return true;
            }
        }

        /// <summary>Get a list of all tables for this file.</summary>
        public List<TableComparison> Tables { get; }


        /// <summary>Find all tables for this file.</summary>
        private List<TableComparison> GetTables()
        {
            var tables = new List<TableComparison>();
            if (Current != null && Current.Tables != null)
            {
                foreach (var currentTable in Current.Tables)
                {
                    var acceptedTable = Accepted?.Tables.Find(t => t.Name == currentTable.Name);
                    tables.Add(new TableComparison(currentTable, acceptedTable));
                }

                // Add in tables that are in the accepted file but not in the current file.
                if (Accepted != null)
                {
                    var tablesNotInCurrent = Accepted.Tables.Except(tables.Select(t => t.Accepted));
                    foreach (var acceptedTable in tablesNotInCurrent)
                        tables.Add(new TableComparison(null, acceptedTable));
                }
            }

            return tables.OrderBy(t => t.Name).ToList();
        }
    }
}