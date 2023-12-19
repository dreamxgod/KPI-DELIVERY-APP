using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace KPI_DELIVERY_APP.Data
{


    public class MyStoreDbContext : DbContext
    {
        public DbSet<ProductStatistics> ProductStatistics { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Customer> Customers { get; set; }

        public MyStoreDbContext(DbContextOptions<MyStoreDbContext> options) : base(options) { }
        public MyStoreDbContext() { }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Визначення ключів
            modelBuilder.Entity<Product>()
                .HasKey(p => p.ProductId);

            modelBuilder.Entity<Product>().HasData(
                new Product("Лаптоп", 1200, "15-дюймовий лаптоп", 10) { ProductId = 1 },
                new Product ("Смартфон", 500, "Android смартфон", 20) { ProductId = 2 }
            );

            modelBuilder.Entity<Order>()
                .HasKey(o => o.OrderId);

            modelBuilder.Entity<OrderItem>()
                .HasKey(oi => oi.OrderItemId);
            
            modelBuilder.Entity<Order>()
                .HasMany(o => o.Items)
                .WithOne()
                .HasForeignKey("OrderId");


            modelBuilder.Entity<Customer>()
            .HasOne(c => c.CustomerProfile)
            .WithOne(cp => cp.Customer)
            .HasForeignKey<CustomerProfile>(cp => cp.CustomerEmail);

        }
    }



}
