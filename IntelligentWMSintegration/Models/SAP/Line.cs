namespace IntelligentWmsIntegration.Models
{
    public class Line
    {
        public int DocNum { get; set; }
        public int DocEntry { get; set; }
        public int LineNum { get; set; }
        public string ItemCode { get; set; }
        public string Dscription { get; set; }
        public string LineStatus { get; set; }
        public double Quantity { get; set; }
        public string unitMsr { get; set; }
        public string SalUnitMsr { get; set; }
        public string InvntryUom { get; set; }
        public double InvQty { get; set; }
        public double Price { get; set; }
        public string WhsCode { get; set; }
        public string Comments { get; set; }
        public string VatGroup { get; set; }
        public string BaseRef { get; set; }
        public int BaseType { get; set; }
        public string BaseEntry { get; set; }
        public string BaseLine { get; set; }

    }
}
