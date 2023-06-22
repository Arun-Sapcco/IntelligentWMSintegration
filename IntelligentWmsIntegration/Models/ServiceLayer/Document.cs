using System;
using System.Collections.Generic;

namespace IntelligentWmsIntegration.Models.ServiceLayer
{
    internal class Document
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public DateTime DocDate { get; set; }
        public DateTime DocDueDate { get; set; }
        public string CardCode { get; set; }
        public string NumAtCard { get; set; }
        public string DocCurrency { get; set; }
        public double DocRate { get; set; }
        public string Comments { get; set; }
        public int SalesPersonCode { get; set; }
        public int ContactPersonCode { get; set; }
        public DateTime TaxDate { get; set; }
        public string ShipToCode { get; set; }
        public int DocumentsOwner { get; set; }
        public string PayToCode { get; set; }
        public int TransportationCode { get; set; }

        public List<DocumentLine> DocumentLines { get; set; }

    }

}
