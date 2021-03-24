using APSIM.POStats.Shared;
using APSIM.POStats.Shared.Comparison;
using APSIM.POStats.Shared.Models;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace APSIM.POStats.Tests
{
    public class ComparisonTests
    {
        /// <summary>Ensure the ApsimFileComparison status works.</summary>
        [Test]
        public void FileStatusWorks()
        {
            var acceptedPullRequest = new PullRequest()
            {
                Files = new List<ApsimFile>()
                {
                    new ApsimFile() { Name = "file1" },
                    new ApsimFile() { Name = "file3" }
                }
            };
            var currentPullRequest = new PullRequest()
            {
                Files = new List<ApsimFile>()
                {
                    new ApsimFile() { Name = "file2" },
                    new ApsimFile() { Name = "file3" }
                },
                AcceptedPullRequest = acceptedPullRequest
            };

            var files = PullRequestFunctions.GetFiles(currentPullRequest);
            Assert.AreEqual(3, files.Count());

            Assert.AreEqual(ApsimFileComparison.StatusType.Missing, files.First(f => f.Name == "file1").Status);
            Assert.AreEqual(ApsimFileComparison.StatusType.New, files.First(f => f.Name == "file2").Status);
            Assert.AreEqual(ApsimFileComparison.StatusType.NoChange, files.First(f => f.Name == "file3").Status);
        }

        /// <summary>Ensure the TableComparison status works.</summary>
        [Test]
        public void TableStatusWorks()
        {
            var acceptedPullRequest = new PullRequest()
            {
                Files = new List<ApsimFile>()
                {
                    new ApsimFile() 
                    { 
                        Name = "file1",
                        Tables = new List<Table>()
                        { 
                            new Table() { Name = "table1" },
                            new Table() { Name = "table3" },
                        }
                    }
                }
            };
            var currentPullRequest = new PullRequest()
            {
                Files = new List<ApsimFile>()
                {
                    new ApsimFile()
                    {
                        Name = "file1",
                        Tables = new List<Table>()
                        {
                            new Table() { Name = "table2" },
                            new Table() { Name = "table3" },
                        }
                    }                
                },
                AcceptedPullRequest = acceptedPullRequest
            };

            var files = PullRequestFunctions.GetFiles(currentPullRequest);
            var tables = files.First().Tables;
            Assert.AreEqual(3, tables.Count());

            Assert.AreEqual(ApsimFileComparison.StatusType.Missing, tables.First(f => f.Name == "table1").Status);
            Assert.AreEqual(ApsimFileComparison.StatusType.New, tables.First(f => f.Name == "table2").Status);
            Assert.AreEqual(ApsimFileComparison.StatusType.NoChange, tables.First(f => f.Name == "table3").Status);
        }

        /// <summary>Ensure the TableComparison status works when there is no accepted table.</summary>
        [Test]
        public void TableStatusWorksWithNoAccepted()
        {
            var currentPullRequest = new PullRequest()
            {
                Files = new List<ApsimFile>()
                {
                    new ApsimFile()
                    {
                        Name = "file1",
                        Tables = new List<Table>()
                        {
                            new Table() { Name = "table2" },
                            new Table() { Name = "table3" },
                        }
                    }
                },
            };

            var files = PullRequestFunctions.GetFiles(currentPullRequest);
            var tables = files.First().Tables;
            Assert.AreEqual(2, tables.Count());

            Assert.AreEqual(ApsimFileComparison.StatusType.New, tables.First(f => f.Name == "table2").Status);
            Assert.AreEqual(ApsimFileComparison.StatusType.New, tables.First(f => f.Name == "table3").Status);
        }

        /// <summary>Ensure the VariableComparison status works.</summary>
        [Test]
        public void VariableStatusWork()
        {
            var acceptedPullRequest = new PullRequest()
            {
                Files = new List<ApsimFile>()
                {
                    new ApsimFile()
                    {
                        Name = "file1",
                        Tables = new List<Table>()
                        {
                            new Table() 
                            {
                                Name = "table1",
                                Variables = new List<Variable>()
                                { 
                                    new Variable()
                                    {
                                        Name = "variable1",
                                        N = 50,
                                        NSE = 0.8,
                                        RMSE = 1000,
                                        RSR = 0.5
                                    },
                                    new Variable()
                                    {
                                        Name = "variable3",
                                        N = 20,
                                        NSE = 0.7,
                                        RMSE = 800,
                                        RSR = 0.4
                                    }
                                }
                            }
                        }
                    }
                }
            };
            var currentPullRequest = new PullRequest()
            {
                Files = new List<ApsimFile>()
                {
                    new ApsimFile()
                    {
                        Name = "file1",
                        Tables = new List<Table>()
                        {
                            new Table()
                            {
                                Name = "table1",
                                Variables = new List<Variable>()
                                {
                                    new Variable()
                                    {
                                        Name = "variable2",
                                        N = 25,
                                        NSE = 0.7,
                                        RMSE = 500,
                                        RSR = 0.2
                                    },
                                    new Variable()
                                    {
                                        Name = "variable3",
                                        N = 20,
                                        NSE = 0.7,
                                        RMSE = 800,
                                        RSR = 0.4
                                    }
                                }
                            }
                        }
                    }
                },
                AcceptedPullRequest = acceptedPullRequest
            };

            var file = PullRequestFunctions.GetFiles(currentPullRequest);
            var table = file.First().Tables.First();
            var variables = table.Variables;

            Assert.AreEqual(3, variables.Count());

            Assert.AreEqual(VariableComparison.Status.Missing, variables.First(f => f.Name == "variable1").NStatus);
            Assert.AreEqual(VariableComparison.Status.New, variables.First(f => f.Name == "variable2").NStatus);
            Assert.AreEqual(VariableComparison.Status.Same, variables.First(f => f.Name == "variable3").NStatus);
            Assert.AreEqual(VariableComparison.Status.Missing, variables.First(f => f.Name == "variable1").RMSEStatus);
            Assert.AreEqual(VariableComparison.Status.New, variables.First(f => f.Name == "variable2").RMSEStatus);
            Assert.AreEqual(VariableComparison.Status.Same, variables.First(f => f.Name == "variable3").RMSEStatus);
            Assert.AreEqual(VariableComparison.Status.Missing, variables.First(f => f.Name == "variable1").NSEStatus);
            Assert.AreEqual(VariableComparison.Status.New, variables.First(f => f.Name == "variable2").NSEStatus);
            Assert.AreEqual(VariableComparison.Status.Same, variables.First(f => f.Name == "variable3").NSEStatus);
            Assert.AreEqual(VariableComparison.Status.Missing, variables.First(f => f.Name == "variable1").RSRStatus);
            Assert.AreEqual(VariableComparison.Status.New, variables.First(f => f.Name == "variable2").RSRStatus);
            Assert.AreEqual(VariableComparison.Status.Same, variables.First(f => f.Name == "variable3").RSRStatus);
        }

        /// <summary>Ensure the VariableComparison status works with no accepted variable.</summary>
        [Test]
        public void VariableStatusWorksWithNoAccepted()
        {
            var currentPullRequest = new PullRequest()
            {
                Files = new List<ApsimFile>()
                {
                    new ApsimFile()
                    {
                        Name = "file1",
                        Tables = new List<Table>()
                        {
                            new Table()
                            {
                                Name = "table1",
                                Variables = new List<Variable>()
                                {
                                    new Variable()
                                    {
                                        Name = "variable2",
                                        N = 25,
                                        NSE = 0.7,
                                        RMSE = 500,
                                        RSR = 0.2
                                    },
                                    new Variable()
                                    {
                                        Name = "variable3",
                                        N = 20,
                                        NSE = 0.7,
                                        RMSE = 800,
                                        RSR = 0.4
                                    }
                                }
                            }
                        }
                    }
                },
            };

            var file = PullRequestFunctions.GetFiles(currentPullRequest);
            var table = file.First().Tables.First();
            var variables = table.Variables;

            Assert.AreEqual(2, variables.Count());

            Assert.AreEqual(VariableComparison.Status.New, variables.First(f => f.Name == "variable2").NStatus);
            Assert.AreEqual(VariableComparison.Status.New, variables.First(f => f.Name == "variable3").NStatus);
            Assert.AreEqual(VariableComparison.Status.New, variables.First(f => f.Name == "variable2").RMSEStatus);
            Assert.AreEqual(VariableComparison.Status.New, variables.First(f => f.Name == "variable3").RMSEStatus);
            Assert.AreEqual(VariableComparison.Status.New, variables.First(f => f.Name == "variable2").NSEStatus);
            Assert.AreEqual(VariableComparison.Status.New, variables.First(f => f.Name == "variable3").NSEStatus);
            Assert.AreEqual(VariableComparison.Status.New, variables.First(f => f.Name == "variable2").RSRStatus);
            Assert.AreEqual(VariableComparison.Status.New, variables.First(f => f.Name == "variable3").RSRStatus);
        }

        /// <summary>
        /// Ensure two variables, when compared, are considered the same when within 1% tolerance.
        /// </summary>
        [Test]
        public void VariableComparisonWithinTolerance()
        {
            Variable current = new Variable
            {
                N = 50,
                NSE = 0.8,
                RMSE = 1000,
                RSR = 0.5
            };
            Variable accepted = new Variable
            {
                N = 50,
                NSE = 0.807,
                RMSE = 1010,
                RSR = 0.503
            };
            var results = VariableFunctions.Compare(current, accepted);
            Assert.AreEqual(VariableComparison.Status.Same, results.NStatus);
            Assert.AreEqual(VariableComparison.Status.Same, results.NSEStatus);
            Assert.AreEqual(VariableComparison.Status.Same, results.RMSEStatus);
            Assert.AreEqual(VariableComparison.Status.Same, results.RSRStatus);
        }

        /// <summary>
        /// Ensure two variables, when compared, show pass results.
        /// </summary>
        [Test]
        public void VariableComparisonPassResuts()
        {
            var accepted = new Variable
            {
                N = 50,
                NSE = 0.8,
                RMSE = 1000,
                RSR = 0.5
            };
            var current = new Variable
            {
                N = 50,
                NSE = 0.9,
                RMSE = 900,
                RSR = 0.2
            };
            var results = VariableFunctions.Compare(current, accepted);
            Assert.AreEqual(VariableComparison.Status.Same, results.NStatus);
            Assert.AreEqual(VariableComparison.Status.Pass, results.NSEStatus);
            Assert.AreEqual(VariableComparison.Status.Pass, results.RMSEStatus);
            Assert.AreEqual(VariableComparison.Status.Pass, results.RSRStatus);
        }

        /// <summary>
        /// Ensure two variables, when compared, show fail results.
        /// </summary>
        [Test]
        public void VariableComparisonFailResuts()
        {
            var accepted = new Variable
            {
                N = 50,
                NSE = 0.9,
                RMSE = 900,
                RSR = 0.2
            };
            var current = new Variable
            {
                N = 50,
                NSE = 0.8,
                RMSE = 1000,
                RSR = 0.5
            };

            var results = VariableFunctions.Compare(current, accepted);
            Assert.AreEqual(VariableComparison.Status.Same, results.NStatus);
            Assert.AreEqual(VariableComparison.Status.Fail, results.NSEStatus);
            Assert.AreEqual(VariableComparison.Status.Fail, results.RMSEStatus);
            Assert.AreEqual(VariableComparison.Status.Fail, results.RSRStatus);
        }

        /// <summary>
        /// Ensure two variables, when compared, show fail results.
        /// </summary>
        [Test]
        public void VariableComparisonOfInfinityPasses()
        {
            var current = new Variable
            {
                N = 1,
                NSE = double.PositiveInfinity,   // Infinity can happen when N = 1
                RMSE = double.PositiveInfinity,
                RSR = double.PositiveInfinity
            };
            var accepted = new Variable
            {
                N = 1,
                NSE = double.PositiveInfinity,
                RMSE = double.PositiveInfinity,
                RSR = double.PositiveInfinity
            };

            var results = VariableFunctions.Compare(current, accepted);
            Assert.AreEqual(VariableComparison.Status.Same, results.NStatus);
            Assert.AreEqual(VariableComparison.Status.Same, results.NSEStatus);
            Assert.AreEqual(VariableComparison.Status.Same, results.RMSEStatus);
            Assert.AreEqual(VariableComparison.Status.Same, results.RSRStatus);
        }

        /// <summary>
        /// Ensure two variables, when compared, show fail results.
        /// </summary>
        public void EnsureOnlyShowChangedStatsWorks()
        {
            var acceptedPullRequest = new PullRequest()
            {
                Files = new List<ApsimFile>()
                {
                    new ApsimFile()
                    {
                        Name = "file1",
                        Tables = new List<Table>()
                        {
                            new Table()
                            {
                                Name = "table1",
                                Variables = new List<Variable>()
                                {
                                    new Variable()
                                    {
                                        Name = "variable1",
                                        N = 50,
                                        NSE = 0.8,
                                        RMSE = 1000,
                                        RSR = 0.5
                                    },
                                    new Variable()
                                    {
                                        Name = "variable3",
                                        N = 20,
                                        NSE = 0.7,
                                        RMSE = 800,
                                        RSR = 0.4
                                    }
                                }
                            }
                        }
                    }
                }
            };
            var currentPullRequest = new PullRequest()
            {
                Files = new List<ApsimFile>()
                {
                    new ApsimFile()
                    {
                        Name = "file1",
                        Tables = new List<Table>()
                        {
                            new Table()
                            {
                                Name = "table1",
                                Variables = new List<Variable>()
                                {
                                    new Variable()
                                    {
                                        Name = "variable2",
                                        N = 25,
                                        NSE = 0.7,
                                        RMSE = 500,
                                        RSR = 0.2
                                    },
                                    new Variable()
                                    {
                                        Name = "variable3",
                                        N = 20,
                                        NSE = 0.7,
                                        RMSE = 800,
                                        RSR = 0.4
                                    }
                                }
                            }
                        }
                    }
                },
                AcceptedPullRequest = acceptedPullRequest
            };

            var file = PullRequestFunctions.GetFiles(currentPullRequest);
            var table = file.First().Tables.First();
            var variables = table.Variables;

            Assert.AreEqual(3, variables.Count());

            Assert.AreEqual(VariableComparison.Status.Missing, variables.First(f => f.Name == "variable1").NStatus);
            Assert.AreEqual(VariableComparison.Status.New, variables.First(f => f.Name == "variable2").NStatus);
            Assert.AreEqual(VariableComparison.Status.Same, variables.First(f => f.Name == "variable3").NStatus);
            Assert.AreEqual(VariableComparison.Status.Missing, variables.First(f => f.Name == "variable1").RMSEStatus);
            Assert.AreEqual(VariableComparison.Status.New, variables.First(f => f.Name == "variable2").RMSEStatus);
            Assert.AreEqual(VariableComparison.Status.Same, variables.First(f => f.Name == "variable3").RMSEStatus);
            Assert.AreEqual(VariableComparison.Status.Missing, variables.First(f => f.Name == "variable1").NSEStatus);
            Assert.AreEqual(VariableComparison.Status.New, variables.First(f => f.Name == "variable2").NSEStatus);
            Assert.AreEqual(VariableComparison.Status.Same, variables.First(f => f.Name == "variable3").NSEStatus);
            Assert.AreEqual(VariableComparison.Status.Missing, variables.First(f => f.Name == "variable1").RSRStatus);
            Assert.AreEqual(VariableComparison.Status.New, variables.First(f => f.Name == "variable2").RSRStatus);
            Assert.AreEqual(VariableComparison.Status.Same, variables.First(f => f.Name == "variable3").RSRStatus);

        }
    }
}