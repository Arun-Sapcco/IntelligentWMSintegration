using IntelligentWMSintegration.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntelligentWMSintegration
{
    internal class Program
    {
        static void Main(string[] args)
        {
            WmsIntegrationService integration = new WmsIntegrationService();
            integration.Process();
        }
    }
}
