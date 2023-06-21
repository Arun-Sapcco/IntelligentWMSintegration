using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntelligentWMSintegration.Models
{
    internal class SapItem
    {
        public string ItemCode { get; set; }
        public string WhsCode { get; set; }
        public double OnHand { get; set; }
    }
}
