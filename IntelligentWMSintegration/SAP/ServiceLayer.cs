using B1SLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntelligentWMSintegration.SAP
{
    public sealed class ServiceLayer
    {
        private static readonly SLConnection _serviceLayer = new SLConnection(
            "https://saphana:50000/b1s/v1/",
            "PU_LIVE_DB",
            "Intercompany",
            "7891234");

        static ServiceLayer() { }

        private ServiceLayer() { }

        public static SLConnection Connection => _serviceLayer;
    }
}
