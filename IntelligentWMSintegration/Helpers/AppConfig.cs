using IntelligentWmsIntegration.Models;
using System.Collections.Generic;

namespace IntelligentWmsIntegration.Helpers
{
    public class AppConfig
    {
        static AppConfig()
        {
            ServiceLayerUrl = "https://saphana:50000/b1s/v1/";
            CompanyList = new List<SAPCompany>();
            // Company List
            CompanyList.Add(new SAPCompany() { CompanyDB = "PU_LIVE_DB", UserName ="Intercompany", Password = "7891234", HanaConnectionString= "Server = SAPHANAPRIMARY:30015; UserId = SYSTEM; Password = Saphana3; CS = PU_TEST_DB" });
            CompanyList.Add(new SAPCompany() { CompanyDB = "FF_LIVE_DB", UserName ="Intercompany", Password = "7891234", HanaConnectionString= "Server = SAPHANAPRIMARY:30015; UserId = SYSTEM; Password = Saphana3; CS = FF_TEST_DB" });
            CompanyList.Add(new SAPCompany() { CompanyDB = "SMELL_LIVE_DB", UserName ="Intercompany", Password = "7891234", HanaConnectionString= "Server = SAPHANAPRIMARY:30015; UserId = SYSTEM; Password = Saphana3; CS = SMELL_TEST_DB" });

            WmsConnectionString = "Data Source =WMS-SQL1; Initial Catalog = StagingWMSSAP; User id = SAP; Password = Saphana1.;";

        }

        public static string ServiceLayerUrl { get; set; }

        public static List<SAPCompany> CompanyList { get; set; }

        public static string WmsConnectionString { get; set; }

    }
}
