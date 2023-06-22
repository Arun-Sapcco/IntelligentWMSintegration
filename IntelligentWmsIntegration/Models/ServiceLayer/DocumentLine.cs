namespace IntelligentWmsIntegration.Models.ServiceLayer
{
    internal class DocumentLine
    {
        public int LineNum { get; set; }
        public string ItemCode { get; set; }
        public double Quantity { get; set; }
        public string WarehouseCode { get; set; }
        public int BaseType { get; set; }
        public int BaseEntry { get; set; }
        public int BaseLine { get; set; }
        public string TaxCode { get; set; }
        public double UnitPrice { get; set; }
    }
}
