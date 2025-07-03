using Microsoft.EntityFrameworkCore;
using APSIM.POStats.Shared.Models;
using APSIM.POStats.Shared;
using System.Globalization;

namespace APSIM.POStats.Tests;

[TestFixture]
public class StatsDbTests
{
    /// <summary>Test that a pull request can be opened when it already exists.</summary>
    [Test]
    public void TestOpenPullRequestThatAlreadyExists()
    {
        using var db = CreateInMemoryDB("db1");

        db.OpenPullRequest(1234, "1", "author", 0);

        // Make sure the pr exists and has no data.
        var pr = db.PullRequests.First(pr => pr.Number == 1234);
        Assert.That(pr, !Is.Null);
        Assert.That(pr.Files.Count, Is.EqualTo(0));
    }


    /// <summary>Test that a pull request can be opened when it doesn't already exist.</summary>
    [Test]
    public void TestOpenPullRequestThatDoesntAlreadyExist()
    {
        using var db = CreateInMemoryDB("db2");

        db.OpenPullRequest(5678, "1", "author", 0);

        // Make sure the pr exists and has no ydata.
        var pr = db.PullRequests.First(pr => pr.Number == 5678);
        Assert.That(pr, !Is.Null);
        Assert.That(pr.Files.Count, Is.EqualTo(0));
    }


    /// <summary>Test that data can be added to a pull request.</summary>
    [Test]
    public void TestAddDataToPullRequest()
    {
        using var db = CreateInMemoryDB("db3");

        db.OpenPullRequest(1234, "1", "author", 0);
        PullRequest prToAdd = new()
        {
            Number = 1234,
            Files = new()
            {
                new ApsimFile()
                {
                    Tables = new()
                    {
                        new Table()
                        {
                            Variables = new()
                            {
                                new Variable()
                                {
                                    Name = "B",
                                    Data = new()
                                    {
                                        new VariableData()
                                        {
                                            Predicted = 100.0,
                                            Observed = 110.0
                                        },
                                        new VariableData()
                                        {
                                            Predicted = 200.0,
                                            Observed = 210.0
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };
        db.AddDataToPullRequest(prToAdd);
        db.ClosePullRequest(1234);

        // Make sure the pr exists and has no data.
        var pr = db.PullRequests.First(pr => pr.Number == 1234);
        Assert.That(pr, !Is.Null);
        Assert.That(pr.DateStatsAccepted, Is.Null); // stats not accepted
        Assert.That(pr.AcceptedPullRequest.Number, Is.EqualTo(5678));     // most recent pr that has been accepted.
        Assert.That(pr.Files.Count, Is.EqualTo(1));
        var variable = pr.Files[0].Tables[0].Variables[0];
        Assert.That(variable.Name, Is.EqualTo("B"));
        Assert.That(variable.Data[0].Predicted, Is.EqualTo(100));
        Assert.That(variable.Data[0].Observed, Is.EqualTo(110));
        Assert.That(variable.Data[1].Predicted, Is.EqualTo(200));
        Assert.That(variable.Data[1].Observed, Is.EqualTo(210));
        Assert.That(variable.N, Is.EqualTo(2));
        Assert.That(variable.RMSE, Is.GreaterThan(0));
        Assert.That(variable.NSE, Is.GreaterThan(0));
        Assert.That(variable.RSR, Is.GreaterThan(0));
    }


    /// <summary>
    /// Create an in memory db.
    /// </summary>
    private StatsDbContext CreateInMemoryDB(string databaseName)
    {
        // https://www.thecodebuzz.com/dbcontext-mock-and-unit-test-entity-framework-net-core/
        // create In Memory Database

        var options = new DbContextOptionsBuilder<StatsDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        var db = new StatsDbContext(options);
        db.PullRequests.Add(new PullRequest
        {
            Id = 1,
            Number = 1234,
            DateStatsAccepted = new DateTime(2021,1,1),
            Files = new()
            {
                new ApsimFile()
                {
                    Tables = new()
                    {
                        new Table()
                        {
                            Variables = new()
                            {
                                new Variable()
                                {
                                    Name = "A",
                                    Data = new()
                                    {
                                        new VariableData()
                                        {
                                            Predicted = 10.0,
                                            Observed = 11.0
                                        },
                                        new VariableData()
                                        {
                                            Predicted = 20.0,
                                            Observed = 21.0
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        });
        db.PullRequests.Add(new PullRequest
        {
            Id = 2,
            Number = 5678,
            DateStatsAccepted = new DateTime(2020,1,1),
            Files = new()
            {
                new ApsimFile()
                {
                    Tables = new()
                    {
                        new Table()
                        {
                            Variables = new()
                            {
                                new Variable()
                                {
                                    Name = "A",
                                    Data = new()
                                    {
                                        new VariableData()
                                        {
                                            Predicted = 12.0,
                                            Observed = 13.0
                                        },
                                        new VariableData()
                                        {
                                            Predicted = 22.0,
                                            Observed = 23.0
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        });
        db.SaveChanges();
        return db;
    }
}
