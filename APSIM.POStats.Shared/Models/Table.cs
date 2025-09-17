using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace APSIM.POStats.Shared.Models
{
    public class Table
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public virtual List<Variable> Variables { get; set; }

        [JsonIgnore]
        public int ApsimFileId { get; set; }

        [JsonIgnore]
        public virtual ApsimFile ApsimFile { get; set; }

        public static List<Variable> MergeVariables(List<Variable> variablesA, List<Variable> variablesB)
        {
            List<Variable> newList = new List<Variable>();

            foreach (Variable a in variablesA)
            {
                newList.Add(a);
            }

            foreach (Variable b in variablesB)
            {
                bool found = false;
                foreach (Variable a in newList)
                {
                    if (a.Name == b.Name)
                    {
                        a.Data.AddRange(b.Data);
                        found = true;
                    }
                }
                if (!found)
                    newList.Add(b);
            }

            return newList;
        }
    }
}
