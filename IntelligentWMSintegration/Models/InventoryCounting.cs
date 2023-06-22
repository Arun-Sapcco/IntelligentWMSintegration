using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntelligentWmsIntegration.Models
{
    internal class InventoryCounting
    {
        public String CountDate { get; set; }
        public string CountTime { get; set; }
        //public string Remarks { get; set; }
        public string Reference2 { get; set; }
        public List<InventoryCountingLine> InventoryCountingLines { get; set; }
    }

    public class InventoryCountingLine
    {
        public string ItemCode { get; set; }
        public string WarehouseCode { get; set; }
        public double CountedQuantity { get; set; }
        //public string Remarks { get; set; }
        public string Counted { get; set; }
    }
}
