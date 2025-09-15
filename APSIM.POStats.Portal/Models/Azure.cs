using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;
using Microsoft.Azure.Batch.Common;
namespace APSIM.POStats.Portal.Models
{
    /// <summary>
    /// Class for managing Azure Batch resources.
    /// </summary>
    public static class AzureBatchManager
    {

        /// <summary>
        /// Closes (deletes) a specified Azure Batch pool.
        /// </summary>
        /// <param name="resourceGroupName">The name of the Azure resource group.</param>
        /// <param name="accountName">The name of the Azure Batch account.</param>
        /// <param name="poolName">The name of the Azure Batch pool to delete.</param>
        public static async Task CloseBatchPoolAsync(string poolName)
        {
            try
            {
                using BatchClient _batchClient = GetBatchClient();

                if (_batchClient == null)
                    throw new InvalidOperationException("Unable to get a Azure batch client while closing a pool.");

                CloudPool pool = _batchClient.PoolOperations.ListPools().Where(p => p.Id == poolName).FirstOrDefault();
                if (pool == null)
                    throw new InvalidOperationException($"Unable to find pool {poolName} while closing a pool.");
                await _batchClient.PoolOperations.DeletePoolAsync(pool.Id);
                Console.WriteLine($"Successfully deleted pool {poolName}.");
            }
            catch (BatchException be)
            {
                Console.WriteLine($"Azure Batch pool with pool id {poolName} failed: " + be.ToString());
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Azure Batch pool with pool id {poolName} failed: " + e.ToString());
                throw;
            }
        }

        /// <summary>
        /// Get a BatchClient object to interact with Azure Batch service.
        /// </summary>
        /// <returns>A <c>BatchClient</c> object for interacting with Azure Batch service.</returns>
        private static BatchClient GetBatchClient()
        {
            // Ensure the necessary environment variables exist.
            string accountUrl = Environment.GetEnvironmentVariable("AZURE_BATCH_ACCOUNT_URL");
            if (string.IsNullOrEmpty(accountUrl))
                throw new Exception("Cannot find variable AZURE_BATCH_ACCOUNT_URL");

            string accountName = Environment.GetEnvironmentVariable("AZURE_BATCH_ACCOUNT_NAME");
            if (string.IsNullOrEmpty(accountName))
                throw new Exception("Cannot find variable AZURE_BATCH_ACCOUNT_NAME");

            string primaryAccessKey = Environment.GetEnvironmentVariable("AZURE_BATCH_ACCOUNT_KEY");
            if (string.IsNullOrEmpty(primaryAccessKey))
                throw new Exception("Cannot find variable AZURE_BATCH_ACCOUNT_KEY");

            return BatchClient.Open(new BatchSharedKeyCredentials(
                accountUrl,
                accountName,
                primaryAccessKey));
        }
    }
}