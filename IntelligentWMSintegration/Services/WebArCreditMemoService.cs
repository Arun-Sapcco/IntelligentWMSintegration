using B1SLayer;
using IntelligentWmsIntegration.DAL;
using IntelligentWmsIntegration.Helpers;
using IntelligentWmsIntegration.Models;
using IntelligentWmsIntegration.Models.ServiceLayer;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Threading.Tasks;

namespace IntelligentWmsIntegration.Services
{
    public class WebArCreditMemoService
    {
        public async static Task Import()
        {
            Logger.WriteLog($"A/R Credit Memo Import starts.");
            foreach (var Company in AppConfig.CompanyList)
            {
                try
                {
                    var companyName = Company.CompanyDB;
                    Logger.WriteLog($"Company Name: {companyName}");

                    string query = "SELECT distinct T0.\"DocNum\", T1.\"DocEntry\" FROM ORRR T0  " +
                                   "INNER JOIN RRR1 T1 ON T0.\"DocEntry\" = T1.\"DocEntry\" " +
                                   "WHERE T1.\"WhsCode\" ='PU' and T0.\"DocStatus\"='O'";

                    string HanaConnectionString = Company.HanaConnectionString;
                    HanaDataAccessLayer HanaDataAccessLayer = new HanaDataAccessLayer(HanaConnectionString);
                    List<SapArCreditMemo> sapList = await HanaDataAccessLayer.ExecuteQueryAsync<List<SapArCreditMemo>>(query);

                    query = "select * from [dbo].[WMSReturnToCustomerInvoice] "
                         + $"where isprocessed = 'Y' and CompanyCode = '{Company.CompanyDB.Split('_').First()}' "
                         + "and BaseDocumentNumber IS NOT NULL and BaseDocumentEntry IS NOT NULL and BaseItemRowLineNum IS NOT NULL "
                         + $"and Warehouse = 'PU' AND InventoryQuantity > 0 and CompanyCode = 'PU' "
                    //+ $"and BaseDocumentEntry ='27322'"
                    + $"and BaseDocumentNumber ='1000138' "
                    + " and text = 'N'";

                    string wmsConnectionString = AppConfig.WmsConnectionString;
                    SqlDataAccessLayer dac = new SqlDataAccessLayer(wmsConnectionString);
                    List<WmsReturnToCustomerInvoice> wmsList = await dac.ExecuteQueryAsync<List<WmsReturnToCustomerInvoice>>(query);

                    var joinedList = from sap in sapList
                                     join wms in wmsList
                                     on sap.DocEntry equals wms.BaseDocumentEntry
                                     select wms;

                    var groupedWebArCreditMemo = joinedList.GroupBy(x => x.BaseDocumentEntry);

                    foreach (var group in groupedWebArCreditMemo)
                    {
                        try
                        {
                            int baseDocEntry = group.Key;
                            int baseDocNum = group.FirstOrDefault().BaseDocumentNumber;

                            Logger.WriteLog($"Processing Return Request with DocNum: {baseDocNum}");

                            query = $"SELECT T0.\"CardCode\", T0.\"CardName\", T0.\"CntctCode\", T0.\"DocCur\",T0.\"DocRate\",T0.\"OwnerCode\", T0.\"SlpCode\", T0.\"NumAtCard\", T0.\"U_WMSRef\", T0.\"U_OrderType\", T0.\"U_DelFrom\", T0.\"U_CompanyCode\", T0.\"DocDate\",T0.\"ShipToCode\",T0.\"PayToCode\",T0.\"TrnspCode\",T0.\"Comments\", T1.\"BaseType\" " +
                                    $"FROM ORRR T0  " +
                                    $"INNER JOIN RRR1 T1 ON T0.\"DocEntry\" = T1.\"DocEntry\" " +
                                    $"WHERE T0.\"DocStatus\" ='O' and  T1.\"LineStatus\" ='O' " +
                                    $"and  T1.\"WhsCode\" ='PU'  and T0.\"DocEntry\" = {baseDocEntry}";

                            List<SapArCreditMemo> sapHeaderList = await HanaDataAccessLayer.ExecuteQueryAsync<List<SapArCreditMemo>>(query);
                            SapArCreditMemo sapHeader = sapHeaderList.FirstOrDefault();
                            if (sapHeader == null)
                                throw new Exception($"No records exist for Base DocNum: {baseDocNum}");

                            int Delivery = 15;
                            int ArInvoice = 13;
                            var ReturnRequest = 234000031;
                            var StandAlone = -1;

                            // Base document is Delivery
                            if (sapHeader.BaseType == Delivery)
                            {
                                try
                                {
                                    // Returns
                                    Return model = new Return()
                                    {
                                        CardCode = sapHeader.CardCode,
                                        DocumentsOwner = sapHeader.OwnerCode,
                                        ShipToCode = sapHeader.ShipToCode,
                                        SalesPersonCode = sapHeader.SlpCode,
                                        NumAtCard = sapHeader.NumAtCard,
                                        DocDate = sapHeader.DocDate.ToString("yyyy-MM-dd"),
                                        PayToCode = sapHeader.PayToCode,
                                        TransportationCode = sapHeader.TrnspCode,
                                        Comments = sapHeader.Comments,
                                        DocCurrency = sapHeader.DocCur,
                                        DocRate = sapHeader.DocRate
                                    };

                                    if (sapHeader.CntctCode == 0)
                                        model.ContactPersonCode = sapHeader.CntctCode;

                                    if (!string.IsNullOrEmpty(sapHeader.U_WMSRef))
                                        model.U_WMSRef = sapHeader.U_WMSRef;

                                    model.U_OrderType = sapHeader.U_OrderType;
                                    int lineNum = 0;

                                    foreach (var deliveryConfirmations in group)
                                    {
                                        query = $"SELECT T0.\"DocEntry\", T0.\"BaseLine\",T0.\"LineStatus\",  T0.\"ItemCode\",T0.\"WhsCode\", T0.\"Quantity\", T0.\"Currency\", T0.\"Rate\", T0.\"Price\", T0.\"VatGroup\", T0.\"BaseType\" FROM RRR1 T0 WHERE T0.\"LineStatus\" ='O' and  T0.\"DocEntry\" ='{baseDocEntry}' and T0.\"LineNum\" = {deliveryConfirmations.BaseItemRowLineNum}";
                                        List<SapArCreditMemoLine> sapLineList = await HanaDataAccessLayer.ExecuteQueryAsync<List<SapArCreditMemoLine>>(query);
                                        SapArCreditMemoLine sapLine = sapLineList.FirstOrDefault();
                                        if (sapLine == null)
                                            throw new Exception($"No line is found for Base DocNum: {baseDocNum}");

                                        ReturnLine arLine = new ReturnLine()
                                        {
                                            LineNum = lineNum,
                                            ItemCode = sapLine.ItemCode,
                                            Quantity = deliveryConfirmations.InventoryQuantity,
                                            WarehouseCode = sapLine.WhsCode,
                                            UnitPrice = sapLine.Price,
                                            VatGroup = sapLine.VatGroup,
                                            BaseType = sapHeader.BaseType,
                                            BaseEntry = baseDocEntry,
                                            BaseLine = deliveryConfirmations.BaseItemRowLineNum
                                        };
                                        model.DocumentLines.Add(arLine);
                                        lineNum++;
                                    }

                                    query = $"SELECT T0.\"DocEntry\", T0.\"VatGroup\", T0.\"ExpnsCode\" \"ExpenseCode\", T0.\"LineTotal\" FROM RRR3 T0 WHERE T0.\"DocEntry\" ={baseDocEntry} and  T0.\"LineTotal\" > 0";
                                    List<DocumentAdditionalExpense> expenseList = HanaDataAccessLayer.ExecuteQuery<List<DocumentAdditionalExpense>>(query);
                                    foreach (var expense in expenseList)
                                    {
                                        DocumentAdditionalExpense obj = new DocumentAdditionalExpense()
                                        {
                                            ExpenseCode = Convert.ToInt32(expense.ExpenseCode),
                                            VatGroup = expense.VatGroup.ToString(),
                                            LineTotal = Convert.ToDouble(expense.LineTotal)
                                        };
                                        model.DocumentAdditionalExpenses.Add(obj);
                                    }

                                    var serviceLayer = new SLConnection(AppConfig.ServiceLayerUrl, companyName, Company.UserName, Company.Password);
                                    var response = await serviceLayer
                                                        .Request("Returns")
                                                        .PostAsync<Return>(model);

                                    Logger.WriteLog($"Return created successfully.");

                                    query = "UPDATE [dbo].[WMSReturnToCustomerInvoice] "
                                          + "SET Text = 'Y' "
                                          + $"where isprocessed = 'Y' and CompanyCode = '{Company.CompanyDB.Split('_').First()}' "
                                          + "and BaseItemRowLineNum IS NOT NULL "
                                          + $"and Warehouse = 'Web' AND InventoryQuantity > 0 and CompanyCode = 'PU' "
                                          + $"and BaseDocumentEntry ='{baseDocEntry}'"
                                          + $"and BaseDocumentNumber ='{baseDocNum}'";
                                    dac.ExecuteNonQuery(query);
                                }
                                catch (Exception ex)
                                {
                                    Logger.WriteLog($"Exception occurs while creating Return due to {ex.Message}.");
                                }
                            }
                            else if (sapHeader.BaseType == ArInvoice)
                            {
                                try
                                {
                                    // AR Credit Memo
                                    Logger.WriteLog($"Starts Creating AR Credit Memo");
                                    ArCreditMemo model = new ArCreditMemo()
                                    {
                                        CardCode = sapHeader.CardCode,
                                        DocumentsOwner = sapHeader.OwnerCode,
                                        ShipToCode = sapHeader.ShipToCode,
                                        SalesPersonCode = sapHeader.SlpCode,
                                        NumAtCard = sapHeader.NumAtCard,
                                        DocDate = sapHeader.DocDate.ToString("yyyy-MM-dd"),
                                        PayToCode = sapHeader.PayToCode,
                                        TransportationCode = sapHeader.TrnspCode,
                                        Comments = sapHeader.Comments,
                                        DocCurrency = sapHeader.DocCur,
                                        DocRate = sapHeader.DocRate
                                    };

                                    if (sapHeader.CntctCode == 0)
                                        model.ContactPersonCode = sapHeader.CntctCode;

                                    if (!string.IsNullOrEmpty(sapHeader.U_WMSRef))
                                        model.U_WMSRef = sapHeader.U_WMSRef;

                                    model.U_OrderType = sapHeader.U_OrderType;
                                    int lineNum = 0;

                                    foreach (var deliveryConfirmations in group)
                                    {
                                        query = $"SELECT T0.\"DocEntry\", T0.\"BaseLine\",T0.\"LineStatus\",  T0.\"ItemCode\",T0.\"WhsCode\", T0.\"Quantity\", T0.\"Currency\", T0.\"Rate\", T0.\"Price\", T0.\"VatGroup\", T0.\"BaseType\" FROM RRR1 T0 WHERE T0.\"LineStatus\" ='O' and  T0.\"DocEntry\" ='{baseDocEntry}' and T0.\"LineNum\" = {deliveryConfirmations.BaseItemRowLineNum}";
                                        List<SapArCreditMemoLine> sapLineList = await HanaDataAccessLayer.ExecuteQueryAsync<List<SapArCreditMemoLine>>(query);
                                        SapArCreditMemoLine sapLine = sapLineList.FirstOrDefault();
                                        if (sapLine == null)
                                            throw new Exception($"No line is found for Base DocNum: {baseDocNum}");

                                        ArCreditMemoLine arLine = new ArCreditMemoLine()
                                        {
                                            LineNum = lineNum,
                                            ItemCode = sapLine.ItemCode,
                                            Quantity = deliveryConfirmations.InventoryQuantity,
                                            WarehouseCode = sapLine.WhsCode,
                                            UnitPrice = sapLine.Price,
                                            VatGroup = sapLine.VatGroup,

                                            BaseType = ReturnRequest,  // sapLine.BaseType,
                                            BaseEntry = deliveryConfirmations.BaseDocumentEntry,
                                            BaseLine = deliveryConfirmations.BaseItemRowLineNum
                                        };
                                        model.DocumentLines.Add(arLine);
                                        lineNum++;
                                    }

                                    query = $"SELECT T0.\"DocEntry\", T0.\"VatGroup\", T0.\"ExpnsCode\" \"ExpenseCode\", T0.\"LineTotal\" FROM RRR3 T0 WHERE T0.\"DocEntry\" ={baseDocEntry} and  T0.\"LineTotal\" > 0";
                                    List<DocumentAdditionalExpense> expenseList = HanaDataAccessLayer.ExecuteQuery<List<DocumentAdditionalExpense>>(query);
                                    foreach (var expense in expenseList)
                                    {
                                        DocumentAdditionalExpense obj = new DocumentAdditionalExpense()
                                        {
                                            ExpenseCode = Convert.ToInt32(expense.ExpenseCode),
                                            VatGroup = expense.VatGroup.ToString(),
                                            LineTotal = Convert.ToDouble(expense.LineTotal)
                                        };
                                        model.DocumentAdditionalExpenses.Add(obj);
                                    }

                                    string json = JsonConvert.SerializeObject(model, Formatting.Indented);

                                    // Posting A/R Credit Memo
                                    var serviceLayer = new SLConnection(AppConfig.ServiceLayerUrl, companyName, Company.UserName, Company.Password);
                                    var response = await serviceLayer
                                                        .Request("CreditNotes")
                                                        .PostAsync<ArCreditMemo>(model);

                                    Logger.WriteLog($"A/R Credit Memo Created successfully.");

                                    query = "UPDATE [dbo].[WMSReturnToCustomerInvoice] "
                                         + "SET Text = 'Y' "
                                         + $"where isprocessed = 'Y' and CompanyCode = '{Company.CompanyDB.Split('_').First()}' "
                                         + "and BaseItemRowLineNum IS NOT NULL "
                                         + $"and Warehouse = 'Web' AND InventoryQuantity > 0 and CompanyCode = 'PU' "
                                         + $"and BaseDocumentEntry ='{baseDocEntry}'"
                                         + $"and BaseDocumentNumber ='{baseDocNum}'";
                                    dac.ExecuteNonQuery(query);

                                }
                                catch (Exception ex)
                                {
                                    Logger.WriteLog($"Exception occurs while creating A/R Credit Memo due to {ex.Message}");
                                }
                            }
                            else if (sapHeader.BaseType == StandAlone)
                            {
                                try
                                {
                                    // AR Credit Memo
                                    Logger.WriteLog($"Starts Creating AR Credit Memo");
                                    ArCreditMemo model = new ArCreditMemo()
                                    {
                                        CardCode = sapHeader.CardCode,
                                        DocumentsOwner = sapHeader.OwnerCode,
                                        ShipToCode = sapHeader.ShipToCode,
                                        SalesPersonCode = sapHeader.SlpCode,
                                        NumAtCard = sapHeader.NumAtCard,
                                        DocDate = sapHeader.DocDate.ToString("yyyy-MM-dd"),
                                        PayToCode = sapHeader.PayToCode,
                                        TransportationCode = sapHeader.TrnspCode,
                                        Comments = sapHeader.Comments,
                                        DocCurrency = sapHeader.DocCur,
                                        DocRate = sapHeader.DocRate
                                    };

                                    if (sapHeader.CntctCode == 0)
                                        model.ContactPersonCode = sapHeader.CntctCode;

                                    if (!string.IsNullOrEmpty(sapHeader.U_WMSRef))
                                        model.U_WMSRef = sapHeader.U_WMSRef;

                                    model.U_OrderType = sapHeader.U_OrderType;
                                    int lineNum = 0;

                                    foreach (var deliveryConfirmations in group)
                                    {
                                        query = $"SELECT T0.\"DocEntry\", T0.\"BaseLine\",T0.\"LineStatus\",  T0.\"ItemCode\",T0.\"WhsCode\", T0.\"Quantity\", T0.\"Currency\", T0.\"Rate\", T0.\"Price\", T0.\"VatGroup\", T0.\"BaseType\" FROM RRR1 T0 WHERE T0.\"LineStatus\" ='O' and  T0.\"DocEntry\" ='{baseDocEntry}' and T0.\"LineNum\" = {deliveryConfirmations.BaseItemRowLineNum}";
                                        List<SapArCreditMemoLine> sapLineList = await HanaDataAccessLayer.ExecuteQueryAsync<List<SapArCreditMemoLine>>(query);
                                        SapArCreditMemoLine sapLine = sapLineList.FirstOrDefault();
                                        if (sapLine == null)
                                            throw new Exception($"No line is found for Base DocNum: {baseDocNum}");

                                        ArCreditMemoLine arLine = new ArCreditMemoLine()
                                        {
                                            LineNum = lineNum,
                                            ItemCode = sapLine.ItemCode,
                                            Quantity = deliveryConfirmations.InventoryQuantity,
                                            WarehouseCode = sapLine.WhsCode,
                                            UnitPrice = sapLine.Price,
                                            VatGroup = sapLine.VatGroup,

                                            BaseType = ReturnRequest,  // sapLine.BaseType,
                                            BaseEntry = deliveryConfirmations.BaseDocumentEntry,
                                            BaseLine = deliveryConfirmations.BaseItemRowLineNum
                                        };
                                        model.DocumentLines.Add(arLine);
                                        lineNum++;
                                    }

                                    query = $"SELECT T0.\"DocEntry\", T0.\"VatGroup\", T0.\"ExpnsCode\" \"ExpenseCode\", T0.\"LineTotal\" FROM RRR3 T0 WHERE T0.\"DocEntry\" ={baseDocEntry} and  T0.\"LineTotal\" > 0";
                                    List<DocumentAdditionalExpense> expenseList = HanaDataAccessLayer.ExecuteQuery<List<DocumentAdditionalExpense>>(query);
                                    foreach (var expense in expenseList)
                                    {
                                        DocumentAdditionalExpense obj = new DocumentAdditionalExpense()
                                        {
                                            ExpenseCode = Convert.ToInt32(expense.ExpenseCode),
                                            VatGroup = expense.VatGroup.ToString(),
                                            LineTotal = Convert.ToDouble(expense.LineTotal)
                                        };
                                        model.DocumentAdditionalExpenses.Add(obj);
                                    }

                                    string json = JsonConvert.SerializeObject(model, Formatting.Indented);

                                    // Posting A/R Credit Memo
                                    var serviceLayer = new SLConnection(AppConfig.ServiceLayerUrl, companyName, Company.UserName, Company.Password);
                                    var response = await serviceLayer
                                                        .Request("CreditNotes")
                                                        .PostAsync<ArCreditMemo>(model);

                                    Logger.WriteLog($"A/R Credit Memo Created successfully.");

                                    query = "UPDATE [dbo].[WMSReturnToCustomerInvoice] "
                                         + "SET Text = 'Y' "
                                         + $"where isprocessed = 'Y' and CompanyCode = '{Company.CompanyDB.Split('_').First()}' "
                                         + "and BaseItemRowLineNum IS NOT NULL "
                                         + $"and Warehouse = 'PU' AND InventoryQuantity > 0 and CompanyCode = 'PU' "
                                         + $"and BaseDocumentEntry ='{baseDocEntry}'"
                                         + $"and BaseDocumentNumber ='{baseDocNum}'";
                                    dac.ExecuteNonQuery(query);

                                }
                                catch (Exception ex)
                                {
                                    Logger.WriteLog($"Exception occurs while creating A/R Credit Memo due to {ex.Message}");
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            Logger.WriteLog($"Exception occurs due to {ex.Message}");
                            continue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLog($"Exception: {ex.Message}");
                }
            }
            Logger.WriteLog($"A/R Credit Memo Import ends.");
        }
    }
}
