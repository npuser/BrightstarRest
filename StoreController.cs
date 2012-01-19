using System.Collections.Generic;
using System.Threading;
using System.Web.Mvc;
using System.Xml.Linq;
using NetworkedPlanet.Brightstar.Client;

namespace BrightstarRest.Models.Controllers
{
    public class StoreController : Controller
    {
        private IBrightstarService _context;
        private IBrightstarService Context
        {
            get
            {                
                return _context ??
                        (_context = BrightstarService.GetEmbeddedClient("C:\\Projects\\node.js\\BrightstarRest\\BSData"));
            }
        }

        //
        // GET: /Store/        
        public ActionResult Index()
        {
            // List all of the stores
            return new JsonResult()
                       {
                           Data = Context.ListStores(),
                           JsonRequestBehavior = JsonRequestBehavior.AllowGet
                       };
        }

        // GET: /Store/{storeName}?query=
        [HttpGet, ValidateInput(false)]
        public ActionResult Store(string storeName, string query)
        {            
            if (!string.IsNullOrWhiteSpace(query))
            {
                if (Context.DoesStoreExist(storeName))
                {
                    var queryResult = Context.ExecuteQuery(storeName, query);

                    var results = XDocument.Load(queryResult);
                    var triples = new List<Dictionary<string, string>>();
                    foreach (var result in results.SparqlResultRows())
                    {
                        var columnValues = new Dictionary<string, string>();
                        foreach (string name in results.GetVariableNames())
                        {
                            columnValues.Add(name, result.GetColumnValue(name).ToString());
                        }
                        triples.Add(columnValues);
                    }
                    return new JsonResult()
                                {
                                    Data = triples,
                                    JsonRequestBehavior = JsonRequestBehavior.AllowGet
                                };
                }
            }
            return new JsonResult() { Data = Context.DoesStoreExist(storeName), JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        // POST: /Store/{storeName}?triples=
        [HttpPost, ValidateInput(false)]
        public ActionResult Insert(string storeName, string triples)
        {
            var jobInfo = Context.ExecuteTransaction(storeName, string.Empty, string.Empty, triples, true);
            while (!jobInfo.JobCompletedOk)
            {
                Thread.Sleep(30);
            }
            return new JsonResult() { Data = true };
        }
    }
}
