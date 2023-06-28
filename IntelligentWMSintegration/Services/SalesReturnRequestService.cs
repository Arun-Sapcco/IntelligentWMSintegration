using IntelligentWmsIntegration.DAL;
using IntelligentWmsIntegration.Helpers;
using IntelligentWmsIntegration.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;

namespace IntelligentWmsIntegration.Services
{
    public class SalesReturnRequestService
    {
        public async static Task Export()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            Logger.WriteLog($"Sales Return Request Export starts.");
            foreach (var Company in AppConfig.CompanyList)
            {
                try
                {
                    var companyName = Company.CompanyDB;
                    Logger.WriteLog($"Company Name: {companyName}");

                    string query = "SELECT DISTINCT T0.\"U_CompanyCode\", T1.\"BaseType\", T0.\"CardCode\", T0.\"CardName\", T0.\"DocNum\", T0.\"DocEntry\", T0.\"DocDate\", "
                          + "T0.\"DocDueDate\", T0.\"NumAtCard\", T0.\"TrnspCode\", T0.\"ShipToCode\", T0.\"Address2\", T2.\"SlpName\",T0.\"U_IsProcessed\", "
                          + "T0.\"Comments\"  FROM ORRR T0 INNER JOIN RRR1 T1 ON T0.\"DocEntry\" = T1.\"DocEntry\" "
                          + "INNER JOIN OSLP T2 ON T0.\"SlpCode\" = T2.\"SlpCode\" WHERE T0.\"DocStatus\" = 'O' and "
                          + "T1.\"LineStatus\" = 'O'  "
                          //+ "and T1.\"WhsCode\" = 'Web' "
                          + "and T0.U_CTX_STTS = 'P' "
                          + "ORDER BY T0.\"DocDate\" desc";

                    string HanaConnectionString = Company.HanaConnectionString;
                    HanaDataAccessLayer HanaDataAccessLayer = new HanaDataAccessLayer(HanaConnectionString);
                    List<SapReturnRequest> documents = await HanaDataAccessLayer.ExecuteQueryAsync<List<SapReturnRequest>>(query);

                    foreach (var header in documents)
                    {
                        query = $"SELECT Count(DocumentEntry) FROM SAPReturnToCustomerHeader WHERE DocumentEntry = {header.DocEntry} and CompanyCode = 'PU' and DocumentNumber = '{header.DocNum}' ";

                        string sqlConnectionString = AppConfig.WmsConnectionString;
                        SqlDataAccessLayer dac = new SqlDataAccessLayer(sqlConnectionString);
                        DataTable dt = await dac.ExecuteQueryAsync<DataTable>(query);
                        int count = Convert.ToInt32(dt.Rows[0][0]);

                        if (count != 0)
                        {
                            Logger.WriteLog($"Records already exists.");
                            continue;
                        }

                        // Insert header
                        query = "INSERT INTO [dbo].[SAPReturnToCustomerHeader] ("
                              + "[CompanyCode] "
                              + ",[BaseDocType] "
                              + ",[CustomerCode] "
                              + ",[CustomerName] "
                              + ",[DocumentNumber] "
                              + ",[DocumentEntry] "
                              + ",[PostingDate] "
                              + ",[DeliveryDate] "
                              + ",[ReferenceNumber] "
                              + ",[ShippingType] "
                              + ",[ShipToCode] "
                              + ",[ShipToAddress] "
                              + ",[SalesExecutive] "
                              + ",[LastUpdateDateTime] "
                              + ",[IsProcessed] "
                              + ",[Remarks] "
                              + ",[UniquePrimaryKey]) "
                              + "VALUES "
                              + $"('{header.U_CompanyCode}', "
                              + $"'{header.BaseType}', "
                              + $"'{header.CardCode}', "
                              + $"'{header.CardName}', "
                              + $"'{header.DocNum}', "
                              + $"'{header.DocEntry}', "
                              + $"'{header.DocDate.ToString("yyyy-MM-dd")}', "
                              + $"'{header.DocDueDate.ToString("yyyy-MM-dd")}', "
                              + $"'{header.NumAtCard}',  "
                              + $"'{header.TrnspCode}', "
                              + $"'{header.ShipToCode}',  "
                              + $"'{header.Address2}',  "
                              + $"'{header.SlpName}',  "
                              + $"'{DateTime.Now}',  "
                              + $"'2', "
                              + $"'{header.Comments}', "
                              + $"'{header.DocEntry}_{header.U_CompanyCode}' )";
                        //+ $",'{header.ic}'< ICType1, nvarchar(50),> "
                        //+ $",'{header}' < ICType2, nvarchar(50),>)";

                        List<string> queryList = new List<string>();
                        queryList.Add(query);

                        query = "SELECT T0.\"DocNum\", T0.\"DocEntry\",T1.\"LineNum\", T1.\"ItemCode\", T1.\"Quantity\",T1.\"unitMsr\", T1.\"InvQty\", T3.\"InvntryUom\", T1.\"Price\", T1.\"WhsCode\", T1.\"FreeTxt\", T1.\"ReturnRsn\", T1.\"BaseType\", T1.\"BaseEntry\", T1.\"BaseLine\", T0.\"Comments\", T0.\"U_IsProcessed\", T1.\"LineStatus\", '' \"UniquePrimaryKey\",T1.\"U_CompanyCode\" "
                              + "FROM ORRR T0  "
                              + "INNER JOIN RRR1 T1 ON T0.\"DocEntry\" = T1.\"DocEntry\" INNER JOIN OSLP T2 ON T0.\"SlpCode\" = T2.\"SlpCode\" "
                              + "INNER JOIN OITM T3 ON T1.\"ItemCode\" = T3.\"ItemCode\" "
                              + "WHERE T0.\"DocStatus\" ='O' and  T1.\"LineStatus\" ='O'  "
                              + $"and  T1.\"WhsCode\" ='Web' AND T0.\"DocEntry\" = {header.DocEntry} AND T0.\"U_CompanyCode\" = '{header.U_CompanyCode}' "
                              + "ORDER BY T0.\"DocDate\" desc";

                        List<SapReturnToCustomerDetails> lines = await HanaDataAccessLayer.ExecuteQueryAsync<List<SapReturnToCustomerDetails>>(query);

                        foreach (var line in lines)
                        {
                            query = "INSERT INTO [dbo].[SAPReturnToCustomerDetails] "
                                  + "([DocumentNumber] "
                                  + ",[DocEntry] "
                                  + ",[LineNum] "
                                  + ",[ItemCode] "
                                  + ",[Quantity] "
                                  + ",[UOM] "
                                  + ",[InventoryQuantity] "
                                  + ",[InventoryUoM] "
                                  + ",[Price] "
                                  + ",[Warehouse] "
                                  + ",[Text] "
                                  + ",[Returnreason] "
                                  + ",[BaseDocType] "
                                  + ",[BaseDocEntry] "
                                  + ",[BaseDocNum] "
                                  + ",[BaseLineNum] "
                                  + ",[Remarks] "
                                  + ",[LastUpdateDateTime] "
                                  + ",[IsProcessed] "
                                  + ",[Close] "
                                  + ",[UniquePrimaryKey] "
                                  + ",[CompanyCode]) "
                                  + " VALUES "
                                  + $"({line.DocNum} "
                                  + $",{line.DocEntry} "
                                  + $",{line.LineNum} "
                                  + $",'{line.ItemCode}' "
                                  + $",{line.Quantity} "
                                  + $",'{line.InvntryUom}' "
                                  + $",'{line.InvQty}' "
                                  + $",'{line.InvntryUom}' "
                                  + $",{line.Price}"
                                  + $",'{line.WhsCode}' "
                                  + $",'{line.FreeTxt}' "
                                  + $",NULL "
                                  + $",'{line.BaseType}' "
                                  + $",'{line.BaseEntry}' "
                                  + $",'{line.BaseEntry}' "
                                  + $",'{line.BaseLine}' "
                                  + $",'{line.Comments}' "
                                  + $",'{DateTime.Now}' "
                                  + $",'' "
                                  + $",'' "
                                  + $",'{line.DocEntry}_{line.U_CompanyCode}' "
                                  + $",'{line.U_CompanyCode}')";

                            queryList.Add(query);
                        }
                        bool isCommitted = await dac.ExecuteNonQueryWithTransactionAsync(queryList);

                        if (isCommitted)
                        {
                            Logger.WriteLog($"Updating flag in Sales Return Request DocNum: {header.DocNum} in Company Code: {companyName}.");

                            query = "UPDATE ORDR "
                                  + "SET U_CTX_STTS = 'A'"
                                  + $"WHERE \"DocEntry\" = '{header.DocEntry}'";

                            await HanaDataAccessLayer.ExecuteNonQueryAsync(query);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLog($"Exception: {ex.Message}");
                }
            }
            Logger.WriteLog($"Sales Return Request Export ends.");
            stopwatch.Stop();

            var timeElaspsed = stopwatch.Elapsed;
        }
    }
}
