using IntelligentWmsIntegration.DAL;
using IntelligentWmsIntegration.Helpers;
using IntelligentWmsIntegration.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace IntelligentWmsIntegration.Services
{
    public class SalesOrderService
    {
        public static void Export()
        {
            Logger.WriteLog($"Sales Order Export starts.");
            foreach (var Company in AppConfig.CompanyList)
            {
                try
                {
                    var companyName = Company.CompanyDB;
                    Logger.WriteLog($"Company Name: {companyName}");

                    string query = "SELECT DISTINCT T0.\"U_CompanyCode\", T0.\"U_OrderType\",T0.\"Confirmed\",T0.\"U_Confirmed\", T0.\"CardCode\", T0.\"CardName\", "
                          + "T0.\"DocNum\", T0.\"DocEntry\", T0.\"DocDate\", T0.\"DocDueDate\", T0.\"NumAtCard\", T0.\"U_DelFrom\", "
                          + "T0.\"DocStatus\",T0.\"TrnspCode\", T0.\"ShipToCode\", T0.\"Address2\", T0.\"PickRmrk\", "
                          + "T0.\"U_Priority\", T2.\"SlpName\", "
                          + "T0.\"U_IsProcessed\",T0.\"U_IC\", "
                          + "T0.\"U_PROCTYPE\" "
                          + "FROM \"ORDR\"  T0 INNER JOIN \"RDR1\"  T1 ON T0.\"DocEntry\" = T1.\"DocEntry\" "
                          + "INNER JOIN \"OSLP\"  T2 ON T0.\"SlpCode\" = T2.\"SlpCode\" "
                          + "Where "
                          + "T1.\"WhsCode\"='Web' and "
                          + "T0.\"U_DelFrom\" ='PU' and "
                          + "T0.\"DocStatus\"='O' and "
                          + "T1.\"LineStatus\"='O' AND "
                          + "T0.U_CTX_STTS = 'P'"
                          //+ "T0.\"DocNum\"='1016292' "
                          + "Order By T0.\"DocDate\" desc";

                    string HanaConnectionString = Company.HanaConnectionString;
                    HanaDataAccessLayer HanaDataAccessLayer = new HanaDataAccessLayer(HanaConnectionString);
                    List<SapSalesOrder> documents = HanaDataAccessLayer.ExecuteQuery<List<SapSalesOrder>>(query);

                    foreach (var header in documents)
                    {
                        query = "SELECT count(TX.DocumentEntry) FROM "
                             + $"( SELECT DocumentEntry FROM SAPSalesOrderHeader WHERE DocumentEntry = {header.DocEntry} and DocumentNumber = '{header.DocNum}' and CompanyCode = '{Company.CompanyDB.Split('_').First()}' "
                             + "UNION ALL "
                             + $"SELECT DocumentEntry FROM SAPSalesOrderHeaderArch WHERE DocumentEntry = {header.DocEntry} and DocumentNumber = '{header.DocNum}' and CompanyCode = '{Company.CompanyDB.Split('_').First()}' ) TX";

                        string wmsConnectionString = AppConfig.WmsConnectionString;
                        SqlDataAccessLayer dac = new SqlDataAccessLayer(wmsConnectionString);
                        DataTable dt = dac.ExecuteQuery(query);
                        int count = Convert.ToInt32(dt.Rows[0][0]);

                        if (count != 0)
                        {
                            Logger.WriteLog($"Records already exists.");
                            continue;
                        }

                        List<string> querylist = new List<string>();
                        // Insert header
                        query = "INSERT INTO [dbo].[SAPSalesOrderHeader] "
                              + "([CompanyCode] "
                              + ",[OrderType] "
                              + ",[Approved] "
                              + ",[Confirmed] "
                              + ",[CustomerCode] "
                              + ",[CustomerName] "
                              + ",[DocumentNumber] "
                              + ",[DocumentEntry] "
                              + ",[PostingDate] "
                              + ",[DeliveryDate] "
                              + ",[ReferenceNumber] "
                              + ",[DeliveryFrom] "
                              + ",[DocumentStatus] "
                              + ",[ShippingType] "
                              + ",[ShipToCode] "
                              + ",[ShipToAddress] "
                              + ",[PickandPackRemark] "
                              + ",[SalesExecutive] "
                              + ",[Priority] "
                              + ",[LastUpdateDateTime] "
                              + ",[IsProcessed] "
                              + ",[UniquePrimaryKey] )"
                              //+ ",[ICType1] "
                              //+ ",[ICType2]) "
                              + "  VALUES "
                              + $"('{header.U_CompanyCode}' "
                              + $",'{header.U_OrderType}' "
                              + $",'{header.U_Confirmed}' "
                              + $",'{header.Confirmed}' "
                              + $",'{header.CardCode}' "
                              + $",'{header.CardName}' "
                              + $",'{header.DocNum}' "
                              + $",'{header.DocEntry}' "
                              + $",'{header.DocDate.ToString("yyyy-MM-dd")}' "
                              + $",'{header.DocDueDate.ToString("yyyy-MM-dd")}' "
                              + $",'{header.NumAtCard}' "
                              + $",'PU' "
                              + $",'{header.DocStatus}' "
                              + $",'{header.TrnspCode}' "
                              + $",'{header.ShipToCode}' "
                              + $",'{header.Address2}' "
                              + $",'{header.PickRmrk}' "
                              + $",'{header.SlpName}' "
                              + $",'{header.U_Priority}' "
                              + $",'{DateTime.Now}' "
                              + $",'0' "
                              + $",'{header.U_CompanyCode}_{header.DocEntry}' )";
                        //+ $",'{header.ic}'< ICType1, nvarchar(50),> "
                        //+ $",'{header}' < ICType2, nvarchar(50),>)";

                        querylist.Add(query);

                        query = "SELECT T0.\"DocNum\", "
                              + "T1.\"DocEntry\", T1.\"LineNum\", T1.\"ItemCode\",T1.\"Quantity\", "
                              + "T1.\"unitMsr\",T2.\"SalUnitMsr\", T2.\"InvntryUom\", "
                              + "T1.\"InvQty\",T1.\"Price\", "
                              + "T1.\"U_WhsType\", "
                              + "T1.\"WhsCode\", "
                              + "T1.\"U_BaseRef_SO\", "
                              + "T1.\"U_BaseEntry_SO\", "
                              + "T1.\"U_BaseLine_SO\", "
                              + "T1.\"U_CompanyCode\", "
                              + "T1.\"U_BaseLineNum\", "
                              + "T1.\"U_BaseCompanyCode\" "
                              + "FROM ORDR T0 INNER JOIN RDR1 T1 ON T0.\"DocEntry\" = T1.\"DocEntry\" "
                              + "INNER JOIN OITM T2 ON T1.\"ItemCode\" = T2.\"ItemCode\" "
                              + $"Where T1.\"U_CompanyCode\" = '{header.U_CompanyCode}' and T1.\"DocEntry\" = '{header.DocEntry}' AND T1.\"WhsCode\"='Web' and T0.\"DocStatus\"='O'";

                        List<SapSalesOrderLine> lines = HanaDataAccessLayer.ExecuteQuery<List<SapSalesOrderLine>>(query);

                        if (lines.Count == 0)
                            continue;

                        foreach (var line in lines)
                        {
                            query = "INSERT INTO [dbo].[SAPSalesOrderDetails] "
                                  + "([DocumentNumber] "
                                  + ",[DocumentEntry] "
                                  + ",[ItemRowLineNum] "
                                  + ",[ItemCode] "
                                  + ",[CatalogNum] "
                                  + ",[Quantity] "
                                  + ",[UOM] "
                                  + ",[InventoryQuantity] "
                                  + ",[InventoryUoM] "
                                  + ",[Price] "
                                  + ",[WarehouseType] "
                                  + ",[Warehouse] "
                                  + ",[Batch] "
                                  + ",[Chemicals] "
                                  + ",[CountryOfOrigin] "
                                  + ",[Coded] "
                                  + ",[Quality] "
                                  + ",[Barcode] "
                                  + ",[WEB] "
                                  + ",[Ownership] "
                                  + ",[Text] "
                                  + ",[CloseRow] "
                                  + ",[DocumentRemark] "
                                  + ",[Close] "
                                  + ",[LastUpdateDateTime] "
                                  + ",[IsProcessed] "
                                  + ",[BaseRef_SO] "
                                  + ",[BaseEntry_SO] "
                                  + ",[BaseLine_SO] "
                                  + ",[UniquePrimaryKey] "
                                  + ",[CompanyCode] "
                                  + ",[BaseCompanyCode] "
                                  + ",[BaseLineNum_PO]) "
                                  + " VALUES "
                                  + $"( '{line.DocNum}' "
                                  + $",'{line.DocEntry}' "
                                  + $",'{line.LineNum}' "
                                  + $",'{line.ItemCode}' "
                                  + $",'' "
                                  + $",'{line.Quantity}' "
                                  + $",'{line.InvntryUom}' "
                                  + $",'{line.InvQty}' "
                                  + $",'{line.InvntryUom}' "
                                  + $",'{line.Price}' "
                                  + $",'CEN' "
                                  + $",'{line.WhsCode}' "
                                  + $",'' "
                                  + $",'' "
                                  + $",'' "
                                  + $",'' "
                                  + $",'' "
                                  + $",'' "
                                  + $",'' "
                                  + $",'' "
                                  + $",'' "
                                  + $",'' "
                                  + $",'{line.Comments}' "
                                  + $",'' "
                                  + $",'{DateTime.Now}' "
                                  + $",'' "
                                  + $",'{line.U_BaseRef_SO}' "
                                  + $",'{line.U_BaseEntry_SO}' "
                                  + $",'{line.U_BaseLine_SO}' "
                                  + $",'{line.U_BaseCompanyCode}_{line.DocEntry}' "
                                  + $",'{line.U_BaseCompanyCode}' "
                                  + $",'{line.U_BaseCompanyCode}' "
                                  + $",'{line.U_BaseLineNum}')";

                            querylist.Add(query);
                        }
                        bool isCommitted = dac.ExecuteNonQueryWithTransaction(querylist);
                        if (isCommitted)
                        {
                            Logger.WriteLog($"Updating flag in Sales Order DocNum: {header.DocNum} in Company Code: {companyName}.");
                            
                            query = "UPDATE ORDR "
                                  + "SET U_CTX_STTS = 'A'"
                                  + $"WHERE \"DocEntry\" = '{header.DocEntry}'";
                            HanaDataAccessLayer.ExecuteNonQuery(query);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLog($"Exception: {ex.Message}");
                }
            }
            Logger.WriteLog($"Sales Order Export ends.");
        }
    }
}
