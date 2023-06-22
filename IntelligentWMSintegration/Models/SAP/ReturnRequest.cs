using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntelligentWmsIntegration.Models
{
    public class ReturnRequest
    {
        public string U_CompanyCode { get; set; }

        public string BaseType { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string DocNum { get; set; }
        public int DocEntry { get; set; }
        public DateTime DocDate { get; set; }
        public DateTime DocDueDate { get; set; }
        public string NumAtCard { get; set; }
        public string TrnspCode { get; set; }
        public string ShipToCode { get; set; }
        public string Address2 { get; set; }
        public string SlpName { get; set; }
        public DateTime Currentdatetime { get; set; }
        public string U_IsProcessed { get; set; }
        public string Comments { get; set; }
    }
}
