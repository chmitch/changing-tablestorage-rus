using Microsoft.Azure;
using Microsoft.Azure.CosmosDB.Table;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System.Linq;
using System;
using System.Threading.Tasks;
using TableStorage.Model;

namespace TableStorage
{
    public class BasicSamples
    {
        public async Task RunSamples()
        {
            Console.WriteLine("Azure Table Storage - Basic Samples\n");
            Console.WriteLine();
            string accountName = CloudConfigurationManager.GetSetting("CosmosAccount");
            string accountKey = CloudConfigurationManager.GetSetting("CosmosKey");

            string tableName = "demo" + Guid.NewGuid().ToString().Substring(0, 5);

            // Create or reference an existing table
            CloudTable table = await Common.CreateTableAsync(tableName, accountName, accountKey);

            await UpdateCosmosTablesRUs(accountName, accountKey, tableName, 1000);
            try
            {

                // Demonstrate basic CRUD functionality 
                await BasicDataOperationsAsync(table);

            }
            finally
            {
                // Delete the table
                //await table.DeleteIfExistsAsync();
                await UpdateCosmosTablesRUs(accountName, accountKey, tableName, 400);
            }
        }

        /// <summary>
        /// Updates cosmos table RU's. 
        /// Note: Assumes that table account is in public cloud 
        /// </summary>
        /// <param name="accountName">Table account name with out DNS suffix</param>
        /// <param name="primaryKey">Table account primary key</param>
        /// <param name="tableName">Name of table</param>
        /// <param name="targetRUs">Target RU's</param>
        /// <returns></returns>
        static async Task UpdateCosmosTablesRUs(string accountName, string primaryKey, string tableName, int targetRUs)
        {
            string endPoint = string.Format("https://{0}.documents.azure.com:443/", accountName);
            string dbName = "TablesDB";

            DocumentClient dc = new DocumentClient(new Uri(endPoint), primaryKey);
            var collectionUri = UriFactory.CreateDocumentCollectionUri(dbName, tableName);
            var collection = await dc.ReadDocumentCollectionAsync(collectionUri);

            // Fetch the resource to be updated
            Offer offer = dc.CreateOfferQuery()
                            .Where(r => r.ResourceLink == collection.Resource.SelfLink)
                            .AsEnumerable()
                            .SingleOrDefault();

            offer = new OfferV2(offer, targetRUs);
            await dc.ReplaceOfferAsync(offer);
        }


        /// <summary>
        /// Demonstrate basic Table CRUD operations.
        /// </summary>
        /// <param name="table">The sample table</param>
        /// <returns>A Task object</returns>
        private static async Task BasicDataOperationsAsync(CloudTable table)
        {
            // Create an instance of a customer entity. See the Model\CustomerEntity.cs for a description of the entity.
            CustomerEntity customer = new CustomerEntity("Harp", "Walter")
            {
                Email = "Walter@contoso.com",
                PhoneNumber = "425-555-0101"
            };

            // Demonstrate how to insert the entity
            Console.WriteLine("Insert an Entity.");
            customer = await SamplesUtils.InsertOrMergeEntityAsync(table, customer);

            // Demonstrate how to Update the entity by changing the phone number
            Console.WriteLine("Update an existing Entity using the InsertOrMerge Upsert Operation.");
            customer.PhoneNumber = "425-555-0105";
            await SamplesUtils.InsertOrMergeEntityAsync(table, customer);
            Console.WriteLine();

            // Demonstrate how to Read the updated entity using a point query 
            Console.WriteLine("Reading the updated Entity.");
            customer = await SamplesUtils.RetrieveEntityUsingPointQueryAsync(table, "Harp", "Walter");
            Console.WriteLine();

            // Demonstrate how to Delete an entity
            Console.WriteLine("Delete the entity. ");
            await SamplesUtils.DeleteEntityAsync(table, customer);
            Console.WriteLine();
        }
    }
}