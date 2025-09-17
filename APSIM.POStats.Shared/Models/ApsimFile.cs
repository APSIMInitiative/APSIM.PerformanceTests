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

        public static ApsimFile Merge(ApsimFile fileA, ApsimFile fileB)
        {
            ApsimFile newFile = new ApsimFile();
            newFile.Id = fileA.Id;
            newFile.Name = fileA.Name;
            newFile.PullRequestId = fileA.PullRequestId;
            newFile.PullRequest = fileA.PullRequest;

            newFile.Tables = new List<Table>();
            foreach (Table a in fileA.Tables)
            {
                newFile.Tables.Add(a);
            }

            foreach (Table b in fileB.Tables)
            {
                bool found = false;
                foreach (Table a in newFile.Tables)
                {
                    if (a.Name == b.Name)
                    {
                        a.Variables = Table.MergeVariables(a.Variables, b.Variables);
                        found = true;
                    }
                }
                if (!found)
                    newFile.Tables.Add(b);
            }

            return newFile;
        }
    }
}