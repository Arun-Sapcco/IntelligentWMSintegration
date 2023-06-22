using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntelligentWmsIntegration.Models
{
    public class WMSReturnToCustomerInvoice
    {
        public string CompanyCode { get; set; }
        public string WMSRef { get; set; }
        public string TargetDocument { get; set; }
        public string CustomerCode { get; set; }
        public string CustomerName { get; set; }
        public int BaseDocumentNumber { get; set; }
        public int BaseDocumentEntry { get; set; }
        public DateTime? PostingDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string ReferenceNumber { get; set; }
        public string ShippingType { get; set; }
        public string ShipToCode { get; set; }
        public string ShipToAddress { get; set; }
        public string SalesExecutive { get; set; }
        public int BaseItemRowLineNum { get; set; }
        public string ItemCode { get; set; }
        public decimal? Quantity { get; set; }
        public string UOM { get; set; }
        public double InventoryQuantity { get; set; }
        public string InventoryUoM { get; set; }
        public decimal? Price { get; set; }
        public string Warehouse { get; set; }
        public string Text { get; set; }
        public string ReturnReason { get; set; }
        public string Remarks { get; set; }
        public DateTime? LastUpdateDateTime { get; set; }
        public string IsProcessed { get; set; }
        public int? Close { get; set; }
    }

}
