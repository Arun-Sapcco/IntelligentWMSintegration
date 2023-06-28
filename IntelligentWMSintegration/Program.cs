using IntelligentWmsIntegration.Services;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace IntelligentWmsIntegration
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            List<Task> tasks = new List<Task>();

            //SalesOrderService.Export();

            //WebArInvoiceService.Import().Wait();

            //await SalesReturnRequestService.Export();

            WebArCreditMemoService.Import().Wait();

            stopwatch.Stop();
            var time = stopwatch.Elapsed;
        }
    }
}
