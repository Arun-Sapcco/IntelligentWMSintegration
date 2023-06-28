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
            //CompanyList.Add(new SAPCompany() { CompanyDB = "PU_LIVE_DB", UserName ="Intercompany", Password = "7891234", HanaConnectionString= "Server = saphana:30015; UserId = SYSTEM; Password = PeRf-Ume5_2o22; CS = PU_LIVE_DB" });

            CompanyList.Add(new SAPCompany() { CompanyDB = "PU_FA_TEST", UserName = "Intercompany", Password = "7891234", HanaConnectionString = "Server = saphana:30015; UserId = SYSTEM; Password = PeRf-Ume5_2o22; CS = PU_FA_TEST" });
            // CompanyList.Add(new SAPCompany() { CompanyDB = "SMELL_LIVE_DB", UserName ="Intercompany", Password = "7891234", HanaConnectionString= "Server = saphana:30015; UserId = SYSTEM; Password = PeRf-Ume5_2o22; CS = SMELL_LIVE_DB" });

            WmsConnectionString = "Data Source =WMS-SQL1; Initial Catalog = StagingWMSSAPTest; User id = SAP; Password = Saphana1.;";

        }

        public static string ServiceLayerUrl { get; set; }

        public static List<SAPCompany> CompanyList { get; set; }

        public static string WmsConnectionString { get; set; }

    }
}
