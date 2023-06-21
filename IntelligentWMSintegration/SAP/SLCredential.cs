using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntelligentWMSintegration.SAP
{
    public class SLCredential
    {
        public string Url { get; set; }
        public string CompanyDB { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public SLCredential()
        {
            Url = "https://saphana:50000/b1s/v1/";
            UserName = "Intercompany";
            Password = "7891234";


        }
    }
}
