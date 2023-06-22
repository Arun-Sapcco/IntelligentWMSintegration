using IntelligentWmsIntegration.Services;

namespace IntelligentWmsIntegration
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //WmsIntegrationService integration = new WmsIntegrationService();
            //integration.Process();

            SalesOrderService.Export();
            WebArInvoiceService.Import();
            SalesReturnRequestService.Export();
            WebArCreditMemoService.Import();
        }
    }
}
