using IntelligentWmsIntegration.Helpers;
using IntelligentWmsIntegration.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using IntelligentWmsIntegration.DAL;

namespace IntelligentWmsIntegration.Services
{
    public class WebArCreditMemoService
    {
        public static void Import()
        {
            Logger.WriteLog($"AR Credit Memo Import starts.");
            foreach (var Company in AppConfig.CompanyList)
            {
                try
                {
                    var companyName = Company.CompanyDB;
                    Logger.WriteLog($"Company Name: companyName");

                    string query = "SELECT distinct T0.\"DocNum\", T1.\"DocEntry\" FROM ORRR T0  " +
                            "INNER JOIN RRR1 T1 ON T0.\"DocEntry\" = T1.\"DocEntry\" " +
                            "WHERE T1.\"WhsCode\" ='Web' and T0.\"DocStatus\"='O'";

                    string HanaConnectionString = Company.HanaConnectionString;
                    HanaDataAccessLayer HanaDataAccessLayer = new HanaDataAccessLayer(HanaConnectionString);
                    List<ArCreditMemo> sapList = HanaDataAccessLayer.ExecuteQuery<List<ArCreditMemo>>(query);

                    query = "select * from [dbo].[WMSReturnToCustomerInvoice] "
                         + $"where isprocessed IS NULL and CompanyCode = '{Company.CompanyDB.Split('_').First()}' "
                         + "and BaseDocumentNumber IS NOT NULL and BaseDocumentEntry IS NOT NULL and BaseItemRowLineNum IS NOT NULL "
                         + $"and Warehouse = 'Web' AND InventoryQuantity > 0  and CompanyCode = 'PU' ";
                    //+ $"and BaseDocumentEntry ='27322'"
                    //+ $"and BaseDocumentNumber ='1019206'";

                    string sqlConnectionString = AppConfig.WmsConnectionString;
                    SqlDataAccessLayer dac = new SqlDataAccessLayer(sqlConnectionString);
                    List<WMSReturnToCustomerInvoice> wmsList = dac.ExecuteQuery<List<WMSReturnToCustomerInvoice>>(query);

                    var joinedList = from sap in sapList
                                     join wms in wmsList
                                     on sap.DocEntry equals wms.BaseDocumentEntry
                                     select wms;

                    var groupedWebArCreditMemo = joinedList.GroupBy(x => x.BaseDocumentEntry);

                    //ServiceLayer
                    //SAPbobsCOM.Documents oDoc = null;
                    //foreach (var group in groupedWebArCreditMemo)
                    //{
                    //    int baseDocumentEntry = 0;
                    //    int baseDocumentNumber = 0;
                    //    try
                    //    {
                    //        baseDocumentEntry = group.Key;
                    //        baseDocumentNumber = group.FirstOrDefault().BaseDocumentNumber;
                            
                    //        Logger.WriteLog($"Processing Return Request with Docnum: {baseDocumentNumber}");

                    //        query = $"SELECT T0.\"CardCode\", T0.\"CardName\", T0.\"CntctCode\", T0.\"DocCur\",T0.\"DocRate\",T0.\"OwnerCode\", T0.\"SlpCode\", T0.\"NumAtCard\", T0.\"U_WMSRef\", T0.\"U_OrderType\", T0.\"U_DelFrom\", T0.\"U_CompanyCode\", T0.\"DocDate\",T0.\"ShipToCode\",T0.\"PayToCode\",T0.\"TrnspCode\",T0.\"Comments\", T1.\"BaseType\" " +
                    //                $"FROM ORRR T0  " +
                    //                $"INNER JOIN RRR1 T1 ON T0.\"DocEntry\" = T1.\"DocEntry\" " +
                    //                $"WHERE T0.\"DocStatus\" ='O' and  T1.\"LineStatus\" ='O' " +
                    //                $"and  T1.\"WhsCode\" ='Web'  and T0.\"DocEntry\" = {baseDocumentEntry}";

                    //        ArCreditMemo header = hanaDataAccessLayer.ExecuteQuery<List<ArCreditMemo>>(query).FirstOrDefault();
                    //        if (header == null)
                    //            throw new Exception($"No records exist for Base DocNum: {baseDocumentNumber}");

                    //        if (header.BaseType == 15)
                    //        {
                    //            // Returns
                    //            Logger.WriteLog($"Starts Creating Return");

                    //            oDoc = (SAPbobsCOM.Documents)ServerConnection.GetCompany(Company).GetBusinessObject(BoObjectTypes.oReturns);
                    //            oDoc.CardCode = header.CardCode;
                    //            oDoc.DocumentsOwner = header.OwnerCode;
                    //            oDoc.ShipToCode = header.ShipToCode;
                    //            oDoc.SalesPersonCode = header.SlpCode;
                    //            oDoc.NumAtCard = header.NumAtCard;
                    //            oDoc.DocDate = header.DocDate;
                    //            oDoc.PayToCode = header.PayToCode;
                    //            oDoc.TransportationCode = header.TrnspCode;
                    //            oDoc.Comments = header.Comments;
                    //            oDoc.DocCurrency = header.DocCur;
                    //            oDoc.DocRate = header.DocRate;

                    //            if (header.CntctCode == 0)
                    //                oDoc.ContactPersonCode = header.CntctCode;

                    //            if (!string.IsNullOrEmpty(header.U_WMSRef))
                    //                oDoc.UserFields.Fields.Item("U_WMSRef").Value = header.U_WMSRef;

                    //            oDoc.UserFields.Fields.Item("U_OrderType").Value = header.U_OrderType;

                    //            foreach (var groupedWebArCreditMemos in group)
                    //            {
                    //                query = $"SELECT T0.\"DocEntry\", T0.\"BaseLine\",T0.\"LineStatus\",  T0.\"ItemCode\",T0.\"WhsCode\", T0.\"Quantity\", T0.\"Currency\", T0.\"Rate\", T0.\"Price\", T0.\"VatGroup\", T0.\"BaseType\" FROM RRR1 T0 WHERE T0.\"LineStatus\" ='O' and  T0.\"DocEntry\" ='{baseDocumentEntry}' and T0.\"LineNum\" = {groupedWebArCreditMemos.BaseItemRowLineNum}";
                    //                ArCreditMemoLine line = hanaDataAccessLayer.ExecuteQuery<List<ArCreditMemoLine>>(query).FirstOrDefault();
                    //                if (line == null)
                    //                    throw new Exception($"No line is found for Base DocNum: {baseDocumentNumber}");

                    //                oDoc.Lines.ItemCode = line.ItemCode;
                    //                oDoc.Lines.Quantity = groupedWebArCreditMemos.InventoryQuantity;
                    //                oDoc.Lines.WarehouseCode = line.WhsCode;
                    //                oDoc.Lines.UnitPrice = line.Price;
                    //                oDoc.Lines.TaxCode = line.VatGroup;

                    //                oDoc.Lines.BaseType = header.BaseType;
                    //                oDoc.Lines.BaseEntry = baseDocumentEntry;
                    //                oDoc.Lines.BaseLine = groupedWebArCreditMemos.BaseItemRowLineNum;
                    //                oDoc.Lines.Add();
                    //            }

                    //            if (oDoc.Add() != 0)
                    //            {
                    //                string error = ServerConnection.GetCompany(Company).GetLastErrorDescription();
                    //                Logger.WriteLog($"Error while adding Return document: {error}");
                    //            }
                    //            else
                    //            {
                    //                Logger.WriteLog($"Return is created succcessfully.");
                    //            }
                    //        }

                    //        else if (header.BaseType == 13 || header.BaseType == -1)
                    //        {
                    //            // AR Credit Memo
                    //            Logger.WriteLog($"Starts Creating Credit Memo");

                    //            oDoc = (SAPbobsCOM.Documents)ServerConnection.GetCompany(Company).GetBusinessObject(BoObjectTypes.oCreditNotes);
                    //            oDoc.CardCode = header.CardCode;
                    //            oDoc.DocumentsOwner = header.OwnerCode;
                    //            oDoc.ShipToCode = header.ShipToCode;
                    //            oDoc.SalesPersonCode = header.SlpCode;
                    //            oDoc.NumAtCard = header.NumAtCard;
                    //            oDoc.DocDate = header.DocDate;
                    //            oDoc.PayToCode = header.PayToCode;
                    //            oDoc.TransportationCode = header.TrnspCode;
                    //            oDoc.Comments = header.Comments;
                    //            oDoc.DocCurrency = header.DocCur;
                    //            oDoc.DocRate = header.DocRate;

                    //            if (header.CntctCode == 0)
                    //                oDoc.ContactPersonCode = header.CntctCode;

                    //            if (!string.IsNullOrEmpty(header.U_WMSRef))
                    //                oDoc.UserFields.Fields.Item("U_WMSRef").Value = header.U_WMSRef;

                    //            oDoc.UserFields.Fields.Item("U_OrderType").Value = header.U_OrderType;

                    //            foreach (var groupedWebArCreditMemos in group)
                    //            {
                    //                query = $"SELECT T0.\"DocEntry\", T0.\"BaseLine\",T0.\"LineStatus\",  T0.\"ItemCode\",T0.\"WhsCode\", T0.\"Quantity\", T0.\"Currency\", T0.\"Rate\", T0.\"Price\", T0.\"VatGroup\", T0.\"BaseType\" FROM RRR1 T0 WHERE T0.\"LineStatus\" ='O' and  T0.\"DocEntry\" ='{baseDocumentEntry}' and T0.\"LineNum\" = {groupedWebArCreditMemos.BaseItemRowLineNum}";
                    //                ArCreditMemoLine line = hanaDataAccessLayer.ExecuteQuery<List<ArCreditMemoLine>>(query).FirstOrDefault();
                    //                if (line == null)
                    //                    throw new Exception($"No line is found for Base DocNum: {baseDocumentNumber}");

                    //                oDoc.Lines.ItemCode = line.ItemCode;
                    //                oDoc.Lines.Quantity = groupedWebArCreditMemos.InventoryQuantity;
                    //                oDoc.Lines.WarehouseCode = line.WhsCode;
                    //                oDoc.Lines.UnitPrice = line.Price;
                    //                oDoc.Lines.TaxCode = line.VatGroup;

                    //                oDoc.Lines.BaseType = header.BaseType;
                    //                oDoc.Lines.BaseEntry = baseDocumentEntry;
                    //                oDoc.Lines.BaseLine = groupedWebArCreditMemos.BaseItemRowLineNum;
                    //                oDoc.Lines.Add();
                    //            }

                    //            if (oDoc.Add() != 0)
                    //            {
                    //                string error = ServerConnection.GetCompany(Company).GetLastErrorDescription();
                    //                Logger.WriteLog($"Error while adding AR Credit Memo document: {error}");
                    //            }
                    //            else
                    //                Logger.WriteLog($"AR Credit Memo is created succcessfully.");
                    //        }

                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        Logger.WriteLog($"Error while adding document: {ex.Message}");
                    //    }
                    //}
                }
                catch (Exception ex)
                {
                    Logger.WriteLog($"Exception: {ex.Message}");
                }
            }
            Logger.WriteLog($"AR Credit Memo Import ends.");
        }
    }
}
