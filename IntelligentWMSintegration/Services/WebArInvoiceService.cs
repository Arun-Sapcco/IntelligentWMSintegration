﻿using B1SLayer;
using IntelligentWmsIntegration.DAL;
using IntelligentWmsIntegration.Helpers;
using IntelligentWmsIntegration.Models;
using IntelligentWmsIntegration.Models.ServiceLayer;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IntelligentWmsIntegration.Services
{
    public class WebArInvoiceService
    {
        public async static Task Import()
        {
            Logger.WriteLog($"A/R Invoice Import starts.");
            foreach (var Company in AppConfig.CompanyList)
            {
                try
                {
                    var companyName = Company.CompanyDB;
                    Logger.WriteLog($"Company Name: {companyName}");

                    string query = "SELECT distinct T0.\"DocNum\", T1.\"DocEntry\" " +
                                   "FROM ORDR T0  " +
                                   "INNER JOIN RDR1 T1 ON T0.\"DocEntry\" = T1.\"DocEntry\" " +
                                   "WHERE T1.\"WhsCode\" ='Web' and T0.\"DocStatus\"='O'";

                    string HanaConnectionString = Company.HanaConnectionString;
                    HanaDataAccessLayer HanaDataAccessLayer = new HanaDataAccessLayer(HanaConnectionString);
                    List<SapSalesOrder> sapList = HanaDataAccessLayer.ExecuteQuery<List<SapSalesOrder>>(query);

                    query = "select * " +
                            "from [dbo].[WMSDeliveryConfirmation] "
                          + $"where isprocessed = 'Y' and CompanyCode = '{Company.CompanyDB.Split('_').First()}' "
                          + "and BaseDocumentNumber IS NOT NULL and BaseDocumentEntry IS NOT NULL and BaseItemRowLineNum IS NOT NULL "
                          + $"and Warehouse = 'Web' AND InventoryQuantity > 0 and CompanyCode = 'PU' "
                    //+ $"and BaseDocumentEntry ='47791'"
                    //+ $"and BaseDocumentNumber ='1036349'"
                    + $" AND Text = 'N'";


                    string wmsConnectionString = AppConfig.WmsConnectionString;
                    SqlDataAccessLayer dac = new SqlDataAccessLayer(wmsConnectionString);
                    List<WmsDeliveryConfirmation> wmsList = dac.ExecuteQuery<List<WmsDeliveryConfirmation>>(query);

                    var joinedList = from sap in sapList
                                     join wms in wmsList
                                     on sap.DocEntry equals wms.BaseDocumentEntry
                                     select wms;

                    var groupedDeliveryConfirmations = joinedList.GroupBy(x => x.BaseDocumentEntry);

                    //ServiceLayer
                    foreach (var group in groupedDeliveryConfirmations)
                    {
                        try
                        {
                            int baseDocEntry = group.Key;
                            int baseDocNum = group.FirstOrDefault().BaseDocumentNumber;

                            Logger.WriteLog($"Processing Delivery with Sales Order DocNum: {baseDocNum}");

                            query = $"SELECT T0.\"CardCode\", T0.\"CardName\", IFNULL(T0.\"CntctCode\",0), T0.\"DocCur\",T0.\"DocRate\",T0.\"OwnerCode\", T0.\"SlpCode\", T0.\"NumAtCard\", T0.\"U_WMSRef\", T0.\"U_OrderType\", T0.\"U_DelFrom\", T0.\"U_CompanyCode\", T0.\"DocDate\",T0.\"ShipToCode\",T0.\"PayToCode\",T0.\"TrnspCode\",T0.\"Comments\" " +
                                $"FROM ORDR T0  " +
                                $"INNER JOIN RDR1 T1 ON T0.\"DocEntry\" = T1.\"DocEntry\" " +
                                $"WHERE T0.\"DocStatus\" ='O' and  T1.\"LineStatus\" ='O' " +
                                $"and  T1.\"WhsCode\" ='Web'  and T0.\"DocEntry\" = {baseDocEntry}";

                            SapSalesOrder sapHeader = HanaDataAccessLayer.ExecuteQuery<List<SapSalesOrder>>(query).FirstOrDefault();
                            if (sapHeader == null)
                                throw new Exception($"No records exist for Base DocNum: {baseDocNum}");

                            ArInvoice model = new ArInvoice()
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
                                DocRate = sapHeader.DocRate,
                            };

                            if (sapHeader.CntctCode == 0)
                                model.ContactPersonCode = sapHeader.CntctCode;

                            if (!string.IsNullOrEmpty(sapHeader.U_WMSRef))
                                model.U_WMSRef = sapHeader.U_WMSRef;

                            if (!string.IsNullOrEmpty(sapHeader.U_DelFrom))
                                model.U_DelFrom = sapHeader.U_DelFrom;

                            model.U_OrderType = sapHeader.U_OrderType;
                            int lineNum = 0;
                            foreach (var deliveryConfirmations in group)
                            {
                                query = $"SELECT T0.\"DocEntry\", T0.\"BaseLine\",T0.\"LineStatus\",  T0.\"ItemCode\",T0.\"WhsCode\", T0.\"Quantity\", T0.\"Currency\", T0.\"Rate\", T0.\"Price\", T0.\"VatGroup\" FROM RDR1 T0 WHERE T0.\"LineStatus\" ='O' and  T0.\"DocEntry\" ='{baseDocEntry}' and T0.\"LineNum\" = {deliveryConfirmations.BaseItemRowLineNum}";
                                SapSalesOrderLine sapLine = HanaDataAccessLayer.ExecuteQuery<List<SapSalesOrderLine>>(query).FirstOrDefault();
                                if (sapLine == null)
                                    throw new Exception($"No line is found for Base DocNum: {baseDocNum}");

                                ArInvoiceLine line = new ArInvoiceLine()
                                {
                                    LineNum = lineNum,
                                    ItemCode = sapLine.ItemCode,
                                    Quantity = deliveryConfirmations.InventoryQuantity,
                                    WarehouseCode = sapLine.WhsCode,
                                    UnitPrice = sapLine.Price,
                                    VatGroup = sapLine.VatGroup,
                                    BaseType = 17,
                                    BaseEntry = baseDocEntry,
                                    BaseLine = deliveryConfirmations.BaseItemRowLineNum
                                };
                                model.DocumentLines.Add(line);
                                lineNum++;
                            }

                            query = $"SELECT T0.\"DocEntry\", T0.\"VatGroup\", T0.\"ExpnsCode\" \"ExpenseCode\", T0.\"LineTotal\" FROM RDR3 T0 WHERE T0.\"DocEntry\" ={baseDocEntry} and  T0.\"LineTotal\" > 0";
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
                            var serviceLayer = new SLConnection(AppConfig.ServiceLayerUrl, companyName, Company.UserName, Company.Password);
                            var response = await serviceLayer
                                                .Request("Invoices")
                                                .PostAsync<ArInvoice>(model);

                            Logger.WriteLog($"A/R Invoice created successfully.");

                            query = $@"SELECT TOP 1 CASE WHEN  T0.""DocStatus"" = 'O' THEN 1 ELSE 0 END  FROM ORDR T0 INNER JOIN  RDR1 T1 ON T0.""DocEntry"" = T1.""DocEntry""  WHERE  T1.""LineStatus"" = 'C' and T1.""DocEntry"" = {baseDocEntry} ";
                            var count = await HanaDataAccessLayer.ExecuteCountQueryAsync(query);
                            if (count != 0)
                            {
                                await serviceLayer
                                     .Request($"Orders({baseDocEntry})/Close")
                                     .PostAsync();
                            }

                            query = "UPDATE [dbo].[WMSDeliveryConfirmation] "
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
                            Logger.WriteLog($"Exception occurs while posting A/R invoice due to {ex.Message}");
                            continue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLog($"Exception: {ex.Message}");
                }
            }
            Logger.WriteLog($"A/R Invoice Import ends.");
        }
    }
}
