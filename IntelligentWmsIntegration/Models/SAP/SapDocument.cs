using System;

namespace IntelligentWmsIntegration.Models
{
    public class SapDocument
    {
        public string Confirmed { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public int DocNum { get; set; }
        public int DocEntry { get; set; }
        public DateTime DocDate { get; set; }
        public DateTime DocDueDate { get; set; }
        public string NumAtCard { get; set; }
        public string DocStatus { get; set; }
        public int TrnspCode { get; set; }
        public string ShipToCode { get; set; }
        public string Address2 { get; set; }
        public string PickRmrk { get; set; }
        public string SlpName { get; set; }
        public int CntctCode { get; set; }
        public int OwnerCode { get; set; }
        public string PayToCode { get; set; }
        public string Comments { get; set; }
        public string DocCur { get; set; }
        public double DocRate { get; set; }
        public int SlpCode { get; set; }
    }
}
