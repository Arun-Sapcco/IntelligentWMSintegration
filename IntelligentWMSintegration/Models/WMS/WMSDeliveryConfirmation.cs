using System;

namespace IntelligentWmsIntegration.Models
{
    public class WMSDeliveryConfirmation
    {
        public string CompanyCode { get; set; }
        public string OrderType { get; set; }
        public int? TargetDocument { get; set; }
        public string WMSRef { get; set; }
        public string CustomerCode { get; set; }
        public string CustomerName { get; set; }
        public int BaseDocumentNumber { get; set; }
        public int BaseDocumentEntry { get; set; }
        public DateTime? PostingDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string ReferenceNumber { get; set; }
        public string DeliveryFrom { get; set; }
        public string ShippingType { get; set; }
        public string ShipToCode { get; set; }
        public string ShipToAddress { get; set; }
        public string PickAndPackRemark { get; set; }
        public string SalesExecutive { get; set; }
        public int? Priority { get; set; }
        public int BaseItemRowLineNum { get; set; }
        public string ItemCode { get; set; }
        public double? Quantity { get; set; }
        public string UOM { get; set; }
        public double InventoryQuantity { get; set; }
        public string InventoryUoM { get; set; }
        public decimal? Price { get; set; }
        public string Warehouse { get; set; }
        public string Batch { get; set; }
        public string Chemicals { get; set; }
        public string CountryOfOrigin { get; set; }
        public int? Coded { get; set; }
        public string Quality { get; set; }
        public string Barcode { get; set; }
        public int? WEB { get; set; }
        public string Ownership { get; set; }
        public string Text { get; set; }
        public int? CloseRow { get; set; }
        public string DocumentRemark { get; set; }
        public DateTime? LastUpdateDateTime { get; set; }
        public string IsProcessed { get; set; }
        public int? Close { get; set; }

        public string WhsCode { get; set; }
    }

}
