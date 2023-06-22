using IntelligentWmsIntegration.Helpers;
using IntelligentWmsIntegration.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using IntelligentWmsIntegration.DAL;

namespace IntelligentWmsIntegration.Services
{
    public class WebArInvoiceService
    {
        public static void Import()
        {
            Logger.WriteLog($"AR Invoice Import starts.");
            foreach (var Company in AppConfig.CompanyList)
            {
                try
                {
                    var companyName = Company.CompanyDB;
                    Logger.WriteLog($"Company Name: {companyName}");

                    string query = "SELECT distinct T0.\"DocNum\", T1.\"DocEntry\" FROM ORDR T0  " +
                            "INNER JOIN RDR1 T1 ON T0.\"DocEntry\" = T1.\"DocEntry\" " +
                            "WHERE T1.\"WhsCode\" ='Web' and T0.\"DocStatus\"='O'";

                    string HanaConnectionString = Company.HanaConnectionString;
                    HanaDataAccessLayer hanaDataAccessLayer = new HanaDataAccessLayer(HanaConnectionString);
                    List<SalesOrder> sapList = hanaDataAccessLayer.ExecuteQuery<List<SalesOrder>>(query);

                    query = "select * from [dbo].[WMSDeliveryConfirmation] "
                          + $"where isprocessed IS NULL and CompanyCode = '{Company.CompanyDB.Split('_').First()}' "
                          + "and BaseDocumentNumber IS NOT NULL and BaseDocumentEntry IS NOT NULL and BaseItemRowLineNum IS NOT NULL "
                          + $"and Warehouse = 'Web' AND InventoryQuantity > 0 and CompanyCode = 'PU' ";
                        //+ $"and BaseDocumentEntry ='27322'"
                        //+ $"and BaseDocumentNumber ='1019206'";

                    string sqlConnectionString = AppConfig.WmsConnectionString;
                    SqlDataAccessLayer dac = new SqlDataAccessLayer(sqlConnectionString);
                    List<WMSDeliveryConfirmation> wmsList = dac.ExecuteQuery<List<WMSDeliveryConfirmation>>(query);

                    var joinedList = from sap in sapList
                                     join wms in wmsList
                                     on sap.DocEntry equals wms.BaseDocumentEntry
                                     select wms;

                    var groupedDeliveryConfirmations = joinedList.GroupBy(x => x.BaseDocumentEntry);


                    //ServiceLayer

                    //SAPbobsCOM.Documents oInvoice = null;
                    //foreach (var group in groupedDeliveryConfirmations)
                    //{
                    //    int baseDocumentEntry = 0;
                    //    int baseDocumentNumber = 0;
                    //    try
                    //    {
                    //        baseDocumentEntry = group.Key;
                    //        baseDocumentNumber = group.FirstOrDefault().BaseDocumentNumber;
                    //        Logger.WriteLog($"Processing Delivery with Sales Order Docnum: {baseDocumentNumber}");

                    //        query = $"SELECT T0.\"CardCode\", T0.\"CardName\", T0.\"CntctCode\", T0.\"DocCur\",T0.\"DocRate\",T0.\"OwnerCode\", T0.\"SlpCode\", T0.\"NumAtCard\", T0.\"U_WMSRef\", T0.\"U_OrderType\", T0.\"U_DelFrom\", T0.\"U_CompanyCode\", T0.\"DocDate\",T0.\"ShipToCode\",T0.\"PayToCode\",T0.\"TrnspCode\",T0.\"Comments\" " +
                    //            $"FROM ORDR T0  " +
                    //            $"INNER JOIN RDR1 T1 ON T0.\"DocEntry\" = T1.\"DocEntry\" " +
                    //            $"WHERE T0.\"DocStatus\" ='O' and  T1.\"LineStatus\" ='O' " +
                    //            $"and  T1.\"WhsCode\" ='Web'  and T0.\"DocEntry\" = {baseDocumentEntry}";

                    //        dt = hanaDataAccessLayer.ExecuteQuery(query);
                    //        SalesOrder header = hanaDataAccessLayer.ExecuteQuery<List<SalesOrder>>(query).FirstOrDefault();
                    //        if (header == null)
                    //            throw new Exception($"No records exist for Base DocNum: {baseDocumentNumber}");

                    //        oInvoice = (SAPbobsCOM.Documents)ServerConnection.GetCompany(Company).GetBusinessObject(BoObjectTypes.oInvoices);

                    //        oInvoice.CardCode = header.CardCode;
                    //        oInvoice.DocumentsOwner = header.OwnerCode;
                    //        oInvoice.ShipToCode = header.ShipToCode;
                    //        oInvoice.SalesPersonCode = header.SlpCode;
                    //        oInvoice.NumAtCard = header.NumAtCard;
                    //        oInvoice.DocDate = header.DocDate;
                    //        oInvoice.PayToCode = header.PayToCode;
                    //        oInvoice.TransportationCode = header.TrnspCode;
                    //        oInvoice.Comments = header.Comments;
                    //        oInvoice.DocCurrency = header.DocCur;
                    //        oInvoice.DocRate = header.DocRate;

                    //        if (header.CntctCode == 0)
                    //            oInvoice.ContactPersonCode = header.CntctCode;

                    //        if (!string.IsNullOrEmpty(header.U_WMSRef))
                    //            oInvoice.UserFields.Fields.Item("U_WMSRef").Value = header.U_WMSRef;

                    //        oInvoice.UserFields.Fields.Item("U_OrderType").Value = header.U_OrderType;

                    //        foreach (var deliveryConfirmations in group)
                    //        {
                    //            query = $"SELECT T0.\"DocEntry\", T0.\"BaseLine\",T0.\"LineStatus\",  T0.\"ItemCode\",T0.\"WhsCode\", T0.\"Quantity\", T0.\"Currency\", T0.\"Rate\", T0.\"Price\", T0.\"VatGroup\" FROM RDR1 T0 WHERE T0.\"LineStatus\" ='O' and  T0.\"DocEntry\" ='{baseDocumentEntry}' and T0.\"LineNum\" = {deliveryConfirmations.BaseItemRowLineNum}";
                    //            SalesOrderLine line = hanaDataAccessLayer.ExecuteQuery<List<SalesOrderLine>>(query).FirstOrDefault();
                    //            if (line == null)
                    //                throw new Exception($"No line is found for Base DocNum: {baseDocumentNumber}");

                    //            oInvoice.Lines.ItemCode = line.ItemCode;
                    //            oInvoice.Lines.Quantity = deliveryConfirmations.InventoryQuantity;
                    //            oInvoice.Lines.WarehouseCode = line.WhsCode;
                    //            oInvoice.Lines.UnitPrice = line.Price;
                    //            oInvoice.Lines.TaxCode = line.VatGroup;


                    //            oInvoice.Lines.BaseType = 17;
                    //            oInvoice.Lines.BaseEntry = baseDocumentEntry;
                    //            oInvoice.Lines.BaseLine = deliveryConfirmations.BaseItemRowLineNum;
                    //            oInvoice.Lines.Add();
                    //        }

                    //        query = $"SELECT top 1 T0.\"DocEntry\", T0.\"VatGroup\", T0.\"ExpnsCode\", T0.\"LineTotal\" FROM RDR3 T0 WHERE T0.\"DocEntry\" ={baseDocumentEntry} and  T0.\"LineTotal\" > 0";

                    //        dt = hanaDataAccessLayer.ExecuteQuery(query);
                    //        if (dt != null)
                    //        {
                    //            oInvoice.Expenses.ExpenseCode = Convert.ToInt32(dt.Rows[0][2]);
                    //            oInvoice.Expenses.VatGroup = dt.Rows[0][1].ToString();
                    //            oInvoice.Expenses.LineTotal = Convert.ToDouble(dt.Rows[0][3]);
                    //            oInvoice.Expenses.Add();
                    //        }
                    //        if (oInvoice.Add() != 0)
                    //        {
                    //            string error = ServerConnection.GetCompany(Company).GetLastErrorDescription();
                    //            Logger.WriteLog($"Error while adding Delivery document: {error}");
                    //        }
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        Logger.WriteLog($"Error while adding Delivery document: {ex.Message}");
                    //    }
                    //}
                }
                catch (Exception ex)
                {
                    Logger.WriteLog($"Exception: {ex.Message}");
                }
            }
            Logger.WriteLog($"AR Invoice Import ends.");
        }
    }
}
