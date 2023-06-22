using IntelligentWmsIntegration.Models.ServiceLayer;
using System.Collections.Generic;

namespace IntelligentWmsIntegration.Models.ServiceLayer
{
    internal class ArInvoice : Document
    {
        public string U_WMSRef { get; set; }
        public string U_OrderType { get; set; }
        public List<DocumentAdditionalExpense> DocumentAdditionalExpenses { get; set; }
    }

    internal class ArInvoiceLine : DocumentLine
    {
    }
}
