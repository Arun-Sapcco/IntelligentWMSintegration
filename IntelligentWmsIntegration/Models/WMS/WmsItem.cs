using System;

namespace IntelligentWmsIntegration.Models
{
    public class WmsItem
    {
        public DateTime DocTime { get; set; }
        public string ItemCode { get; set; }
        public double CountedQuantity { get; set; }
        public string Warehouse { get; set; }
        public int ECommerce { get; set; }
    }

}
