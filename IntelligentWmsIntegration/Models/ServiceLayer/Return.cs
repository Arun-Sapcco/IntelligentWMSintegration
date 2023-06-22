namespace IntelligentWmsIntegration.Models.ServiceLayer
{
    internal class Return: Document
    {
        public string U_WMSRef { get; set; }
        public string U_OrderType { get; set; }
    }
    internal class ReturnLine: DocumentLine
    {
    }
}
