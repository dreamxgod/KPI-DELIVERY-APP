using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KPI_DELIVERY_APP.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace KPI_DELIVERY_APP
{
    

    public class SampleContextFactory : IDesignTimeDbContextFactory<MyStoreDbContext>
    {
        public MyStoreDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<MyStoreDbContext>();

            // получаем конфигурацию из файла appsettings.json
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory());
            builder.AddJsonFile("appsettings.json");
            IConfigurationRoot config = builder.Build();

            // получаем строку подключения из файла appsettings.json
            string connectionString = config.GetConnectionString("DefaultConnection");
            optionsBuilder.UseSqlServer(connectionString);
            return new MyStoreDbContext(optionsBuilder.Options);
        }
    }
}
