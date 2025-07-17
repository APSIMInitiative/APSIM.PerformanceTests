using APSIM.POStats.Shared;
using APSIM.POStats.Shared.Comparison;
using APSIM.POStats.Shared.Models;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace APSIM.POStats.Tests
{
    public class ComparisonTests
    {
        /// <summary>Ensure the ApsimFileComparison status works.</summary>
        [Test]
        public void FileStatusWorks()
        {
            var acceptedPullRequest = new PullRequestDetails()
            {
                Files = new List<ApsimFile>()
                {
                    new ApsimFile() { Name = "file1" },
                    new ApsimFile() { Name = "file3" }
                }
            };
            var currentPullRequest = new PullRequestDetails()
            {
                Files = new List<ApsimFile>()
                {
                    new ApsimFile() { Name = "file2" },
                    new ApsimFile() { Name = "file3" }
                },
                AcceptedPullRequest = acceptedPullRequest
            };

            var files = PullRequestFunctions.GetFileComparisons(currentPullRequest);
            Assert.That(files.Count(), Is.EqualTo(3));

            Assert.That(files.First(f => f.Name == "file1").Status, Is.EqualTo(ApsimFileComparison.StatusType.Missing));
            Assert.That(files.First(f => f.Name == "file2").Status, Is.EqualTo(ApsimFileComparison.StatusType.New));
            Assert.That(files.First(f => f.Name == "file3").Status, Is.EqualTo(ApsimFileComparison.StatusType.NoChange));
        }

        /// <summary>Ensure the TableComparison status works.</summary>
        [Test]
        public void TableStatusWorks()
        {
            var acceptedPullRequest = new PullRequestDetails()
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
            var currentPullRequest = new PullRequestDetails()
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

            var files = PullRequestFunctions.GetFileComparisons(currentPullRequest);
            var tables = files.First().Tables;
            Assert.That(tables.Count(), Is.EqualTo(3));

            Assert.That(tables.First(f => f.Name == "table1").Status, Is.EqualTo(ApsimFileComparison.StatusType.Missing));
            Assert.That(tables.First(f => f.Name == "table2").Status, Is.EqualTo(ApsimFileComparison.StatusType.New));
            Assert.That(tables.First(f => f.Name == "table3").Status, Is.EqualTo(ApsimFileComparison.StatusType.NoChange));
        }

        /// <summary>Ensure the TableComparison status works when there is no accepted table.</summary>
        [Test]
        public void TableStatusWorksWithNoAccepted()
        {
            var currentPullRequest = new PullRequestDetails()
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

            var files = PullRequestFunctions.GetFileComparisons(currentPullRequest);
            var tables = files.First().Tables;
            Assert.That(tables.Count(), Is.EqualTo(2));

            Assert.That(tables.First(f => f.Name == "table2").Status, Is.EqualTo(ApsimFileComparison.StatusType.New));
            Assert.That(tables.First(f => f.Name == "table3").Status, Is.EqualTo(ApsimFileComparison.StatusType.New));
        }

        /// <summary>Ensure the VariableComparison status works.</summary>
        [Test]
        public void VariableStatusWork()
        {
            var acceptedPullRequest = new PullRequestDetails()
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
            var currentPullRequest = new PullRequestDetails()
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

            var fileComparisons = PullRequestFunctions.GetFileComparisons(currentPullRequest);
            var table = fileComparisons.First().Tables.First();
            var variables = table.VariableComparisons;

            Assert.That(variables.Count(), Is.EqualTo(3));

            Assert.That(variables.First(f => f.Name == "variable1").NStatus, Is.EqualTo(VariableComparison.Status.Missing));
            Assert.That(variables.First(f => f.Name == "variable2").NStatus, Is.EqualTo(VariableComparison.Status.New));
            Assert.That(variables.First(f => f.Name == "variable3").NStatus, Is.EqualTo(VariableComparison.Status.Same));
            Assert.That(variables.First(f => f.Name == "variable1").RMSEStatus, Is.EqualTo(VariableComparison.Status.Missing));
            Assert.That(variables.First(f => f.Name == "variable2").RMSEStatus, Is.EqualTo(VariableComparison.Status.New));
            Assert.That(variables.First(f => f.Name == "variable3").RMSEStatus, Is.EqualTo(VariableComparison.Status.Same));
            Assert.That(variables.First(f => f.Name == "variable1").NSEStatus, Is.EqualTo(VariableComparison.Status.Missing));
            Assert.That(variables.First(f => f.Name == "variable2").NSEStatus, Is.EqualTo(VariableComparison.Status.New));
            Assert.That(variables.First(f => f.Name == "variable3").NSEStatus, Is.EqualTo(VariableComparison.Status.Same));
            Assert.That(variables.First(f => f.Name == "variable1").RSRStatus, Is.EqualTo(VariableComparison.Status.Missing));
            Assert.That(variables.First(f => f.Name == "variable2").RSRStatus, Is.EqualTo(VariableComparison.Status.New));
            Assert.That(variables.First(f => f.Name == "variable3").RSRStatus, Is.EqualTo(VariableComparison.Status.Same));
        }

        /// <summary>Ensure the VariableComparison status works with no accepted variable.</summary>
        [Test]
        public void VariableStatusWorksWithNoAccepted()
        {
            var currentPullRequest = new PullRequestDetails()
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

            var file = PullRequestFunctions.GetFileComparisons(currentPullRequest);
            var table = file.First().Tables.First();
            var variables = table.VariableComparisons;

            Assert.That(variables.Count(), Is.EqualTo(2));

            Assert.That(variables.First(f => f.Name == "variable2").NStatus, Is.EqualTo(VariableComparison.Status.New));
            Assert.That(variables.First(f => f.Name == "variable3").NStatus, Is.EqualTo(VariableComparison.Status.New));
            Assert.That(variables.First(f => f.Name == "variable2").RMSEStatus, Is.EqualTo(VariableComparison.Status.New));
            Assert.That(variables.First(f => f.Name == "variable3").RMSEStatus, Is.EqualTo(VariableComparison.Status.New));
            Assert.That(variables.First(f => f.Name == "variable2").NSEStatus, Is.EqualTo(VariableComparison.Status.New));
            Assert.That(variables.First(f => f.Name == "variable3").NSEStatus, Is.EqualTo(VariableComparison.Status.New));
            Assert.That(variables.First(f => f.Name == "variable2").RSRStatus, Is.EqualTo(VariableComparison.Status.New));
            Assert.That(variables.First(f => f.Name == "variable3").RSRStatus, Is.EqualTo(VariableComparison.Status.New));
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
            Assert.That(results.NStatus, Is.EqualTo(VariableComparison.Status.Same));
            Assert.That(results.NSEStatus, Is.EqualTo(VariableComparison.Status.Same));
            Assert.That(results.RMSEStatus, Is.EqualTo(VariableComparison.Status.Better));
            Assert.That(results.RSRStatus, Is.EqualTo(VariableComparison.Status.Better));
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
            Assert.That(results.NStatus, Is.EqualTo(VariableComparison.Status.Same));
            Assert.That(results.NSEStatus, Is.EqualTo(VariableComparison.Status.Better));
            Assert.That(results.RMSEStatus, Is.EqualTo(VariableComparison.Status.Better));
            Assert.That(results.RSRStatus, Is.EqualTo(VariableComparison.Status.Better));
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
            Assert.That(results.NStatus, Is.EqualTo(VariableComparison.Status.Same));
            Assert.That(results.NSEStatus, Is.EqualTo(VariableComparison.Status.Different));
            Assert.That(results.RMSEStatus, Is.EqualTo(VariableComparison.Status.Different));
            Assert.That(results.RSRStatus, Is.EqualTo(VariableComparison.Status.Different));
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
            Assert.That(results.NStatus, Is.EqualTo(VariableComparison.Status.Same));
            Assert.That(results.NSEStatus, Is.EqualTo(VariableComparison.Status.Same));
            Assert.That(results.RMSEStatus, Is.EqualTo(VariableComparison.Status.Same));
            Assert.That(results.RSRStatus, Is.EqualTo(VariableComparison.Status.Same));
        }

        /// <summary>
        /// Ensure two variables, when compared, show fail results.
        /// </summary>
        [Test]
        public void EnsureOnlyShowChangedStatsWorks()
        {
            var acceptedPullRequest = new PullRequestDetails()
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
            var currentPullRequest = new PullRequestDetails()
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

            var file = PullRequestFunctions.GetFileComparisons(currentPullRequest);
            var table = file.First().Tables.First();
            var variables = table.VariableComparisons;

            Assert.That(variables.Count(), Is.EqualTo(3));

            Assert.That(variables.First(f => f.Name == "variable1").NStatus, Is.EqualTo(VariableComparison.Status.Missing));
            Assert.That(variables.First(f => f.Name == "variable2").NStatus, Is.EqualTo(VariableComparison.Status.New));
            Assert.That(variables.First(f => f.Name == "variable3").NStatus, Is.EqualTo(VariableComparison.Status.Same));
            Assert.That(variables.First(f => f.Name == "variable1").RMSEStatus, Is.EqualTo(VariableComparison.Status.Missing));
            Assert.That(variables.First(f => f.Name == "variable2").RMSEStatus, Is.EqualTo(VariableComparison.Status.New));
            Assert.That(variables.First(f => f.Name == "variable3").RMSEStatus, Is.EqualTo(VariableComparison.Status.Same));
            Assert.That(variables.First(f => f.Name == "variable1").NSEStatus, Is.EqualTo(VariableComparison.Status.Missing));
            Assert.That(variables.First(f => f.Name == "variable2").NSEStatus, Is.EqualTo(VariableComparison.Status.New));
            Assert.That(variables.First(f => f.Name == "variable3").NSEStatus, Is.EqualTo(VariableComparison.Status.Same));
            Assert.That(variables.First(f => f.Name == "variable1").RSRStatus, Is.EqualTo(VariableComparison.Status.Missing));
            Assert.That(variables.First(f => f.Name == "variable2").RSRStatus, Is.EqualTo(VariableComparison.Status.New));
            Assert.That(variables.First(f => f.Name == "variable3").RSRStatus, Is.EqualTo(VariableComparison.Status.Same));

        }

        /// <summary>Ensure a missing table is detected.</summary>
        [Test]
        public void MissingTableDetected()
        {
            var acceptedPullRequest = new PullRequestDetails()
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
                                    }
                                }
                            },
                            new Table()
                            {
                                Name = "table2",
                                Variables = new List<Variable>()
                                {
                                    new Variable()
                                    {
                                        Name = "variable2",
                                        N = 50,
                                        NSE = 0.8,
                                        RMSE = 1000,
                                        RSR = 0.5
                                    }
                                }
                            }

                        }
                    }
                }
            };
            var currentPullRequest = new PullRequestDetails()
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
                                Name = "table2",
                                Variables = new List<Variable>()
                                {
                                    new Variable()
                                    {
                                        Name = "variable2",
                                        N = 50,
                                        NSE = 0.8,
                                        RMSE = 1000,
                                        RSR = 0.5
                                    }
                                }
                            }
                        }
                    }
                },
                AcceptedPullRequest = acceptedPullRequest
            };

            var files = PullRequestFunctions.GetFileComparisons(currentPullRequest);
            var tables = files.First().Tables;
            Assert.That(tables.Count(), Is.EqualTo(2));

            Assert.That(tables.First(f => f.Name == "table1").Status, Is.EqualTo(ApsimFileComparison.StatusType.Missing));
            Assert.That(tables.Last(f => f.Name == "table2").Status, Is.EqualTo(ApsimFileComparison.StatusType.NoChange));

            VariableComparison.Status status = PullRequestFunctions.GetStatus(currentPullRequest);
            Assert.That(status, Is.EqualTo(VariableComparison.Status.Different));
        }


    }
}