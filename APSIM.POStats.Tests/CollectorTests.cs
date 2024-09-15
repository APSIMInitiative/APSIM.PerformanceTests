using APSIM.Shared.Utilities;
using System.Reflection;
using Microsoft.Data.Sqlite;
using APSIM.POStats.Shared;
using System.Text.Json;

namespace APSIM.POStats.Tests;

[TestFixture]
public class TestsCollector
{
    private string path;
    private SqliteConnection database;

    [SetUp]
    public void SetUp()
    {
        // Set the working directory to the unit test bin directory.
        var workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        Directory.SetCurrentDirectory(workingDirectory);

        // Create and put temporary files in a temp directory.
        path = Path.Combine(Path.GetTempPath(), "Test");
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
        Directory.CreateDirectory(path);

        var filename = Path.Combine(path, "Test.apsimx");
        using (var writer = new FileStream(filename, FileMode.Create))
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("APSIM.POStats.Tests.Test.apsimx"))
                if (stream != null)
                    stream.CopyTo(writer);

        // Create an empty database.
        var dbFileName = Path.Combine(path, "Test.db");
        database = new SqliteConnection($"Data source={dbFileName}");
        database.Open();
    }

    [TearDown]
    public void TearDown()
    {
        database.Close();
        SqliteConnection.ClearAllPools();
        Directory.Delete(path, recursive: true);
    }

    /// <summary>Ensures the collector works as expected.</summary>
    [Test]
    public void TestEnsureNormalCollectorOperationWorks()
    {
        SqliteUtilities.CreateTable(database, DataTableUtilities.FromCSV("_Simulations",
            "ID,Name" + Environment.NewLine +
            " 1,Sim1" + Environment.NewLine
            ));

        SqliteUtilities.CreateTable(database, DataTableUtilities.FromCSV("PO1",
            "SimulationID,Date,Predicted.A,Observed.A" + Environment.NewLine +
            " 1,2000-01-01, 10.0, 11.0" + Environment.NewLine +
            " 1,2000-01-02, 20.0, 21.0" + Environment.NewLine
            ));

        SqliteUtilities.CreateTable(database, DataTableUtilities.FromCSV("PO2",
            "SimulationID,Date,Predicted.A,Observed.A" + Environment.NewLine +
            " 1,2000-01-03, 100.0, 110.0" + Environment.NewLine +
            " 1,2000-01-04, 200.0, 210.0" + Environment.NewLine
            ));

        var pullRequest = Collector.RetrieveData(1234, new DateTime(2000, 1, 1), null, new string[] { path });

        string jsonString = JsonSerializer.Serialize(pullRequest);;

        Assert.That(pullRequest.Files.ToList().Count, Is.EqualTo(1));
        Assert.That(pullRequest.Files[0].Tables.Count, Is.EqualTo(2));

        // Table 1.
        Assert.That(pullRequest.Files[0].Tables[0].Name, Is.EqualTo("PO1"));
        Assert.That(pullRequest.Files[0].Tables[0].Variables.Count, Is.EqualTo(1));
        Assert.That(pullRequest.Files[0].Tables[0].Variables[0].Name, Is.EqualTo("A"));
        Assert.That(pullRequest.Files[0].Tables[0].Variables[0].N, Is.EqualTo(2));
        Assert.That(pullRequest.Files[0].Tables[0].Variables[0].RMSE, Is.EqualTo(1));
        Assert.That(pullRequest.Files[0].Tables[0].Variables[0].NSE, Is.EqualTo(0.96));
        Assert.That(pullRequest.Files[0].Tables[0].Variables[0].RSR, Is.EqualTo(0.1414213562373095));
        Assert.That(pullRequest.Files[0].Tables[0].Variables[0].Data.Count, Is.EqualTo(2));
        Assert.That(pullRequest.Files[0].Tables[0].Variables[0].Data[0].Observed, Is.EqualTo(11.0));
        Assert.That(pullRequest.Files[0].Tables[0].Variables[0].Data[0].Predicted, Is.EqualTo(10.0));
        Assert.That(pullRequest.Files[0].Tables[0].Variables[0].Data[0].Label, Is.EqualTo("Simulation: Sim1, Date: 2000-01-01"));
        Assert.That(pullRequest.Files[0].Tables[0].Variables[0].Data[1].Observed, Is.EqualTo(21.0));
        Assert.That(pullRequest.Files[0].Tables[0].Variables[0].Data[1].Predicted, Is.EqualTo(20.0));
        Assert.That(pullRequest.Files[0].Tables[0].Variables[0].Data[1].Label, Is.EqualTo("Simulation: Sim1, Date: 2000-01-02"));

        // Table 2.
        Assert.That(pullRequest.Files[0].Tables[1].Name, Is.EqualTo("PO2"));
        Assert.That(pullRequest.Files[0].Tables[1].Variables.Count, Is.EqualTo(1));
        Assert.That(pullRequest.Files[0].Tables[1].Variables[0].Name, Is.EqualTo("A"));
        Assert.That(pullRequest.Files[0].Tables[1].Variables[0].N, Is.EqualTo(2));
        Assert.That(pullRequest.Files[0].Tables[1].Variables[0].RMSE, Is.EqualTo(10.0));
        Assert.That(pullRequest.Files[0].Tables[1].Variables[0].NSE, Is.EqualTo(0.96));
        Assert.That(pullRequest.Files[0].Tables[1].Variables[0].RSR, Is.EqualTo(0.1414213562373095));

        Assert.That(pullRequest.Files[0].Tables[1].Variables[0].Data.Count, Is.EqualTo(2));
        Assert.That(pullRequest.Files[0].Tables[1].Variables[0].Data[0].Observed, Is.EqualTo(110.0));
        Assert.That(pullRequest.Files[0].Tables[1].Variables[0].Data[0].Predicted, Is.EqualTo(100.0));
        Assert.That(pullRequest.Files[0].Tables[1].Variables[0].Data[0].Label, Is.EqualTo("Simulation: Sim1"));
        Assert.That(pullRequest.Files[0].Tables[1].Variables[0].Data[1].Observed, Is.EqualTo(210.0));
        Assert.That(pullRequest.Files[0].Tables[1].Variables[0].Data[1].Predicted, Is.EqualTo(200.0));
        Assert.That(pullRequest.Files[0].Tables[1].Variables[0].Data[1].Label, Is.EqualTo("Simulation: Sim1"));
    }

    /// <summary>Ensures the collector doesn't find predicted columns that are string datatype.</summary>
    [Test]
    public void EnsurePredictedStringColumnsAreIgnored()
    {
        SqliteUtilities.CreateTable(database, DataTableUtilities.FromCSV("_Simulations",
            "ID,Name" + Environment.NewLine +
            " 1,Sim1" + Environment.NewLine
            ));

        SqliteUtilities.CreateTable(database, DataTableUtilities.FromCSV("PO1",
            "SimulationID,Date,Predicted.A,Observed.A" + Environment.NewLine +
            " 1,2000-01-01, x, 11.0" + Environment.NewLine +
            " 1,2000-01-02, x, 21.0" + Environment.NewLine
            ));
        database.Close();

        var pullRequest = Collector.RetrieveData(1234, new DateTime(2000, 1, 1), null, new string[] { path });
        Assert.That(pullRequest.Files.ToList().Count, Is.EqualTo(0));
    }

    /// <summary>Ensures the collector doesn't find observed columns that are string datatype.</summary>
    [Test]
    public void EnsureObservedStringColumnsAreIgnored()
    {
        SqliteUtilities.CreateTable(database, DataTableUtilities.FromCSV("_Simulations",
            "ID,Name" + Environment.NewLine +
            " 1,Sim1" + Environment.NewLine
            ));
        SqliteUtilities.CreateTable(database, DataTableUtilities.FromCSV("PO1",
            "SimulationID,Date,Predicted.A,Observed.A" + Environment.NewLine +
            " 1,2000-01-01, 10.0, x" + Environment.NewLine +
            " 1,2000-01-02, 20.0, x" + Environment.NewLine
            ));
        database.Close();

        var pullRequest = Collector.RetrieveData(1234, new DateTime(2000, 1, 1), null, new string[] { path });
        Assert.That(pullRequest.Files.ToList().Count, Is.EqualTo(0));
    }

    /// <summary>Ensures the collector doesn't find observed columns that are string datatype.</summary>
    [Test]
    public void EnsureObservedStringValuesInRowsAreIgnored()
    {
        SqliteUtilities.CreateTable(database, DataTableUtilities.FromCSV("_Simulations",
            "ID,Name" + Environment.NewLine +
            " 1,Sim1" + Environment.NewLine
            ));
        SqliteUtilities.CreateTable(database, DataTableUtilities.FromCSV("PO1",
            "SimulationID,Date,Predicted.A,Observed.A" + Environment.NewLine +
            " 1,2000-01-01, 10.0, 11.0" + Environment.NewLine +
            " 1,2000-01-02, 20.0, x" + Environment.NewLine
            ));
        database.Close();

        var pullRequest = Collector.RetrieveData(1234, new DateTime(2000, 1, 1), null, new string[] { path });
        Assert.That(pullRequest.Files.ToList().Count, Is.EqualTo(1));
        Assert.That(pullRequest.Files[0].Tables.Count, Is.EqualTo(1));

        // Table 1.
        Assert.That(pullRequest.Files[0].Tables[0].Name, Is.EqualTo("PO1"));
        Assert.That(pullRequest.Files[0].Tables[0].Variables.Count, Is.EqualTo(1));
        Assert.That(pullRequest.Files[0].Tables[0].Variables[0].Name, Is.EqualTo("A"));
        Assert.That(pullRequest.Files[0].Tables[0].Variables[0].Data.Count, Is.EqualTo(1));
        Assert.That(pullRequest.Files[0].Tables[0].Variables[0].Data[0].Observed, Is.EqualTo(11.0));
        Assert.That(pullRequest.Files[0].Tables[0].Variables[0].Data[0].Predicted, Is.EqualTo(10.0));
    }

    /// <summary>Ensures the collector finds a predicted/observed table that isn't under the DataStore in the .apsimx file.</summary>
    [Test]
    public void EnsurePOTableNotUnderDataStoreIsFound()
    {
        SqliteUtilities.CreateTable(database, DataTableUtilities.FromCSV("_Simulations",
            "ID,Name" + Environment.NewLine +
            " 1,Sim1" + Environment.NewLine
            ));
        SqliteUtilities.CreateTable(database, DataTableUtilities.FromCSV("PO3",  // PO3 is not under DataStore in test.apsimx
            "SimulationID,Date,Predicted.A,Observed.A" + Environment.NewLine +
            " 1,2000-01-01, 10.0, 11.0" + Environment.NewLine +
            " 1,2000-01-02, 20.0, 21.0" + Environment.NewLine
            ));
        database.Close();

        var pullRequest = Collector.RetrieveData(1234, new DateTime(2000, 1, 1), null, new string[] { path });
        Assert.That(pullRequest.Files.ToList().Count, Is.EqualTo(1));
        Assert.That(pullRequest.Files[0].Tables.Count, Is.EqualTo(1));

        // Table 1.
        Assert.That(pullRequest.Files[0].Tables[0].Name, Is.EqualTo("PO3"));
        Assert.That(pullRequest.Files[0].Tables[0].Variables.Count, Is.EqualTo(1));
        Assert.That(pullRequest.Files[0].Tables[0].Variables[0].Name, Is.EqualTo("A"));
        Assert.That(pullRequest.Files[0].Tables[0].Variables[0].Data.Count, Is.EqualTo(2));
    }

    /// <summary>Ensures the collector will find integer predicted / observed numbers.</summary>
    [Test]
    public void EnsureCollectorFindsIntegers()
    {
        SqliteUtilities.CreateTable(database, DataTableUtilities.FromCSV("_Simulations",
            "ID,Name" + Environment.NewLine +
            " 1,Sim1" + Environment.NewLine
            ));

        SqliteUtilities.CreateTable(database, DataTableUtilities.FromCSV("PO1",
            "SimulationID,Date,Predicted.A,Observed.A" + Environment.NewLine +
            " 1,2000-01-01, 10, 11" + Environment.NewLine +
            " 1,2000-01-02, 20, 21" + Environment.NewLine
            ));
        database.Close();

        var pullRequest = Collector.RetrieveData(1234, new DateTime(2000, 1, 1), null, new string[] { path });
        Assert.That(pullRequest.Files.ToList().Count, Is.EqualTo(1));
        Assert.That(pullRequest.Files[0].Tables.Count, Is.EqualTo(1));

        // Table 1.
        Assert.That(pullRequest.Files[0].Tables[0].Name, Is.EqualTo("PO1"));
        Assert.That(pullRequest.Files[0].Tables[0].Variables.Count, Is.EqualTo(1));
        Assert.That(pullRequest.Files[0].Tables[0].Variables[0].Name, Is.EqualTo("A"));
        Assert.That(pullRequest.Files[0].Tables[0].Variables[0].N, Is.EqualTo(2));
        Assert.That(pullRequest.Files[0].Tables[0].Variables[0].RMSE, Is.EqualTo(1));
        Assert.That(pullRequest.Files[0].Tables[0].Variables[0].NSE, Is.EqualTo(0.96));
        Assert.That(pullRequest.Files[0].Tables[0].Variables[0].RSR, Is.EqualTo(0.1414213562373095));
        Assert.That(pullRequest.Files[0].Tables[0].Variables[0].Data.Count, Is.EqualTo(2));
        Assert.That(pullRequest.Files[0].Tables[0].Variables[0].Data[0].Observed, Is.EqualTo(11.0));
        Assert.That(pullRequest.Files[0].Tables[0].Variables[0].Data[0].Predicted, Is.EqualTo(10.0));
        Assert.That(pullRequest.Files[0].Tables[0].Variables[0].Data[0].Label, Is.EqualTo("Simulation: Sim1, Date: 2000-01-01"));
        Assert.That(pullRequest.Files[0].Tables[0].Variables[0].Data[1].Observed, Is.EqualTo(21.0));
        Assert.That(pullRequest.Files[0].Tables[0].Variables[0].Data[1].Predicted, Is.EqualTo(20.0));
        Assert.That(pullRequest.Files[0].Tables[0].Variables[0].Data[1].Label, Is.EqualTo("Simulation: Sim1, Date: 2000-01-02"));
    }
}
