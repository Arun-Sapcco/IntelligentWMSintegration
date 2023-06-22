namespace IntelligentWmsIntegration.Models.ServiceLayer
{
    internal class ArCreditMemo : Document
    {
        public string U_WMSRef { get; set; }
        public string U_OrderType { get; set; }
    }

    internal class ArCreditMemoLine: DocumentLine
    {

    }
}
