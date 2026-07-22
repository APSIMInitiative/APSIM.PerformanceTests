using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace APSIM.POStats.Shared.Models
{
    public class Table
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Comparison { get; set; }
        public virtual List<Variable> Variables { get; set; }

        [JsonIgnore]
        public int ApsimFileId { get; set; }

        [JsonIgnore]
        public virtual ApsimFile ApsimFile { get; set; }
    }
}
