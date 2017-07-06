using AlaskaAir.FlightOps.MobileDashboard.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Text;

using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Newtonsoft.Json;
using System.Collections;

namespace AlaskAir.MobileDashboard.Controllers
{
    public class AirlineStatusController : ApiController
    {

        // Authorization to connect to the dsodatabase
        // Delete these after every session, because it's authorization
        private const string endpoint = "https://dsodocumentdb.documents.azure.com:443/";
        private const string primaryKey = "zdztF757TrQ0walwRd8Sa1FKfHJD9ESY0ECqwbN3BLIhy4xum27zuDS9F2KKiloPGHUv4nuJPRHQEM6X48Puxg==";

        // Fields that need to be accessed globally
        private Database database;
        private DocumentCollection collection;
        private DocumentClient client;

        private ArrayList airlineStatusDocuments = new ArrayList();

        //[Route("AirlineStatus")]
        //public IEnumerable<AirlineStatus> GetAirlineStatuses()
        //{
        //    AirlineStatus[] airlineStatuses = null;

        //    try
        //    {
        //        //Use IOC container to instantiate AirlineMetricsDbProvider
        //        //Changing to SQL
        //        //using (IAirlineMetricsDbProvider airlineMetricsDbProvider = UnityConfig.GetConfiguredContainer().Resolve<IAirlineMetricsDbProvider>
        //        //    (
        //        //        new ParameterOverrides
        //        //            {
        //        //                { "applicationId", standardHeaders.ApplicationId },
        //        //                { "conversationId", standardHeaders.ConversationId }
        //        //            }
        //        //        )
        //        //    )
        //        //{
        //        //    //Actual call to db to retrieve data starts here..........
        //        //    airlineStatuses = airlineMetricsDbProvider.GetAllAirlineStatus();
        //        //}

        //    }
        //    catch { }

        //    return airlineStatuses;
        //}

        //[ActionName("AirlineStatus")]
        //public async IEnumerable<AirlineStatus> GetAirlineStatuses()
        //{
        //    var items = await AirlineStatusDocumentDBRepository<AirlineStatus>.GetItemsAsync(d => !d.Completed);
        //    return View(items);
        //}


        // Creates a new instance of the DocumentClient
        private async Task CreateDocumentClient()
        {
            try
            {
                client = new DocumentClient(new Uri(endpoint), primaryKey);
                database = client.CreateDatabaseQuery("SELECT * FROM c WHERE c.id = 'dsodocumentdb'").AsEnumerable().First();
                collection = client.CreateDocumentCollectionQuery(database.CollectionsLink,
                    "SELECT * FROM c WHERE c.id = 'airlineStatusContent'").AsEnumerable().First();
            }
            catch (Exception e)
            {
                Exception baseException = e.GetBaseException();
                Console.WriteLine("Error: {0}, Message: {1}", e.Message, baseException.Message);
            }
        }

        // Queries for documents with the given sql query and returns them in an ArrayList
        private async Task<ArrayList> QueryDocumentsWithPaging(String sql)
        {
            Console.WriteLine();
            Console.WriteLine("**** Querying Documents: " + sql + " ****");
            Console.WriteLine();

            var query = client.CreateDocumentQuery(collection.SelfLink, sql).AsDocumentQuery();
            var airlineStatusDocs = new ArrayList();

            while (query.HasMoreResults)
            {
                var documents = await query.ExecuteNextAsync();
                foreach (var document in documents)
                {
                    airlineStatusDocs.Add(document);
                }
            }
            return airlineStatusDocs;
        }

        // GET all the documents from the collection
        // This should return an AirlineStatus
        public async Task<IEnumerable> Get()
        {
            var finalList = new ArrayList();

            try
            {
                await CreateDocumentClient();
                var query = "select * from c";
                var results = await QueryDocumentsWithPaging(query);
                foreach (var item in results) {
                    finalList.Add(item);
                }
            }
            catch (Exception e)
            {
                Exception baseException = e.GetBaseException();
                Console.WriteLine("Error: {0}, Message: {1}", e.Message, baseException.Message);
            }
            Console.WriteLine("Data returned: " + finalList.ToArray());
            return finalList.ToArray();
        }
        
        // GET the most recently updated document of the given date in the format of yyyy-MM-dd (example: 2017-07-05)
        public async Task<IEnumerable> GetMostRecentDocument(String year, String month, String day)
        {
            String[] dateAttributes = {year, month, day};
            String formattedDate = String.Join("-", dateAttributes);
            var list = new ArrayList();

            try
            {
                await CreateDocumentClient();
                var query = "select * from c where c.date = '" + formattedDate + "' order by c._ts desc";
                var results = await QueryDocumentsWithPaging(query);

                // This is a little weird. We just need the first document but we need to return the result in a list
                // so we add the first element into a new list and return that
                for (int i = 0; i < 1; i++)
                {
                    list.Add(results[0]);
                }
            }
            catch (Exception e)
            {
                Exception baseException = e.GetBaseException();
                Console.WriteLine("Error: {0}, Message: {1}", e.Message, baseException.Message);
            }
            return list;
        }

        // POST a new document into the Azure database
        // User writes new manual data and it gets uploaded to the database
        public async Task Post([FromBody]dynamic jsonObject)
        {
            Console.WriteLine();
            Console.WriteLine("**** Insert New Document ****");
            Console.WriteLine();

            try
            {
                await CreateDocumentClient();

                String jsonString = JsonConvert.SerializeObject(jsonObject);

                dynamic results = JsonConvert.DeserializeObject<dynamic>(jsonString);
                results.date = DateTime.Now.ToString("yyyy-MM-dd");

                CreateDocument(results);
            }
            catch (Exception e)
            {
                Exception baseException = e.GetBaseException();
                Console.WriteLine("Error: {0}, Message: {1}", e.Message, baseException.Message);
            }
            
        }

        // Create document from the given object
        private async Task<Document> CreateDocument(object documentObject)
        {
            var result = await client.CreateDocumentAsync(collection.SelfLink, documentObject);
            var document = result.Resource;
            Console.WriteLine("Created new document: {0}\r\n{1}", document.Id, document);
            return result;
        }

        // Reading data from the document with the given query
        // Returns string representation of the JSON content
        private async Task<String> GetDocumentContent(string sql)
        {
            Console.WriteLine();
            Console.WriteLine("**** Gets the contents of a document ****");
            Console.WriteLine();

            var document = client.CreateDocumentQuery(collection.SelfLink, sql);
            String content = JsonConvert.SerializeObject(document);
            return content;
        }
    }
}
