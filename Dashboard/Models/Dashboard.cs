using DashboardApp.Models;
using Microsoft.Azure.Amqp.Transaction;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashboardApp.Models
{

    public struct RevenueByDate
    {
        public string Date { get; set; }//general date
        public decimal TotalAmount { get; set; }
    }

    public class Dashboard : Database.DbConnection
    {
        private DateTime startDate;
        private DateTime endDate;
        private int numberDays;

        public int NumCustomers { get; private set; } //müşteri sayısı
        public int NumSuppliers { get; private set; }//teddarikçi sayısı
        public int NumProducts { get; private set; }//ürün sayısı
        public List<KeyValuePair<string, int>> TopProductsList { get; private set; }//ürün listesi
        public List<KeyValuePair<string, int>> UnderstockList { get; private set; }//stok listesi
        public List<RevenueByDate> GrossRevenueList { get; private set; }//brüt gelir
        public int NumOrders { get; set; }//sipariş mikatarı
        public decimal TotalRevenue { get; set; }//total gelir
        public decimal TotalProfit { get; set; }//toplam kar

        //Constructor
        public Dashboard()
        {

        }


        private void GetNumberItems()
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = new SqlCommand())
                {

                    command.Parameters.Add("@fromDate", System.Data.SqlDbType.DateTime).Value = startDate;
                    command.Parameters.Add("@toDate", System.Data.SqlDbType.DateTime).Value = endDate;

                    command.Connection = connection;

                    //Get total number of customers
                    command.CommandText = "select count(Id) from Customer";
                    NumCustomers = (int)command.ExecuteScalar();

                    //Get Total Number of Suppliers
                    command.CommandText = "select count(Id) from Supplier";
                    NumSuppliers = (int)command.ExecuteScalar();

                    //Get Total Number of Products
                    command.CommandText = "select count(Id) from Product";
                    NumProducts = (int)command.ExecuteScalar();

                    //Get Total Number of Orders
                    command.CommandText = @"select count(Id) from [Order]";
                    
                    NumOrders = (int)command.ExecuteScalar();
                }
            }
        }
        private void GetOrderAnalisys()
        {

            GrossRevenueList = new List<RevenueByDate>();
            TotalProfit = 0;
            TotalRevenue = 0;

            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = new SqlCommand())
                {
                    DateTime @fromDate = new DateTime(2020, 07, 04, 0, 0, 0);
                    DateTime @toDate = new DateTime(2020, 08, 04, 0, 0, 0);

                    command.Parameters.Add("@fromDate", System.Data.SqlDbType.DateTime).Value = startDate;
                    command.Parameters.Add("@toDate", System.Data.SqlDbType.DateTime).Value = endDate;

                    command.Connection = connection;
                    command.CommandText = @"select OrderDate,sum(TotalAmount)
                                            from [Order]
                                            group by OrderDate";

                    var reader = command.ExecuteReader();
                    var resultTable = new List<KeyValuePair<DateTime, decimal>>();
                    while (reader.Read())
                    {
                        resultTable.Add(
                            new KeyValuePair<DateTime, decimal>((DateTime)reader[0], (decimal)reader[1])
                            );
                        TotalRevenue += (decimal)reader[1];
                    }
                    TotalProfit = TotalRevenue * 0.2m;//20%
                    reader.Close();

                    //Group by Hours
                    if (numberDays <= 1)
                    {
                        GrossRevenueList = (from orderList in resultTable
                                            group orderList by orderList.Key.ToString("hh tt")
                                           into order
                                            select new RevenueByDate
                                            {
                                                Date = order.Key,
                                                TotalAmount = order.Sum(amount => amount.Value)
                                            }).ToList();
                    }
                    //Group by Days
                    else if (numberDays <= 30)
                    {
                        GrossRevenueList = (from orderList in resultTable
                                            group orderList by orderList.Key.ToString("dd MMM")
                                           into order
                                            select new RevenueByDate
                                            {
                                                Date = order.Key,
                                                TotalAmount = order.Sum(amount => amount.Value)
                                            }).ToList();
                    }

                    //Group by Weeks
                    else if (numberDays <= 92)
                    {
                        GrossRevenueList = (from orderList in resultTable
                                            group orderList by CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                                                orderList.Key, CalendarWeekRule.FirstDay, DayOfWeek.Monday)
                                           into order
                                            select new RevenueByDate
                                            {
                                                Date = "Week " + order.Key.ToString(),
                                                TotalAmount = order.Sum(amount => amount.Value)
                                            }).ToList();
                    }

                    //Group by Months
                    else if (numberDays <= (365 * 2))
                    {
                        bool isYear = numberDays <= 365 ? true : false;
                        GrossRevenueList = (from orderList in resultTable
                                            group orderList by orderList.Key.ToString("MMM yyyy")
                                           into order
                                            select new RevenueByDate
                                            {
                                                Date = isYear ? order.Key.Substring(0, order.Key.IndexOf(" ")) : order.Key,
                                                TotalAmount = order.Sum(amount => amount.Value)
                                            }).ToList();
                    }

                    //Group by Years
                    else
                    {
                        GrossRevenueList = (from orderList in resultTable
                                            group orderList by orderList.Key.ToString("yyyy")
                                           into order
                                            select new RevenueByDate
                                            {
                                                Date = order.Key,
                                                TotalAmount = order.Sum(amount => amount.Value)
                                            }).ToList();
                    }
                }
            }
        }

        private void GetProductAnalysis()
        {
            TopProductsList = new List<KeyValuePair<string, int>>();
            UnderstockList = new List<KeyValuePair<string, int>>();
            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = new SqlCommand())
                {
                    SqlDataReader reader;
                    command.Connection = connection;
                    //Get Top 5 products
                    command.CommandText = @"select top 5 P.ProductName, SUM(OrderItem.Quantity) AS Q
                                            from OrderItem
                                            inner join Product P on P.Id = OrderItem.ProductId
                                            inner join [Order] O on O.Id = OrderItem.OrderId
                                            GROUP BY P.ProductName
                                            order by Q desc ";
                    command.Parameters.Add("@fromDate", System.Data.SqlDbType.DateTime).Value = startDate;
                    command.Parameters.Add("@toDate", System.Data.SqlDbType.DateTime).Value = endDate;
                    reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        TopProductsList.Add(
                            new KeyValuePair<string, int>(reader[0].ToString(), (int)reader[1]));
                    }
                    reader.Close();

                    //Get Understock
                    command.CommandText = @"SELECT ProductName , stock
                                            from Product
                                            where stock <= 6 and IsDiscontinued = 0";
                    reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        UnderstockList.Add(
                            new KeyValuePair<string, int>(reader[0].ToString(), (int)reader[1]));
                    }
                    reader.Close();
                }
            }
        }


        //public methods
        public bool LoadData(DateTime startDate, DateTime endDate)
        {
            endDate = new DateTime(endDate.Year, endDate.Month, endDate.Day,
                endDate.Hour, endDate.Minute, 59);
            if (startDate != this.startDate || endDate != this.endDate)
            {
                this.startDate = startDate;
                this.endDate = endDate;
                this.numberDays = (endDate - startDate).Days;

                GetNumberItems();
                GetProductAnalysis();
                GetOrderAnalisys();
                Console.WriteLine("Refrehed data: {0} - {1}", startDate.ToString(), endDate.ToString());

                return true;
            }
            else
            {
                Console.WriteLine("Data not refreshed , same query: {0} - {1}",startDate.ToString(),endDate.ToString());
                return false;
            }

        }

    }
}