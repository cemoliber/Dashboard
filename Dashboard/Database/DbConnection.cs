
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashboardApp.Database
{
    public abstract class DbConnection
    {
        private readonly string connectionString;

        public DbConnection()
        {
            connectionString = "Data Source=DESKTOP-91IMOEI\\SQLEXPRESS;Initial Catalog=DatabaseDashboard;Integrated Security=True;Pooling=False";
            SqlCommand cma = new SqlCommand("insert into Customer values (@Id,@FirstName,@LastName,@City,@Country,@Phone)");
            SqlCommand cmb = new SqlCommand("insert into Order values (@Id,@OrderDate,@OrderNumber,@CustomerId,@TotalAmount)");
            SqlCommand cmc = new SqlCommand("insert into OrderItem values (@Id,@OrderId,@ProductId,@UnitPrice,@Quantity)");
            SqlCommand cmd = new SqlCommand("insert into Product values (@Id,@ProductName,@SupplierId,@UnitPrice,@Package,@Stock,@IsDiscontinued)");
            SqlCommand cme = new SqlCommand("insert into Supplier values (@Id,@CompanyName,@ContactName,@ContactTitle,@City,@Country,@Phone,@Fax)");
            
            
        }

        protected SqlConnection GetConnection()
        {
            return new SqlConnection(connectionString);
        }
    }
}