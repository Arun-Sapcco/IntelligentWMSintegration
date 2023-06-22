using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntelligentWmsIntegration.Models
{
   public  class SAPReturnToCustomerDetails
    {
        public string DocNum { get; set; }
        public string DocEntry { get; set; }
        public string LineNum { get; set; }
        public string ItemCode { get; set; }
        public string Quantity { get; set; }
        public string UnitMsr { get; set; }
        public string InvQty { get; set; }
        public string InvntryUom { get; set; }
        public string Price { get; set; }
        public string WhsCode { get; set; }
        public string FreeTxt { get; set; }
        public string ReturnRsn { get; set; }
        public string BaseType { get; set; }
        public string BaseEntry { get; set; }
        public string BaseLine { get; set; }
        public string Comments { get; set; }
        public string U_IsProcessed { get; set; }
        public string LineStatus { get; set; }
        public string UniquePrimaryKey { get; set; }
        public string U_CompanyCode { get; set; }
    }
}
