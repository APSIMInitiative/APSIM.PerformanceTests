using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace APSIM.POStats.Shared.Models
{
    public class ApsimFile
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public virtual List<Table> Tables { get; set; }

        [JsonIgnore]
        public int PullRequestId { get; set; }
        [JsonIgnore]
        public virtual PullRequestDetails PullRequest { get; set; }
    }
}