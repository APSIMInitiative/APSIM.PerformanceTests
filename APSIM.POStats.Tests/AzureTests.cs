using APSIM.POStats.Portal.Models;


namespace APSIM.POStats.Tests;

[TestFixture]
public class AzureTests
{
    // This can be used for testing the CloseBatchPoolAsync method.
    // Make sure you have a pool called "testpool" in your Azure Batch account before
    // [Test]
    // public async Task TestCloseBatchPoolAsync()
    // {
    //     using (File.OpenRead(".env"))
    //     if (File.Exists(".env"))
    //     {
    //         var lines = File.ReadAllLines(".env");
    //         foreach (var line in lines)
    //         {
    //             var parts = line.Split('=', 2);
    //             if (parts.Length == 2)
    //             {
    //                 Environment.SetEnvironmentVariable(parts[0], parts[1]);
    //             }
    //         }
    //     }
    //     string poolName = "testpool";
    //     await AzureBatchManager.CloseBatchPoolAsync(poolName);
    // }

}
    