using B1SLayer;
using IntelligentWmsIntegration.DAL;
using IntelligentWmsIntegration.Helpers;
using IntelligentWmsIntegration.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace IntelligentWmsIntegration.Services
{
    internal class WmsIntegrationService
    {
        private List<string> GetCompanyList()
        {
            string query = $@"select distinct CompanyCode FROM WMSStockTake";
            //string query = $@"select distinct CompanyCode FROM WMSStockTake WHERE CompanyCode = 'PU'";
            string connectionString = ConfigurationManager.ConnectionStrings["SqlConnectionString"].ConnectionString;
            SqlDataAccessLayer dal = new SqlDataAccessLayer(connectionString);

            DataTable dataTable = dal.ExecuteQuery(query);

            List<string> stringList = new List<string>();

            foreach (DataRow row in dataTable.Rows)
            {
                foreach (DataColumn column in dataTable.Columns)
                {
                    string value = row[column].ToString();
                    stringList.Add(value);
                }
            }

            return stringList;
        }

        private string GetCompanyCode(string companyCode)
        {
            string database = "";
            if (companyCode == "PU")
                database = "PU_LIVE_DB";
            else if (companyCode == "SAP")
                database = "SMELL_LIVE_DB";
            else if (companyCode == "FF")
                database = "FF_LIVE_DB";
            return database;
        }

        public void Process()
        {
            Logger.WriteLog("Process starts.");

            foreach (var companyCode in GetCompanyList())
            {
                Logger.WriteLog($"Company: {GetCompanyCode(companyCode)} starts");

                InventoryCounting inventoryCounting = GetEligibleList(companyCode);

                Task<bool> postTask = PostStockCounAsync(companyCode, inventoryCounting);
                var result = postTask.Result;
                Logger.WriteLog($"Company: {GetCompanyCode(companyCode)} ends");

            }

            Logger.WriteLog("Process ends.");
        }

        private async Task<bool> PostStockCounAsync(string companyCode, InventoryCounting model)
        {
            try
            {
                string database = GetCompanyCode(companyCode);

                var serviceLayer = new SLConnection(
                                        "https://saphana:50000/b1s/v1/",
                                        $"{database}",
                                        "Intercompany",
                                        "7891234");
                //var serviceLayer = ServiceLayer.Connection;
                var response = await serviceLayer
                               .Request("InventoryCountings")
                               .PostAsync<InventoryCounting>(model);
                Logger.WriteLog($"Inventory Couting posted succussfully.");
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Exception occures while posting inventory couting due to {ex.Message}");
            }

            return true;
        }

        private InventoryCounting GetEligibleList(string companyCode)
        {
            //synchronous
            var wmsList = GetWmsData(companyCode);
            var wmsItemCode = wmsList.Select(x => x.ItemCode).Distinct().ToList();
            var strItemCode = string.Join(",", wmsItemCode.Select(x => $"'{x}'"));

            var sapList = GetSapData(companyCode, strItemCode);

            var sapItemCode = sapList.Select(x => x.ItemCode).Distinct().ToList();

            var missingItemCodes = wmsItemCode.Except(sapItemCode).ToList();
            Logger.LogMissingItemCodes(companyCode, missingItemCodes);

            var validItemCodes = wmsItemCode.Intersect(sapItemCode).ToList();

            //First step
            List<WmsItem> eligibleList = new List<WmsItem>();
            eligibleList = wmsList.Where(x => validItemCodes.Contains(x.ItemCode)).ToList();


            // Second Step
            //List<WmsItem> PUItemList = new List<WmsItem>();
            //List<WmsItem> WebItemList = new List<WmsItem>();
            List<WmsItem> addedItemList = new List<WmsItem>();


            eligibleList.Where(x => x.ECommerce != 0 && companyCode == "PU").ToList().ForEach(x =>
           {
               int differenceQty = (int)(x.CountedQuantity - x.ECommerce);

               if (differenceQty > 0)
               {
                   addedItemList.Add(
                       new WmsItem()
                       {
                           ItemCode = x.ItemCode,
                           DocTime = x.DocTime,
                           ECommerce = x.ECommerce,
                           Warehouse = x.Warehouse,
                           CountedQuantity = differenceQty
                       });

                   addedItemList.Add(new WmsItem()
                   {
                       ItemCode = x.ItemCode,
                       DocTime = x.DocTime,
                       ECommerce = x.ECommerce,
                       Warehouse = "Web",
                       CountedQuantity = x.ECommerce
                   });
                   eligibleList.Remove(x);

               }
               else if (differenceQty == 0)
               {
                   addedItemList.Add(
                      new WmsItem()
                      {
                          ItemCode = x.ItemCode,
                          DocTime = x.DocTime,
                          ECommerce = x.ECommerce,
                          Warehouse = x.Warehouse,
                          CountedQuantity = 0
                      });

                   addedItemList.Add(new WmsItem()
                   {
                       ItemCode = x.ItemCode,
                       DocTime = x.DocTime,
                       ECommerce = x.ECommerce,
                       Warehouse = "Web",
                       CountedQuantity = x.ECommerce
                   });
                   eligibleList.Remove(x);
               }
           });

           
            var webItemList = addedItemList.Where(x => x.Warehouse == "Web").GroupBy(x => x.ItemCode).Select(y => new WmsItem()
            {
                ItemCode = y.Key,
                Warehouse = y.First().Warehouse,
                CountedQuantity = y.Sum(z => z.CountedQuantity),
                DocTime = y.First().DocTime,
            });
            addedItemList = addedItemList.Where(x => x.Warehouse != "Web").ToList();


            eligibleList.AddRange(addedItemList);
            eligibleList.AddRange(webItemList);

            // Third Step
            eligibleList = (from x in eligibleList
                            join y in sapList on new { x.ItemCode, x.Warehouse, x.CountedQuantity }
                                                equals new { y.ItemCode, Warehouse = y.WhsCode, CountedQuantity = y.OnHand }
                                                into matches
                            where !matches.Any()
                            select x).ToList();

            // List of those item which are in SAP which onhanad quantity greater than zero  not in sql list
            var strValidItem = string.Join(",", validItemCodes.Select(x => $"'{x}'"));
            List<SapItem> extraItemList = GetExtraItemList(companyCode, validItemCodes);

            addedItemList.Clear();
            foreach (var item in extraItemList)
            {
                addedItemList.Add(new WmsItem()
                {
                    ItemCode = item.ItemCode,
                    Warehouse = item.WhsCode,
                    CountedQuantity = 0,
                });
            }

            eligibleList.AddRange(addedItemList);

            InventoryCounting inventoryCounting = new InventoryCounting();

            if (eligibleList.Count >= 0)
            {
                DateTime docTime = eligibleList.First().DocTime;

                inventoryCounting = new InventoryCounting()
                {
                    CountDate = docTime.ToString("yyyy-MM-dd"),
                    CountTime = docTime.ToString("hh:mm:ss"),
                    Reference2 = $"ST{docTime.ToString("yyMMdd")}",
                    InventoryCountingLines = new List<InventoryCountingLine>()
                };

                List<InventoryCountingLine> lines = eligibleList.Select(x => new InventoryCountingLine()
                {
                    ItemCode = x.ItemCode,
                    CountedQuantity = x.CountedQuantity,
                    WarehouseCode = x.Warehouse,
                    Counted = "tYES"
                }).ToList();

                inventoryCounting.InventoryCountingLines.AddRange(lines);
            }
            return inventoryCounting;
        }

        private List<string> GetDistinctWhsCode(string companyCode)
        {
            string query = $@"Select distinct Warehouse FROM WMSStockTake where CompanyCode = '{companyCode}'";
            string connectionString = ConfigurationManager.ConnectionStrings["SqlConnectionString"].ConnectionString;
            SqlDataAccessLayer dal = new SqlDataAccessLayer(connectionString);

            DataTable dataTable = dal.ExecuteQuery(query);

            List<string> stringList = new List<string>();

            foreach (DataRow row in dataTable.Rows)
            {
                foreach (DataColumn column in dataTable.Columns)
                {
                    string value = row[column].ToString();
                    stringList.Add(value);
                }
            }

            return stringList;
        }

        //private async Task<List<WmsItem>> GetWmsDataAsync(string companyCode)
        //{
        //    string connectionString = ConfigurationManager.ConnectionStrings["SqlConnectionString"].ConnectionString;
        //    SqlDataAccessLayer dal = new SqlDataAccessLayer(connectionString);

        //    string query = $"SELECT * FROM WMSStockTake where CompanyCode = '{companyCode}'";
        //    //string query = "SELECT * FROM WMSStockTake WHERE ItemCode = 'YVES00094'";
        //    return await dal.ExecuteQueryAsync<List<WmsItem>>(query);
        //}

        private List<WmsItem> GetWmsData(string companyCode)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["SqlConnectionString"].ConnectionString;
            SqlDataAccessLayer dal = new SqlDataAccessLayer(connectionString);

            string query = $"SELECT [DocTime] ,[ItemCode] ,[CountedQuantity] ,[Warehouse] ,[ECommerce] FROM [dbo].[WMSStockTake] where companyCode = '{companyCode}'";
            //string query = $"SELECT [DocTime] ,[ItemCode] ,[CountedQuantity] ,[Warehouse] ,[ECommerce] FROM [dbo].[WMSStockTake] where companyCode = '{companyCode}' AND ItemCode = 'CHAN00055'";
            return dal.ExecuteQuery<List<WmsItem>>(query);
        }

        //private async Task<List<SapItem>> GetSapDataAsync(string companyCode)
        //{
        //    string database = "";
        //    if (companyCode == "PU")
        //        database = "PU_LIVE_DB";
        //    else if (companyCode == "SAP")
        //        database = "SMELL_LIVE_DB";
        //    else if (companyCode == "FF")
        //        database = "FF_LIVE_DB";

        //    string name = $"{database}_ConnectionString";

        //    string connectionString = ConfigurationManager.ConnectionStrings[name].ConnectionString;
        //    HanaDataAccessLayer dal = new HanaDataAccessLayer(connectionString);

        //    string query = $@"SELECT T0.""ItemCode"", T0.""WhsCode"", T0.""OnHand"" FROM OITW T0  Order By  T0.""ItemCode"", T0.""WhsCode""";
        //    //string query = $@"SELECT T0.""ItemCode"", T0.""WhsCode"", T0.""OnHand"" FROM OITW T0 WHERE T0.""ItemCode"" = 'YVES00094' Order By  T0.""ItemCode"", T0.""WhsCode""";
        //    return await dal.ExecuteQueryAsync<List<SapItem>>(query);
        //}

        private List<SapItem> GetSapData(string companyCode, string itemCodeList)
        {

            string database = GetCompanyCode(companyCode);
            string name = $"{database}_ConnectionString";

            string connectionString = ConfigurationManager.ConnectionStrings[name].ConnectionString;
            HanaDataAccessLayer dal = new HanaDataAccessLayer(connectionString);

            string query = $@"SELECT T0.""ItemCode"", T0.""WhsCode"", T0.""OnHand"" FROM OITW T0 INNER JOIN OITM T1 ON T0.""ItemCode"" = T1.""ItemCode"" WHERE T0.""ItemCode"" IN ({itemCodeList}) AND T1.""InvntItem"" = 'Y' Order By  T0.""ItemCode"", T0.""WhsCode""";
            //string query = $@"SELECT T0.""ItemCode"", T0.""WhsCode"", T0.""OnHand"" FROM OITW T0 INNER JOIN OITM T1 ON T0.""ItemCode"" = T1.""ItemCode"" WHERE T0.""ItemCode"" IN ({itemCodeList}) AND T1.""InvntItem"" = 'Y' AND T0.""ItemCode"" = 'CHAN00055' Order By  T0.""ItemCode"", T0.""WhsCode"" ";
            return dal.ExecuteQuery<List<SapItem>>(query);
        }

        private List<SapItem> GetExtraItemList(string companyCode, List<string> validItemCodes)
        {
            List<string> whsCodeList = GetDistinctWhsCode(companyCode);
            whsCodeList.Add("Web");

            string strWhsCode = string.Join(",", whsCodeList.Select(x => $"'{x}'"));

            string database = GetCompanyCode(companyCode);
            string name = $"{database}_ConnectionString";

            string connectionString = ConfigurationManager.ConnectionStrings[name].ConnectionString;
            HanaDataAccessLayer dal = new HanaDataAccessLayer(connectionString);


            var strItemCode = string.Join(",", validItemCodes.Select(x => $"'{x}'"));

            string query = $@"SELECT T0.""ItemCode"", T0.""WhsCode"", T0.""OnHand"" FROM OITW T0 INNER JOIN OITM T1 ON T0.""ItemCode"" = T1.""ItemCode"" WHERE T0.""ItemCode"" NOT IN ({strItemCode}) AND T0.""WhsCode"" IN ({strWhsCode}) AND T0.""OnHand"" > 0  AND T1.""InvntItem"" = 'Y' Order By  T0.""ItemCode"", T0.""WhsCode""";
            //string query = $@"SELECT T0.""ItemCode"", T0.""WhsCode"", T0.""OnHand"" FROM OITW T0 INNER JOIN OITM T1 ON T0.""ItemCode"" = T1.""ItemCode"" WHERE T0.""ItemCode"" NOT IN ({strItemCode}) AND T0.""WhsCode"" IN ({strWhsCode}) AND T0.""OnHand"" > 0  AND T1.""InvntItem"" = 'Y' AND T0.""ItemCode"" = 'CHAN00055' Order By  T0.""ItemCode"", T0.""WhsCode"" ";
            return dal.ExecuteQuery<List<SapItem>>(query);
        }
    }
}
