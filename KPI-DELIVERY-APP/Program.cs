﻿using KPI_DELIVERY_APP.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;

// Оновлення інтерфейсу IProduct для включення ProductId

public interface IProduct
{
    int ProductId { get; set; }
    string Name { get; set; }
    decimal Price { get; set; }
    string Description { get; set; }
    int StockQuantity { get; set; }
}

// Інші інтерфейси залишаються без змін
public interface IDeliveryService
{
    void Deliver(IProduct product, string address);
}

public interface IStore
{
    void AddOrder(Order order);
    void AddProductToInventory(IProduct product, int quantity);
    Order GetOrder(int orderId);
}

// Додавання ProductId до класу Product
public class Product : IProduct
{
    public int ProductId { get; set; } // Додано для EF Core
    public string Name { get; set; }
    public decimal Price { get; set; }
    public string Description { get; set; }
    public int StockQuantity { get; set; }

    public Product(string name, decimal price, string description, int stockQuantity)
    {
        Name = name;
        Price = price;
        Description = description;
        StockQuantity = stockQuantity;
    }
}


public class DeliveryService : IDeliveryService
{
    public string CompanyName { get; set; }

    public DeliveryService(string companyName)
    {
        CompanyName = companyName;
    }

    public void Deliver(IProduct product, string address)
    {
        Console.WriteLine($"Доставка {product.Name} від {CompanyName} за адресою {address}");
    }
}

public class Customer
{
    [Required]
    public string Name { get; set; }

    [Key]
    [StringLength(100)]
    public string Email { get; set; }

    public CustomerProfile CustomerProfile { get; set; }

    public Customer(string name, string email)
    {
        Name = name;
        Email = email;
    }
}

public class CustomerProfile
{
    public int CustomerProfileId { get; set; }
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }

    // Зовнішній ключ для Customer
    public string? CustomerEmail { get; set; }

    // Навігаційна властивість для Customer
    public Customer Customer { get; set; }
}

// Додавання ідентифікаторів до OrderItem
public class OrderItem
{
    public int OrderItemId { get; set; } // Додано для EF Core
    public Product Product { get; set; }
    public int Quantity { get; set; }

    public OrderItem() { }

    public OrderItem(Product product, int quantity)
    {
        Product = product;
        Quantity = quantity;
    }
}

public class Order
{
    public event Action<IProduct, string> DeliveryCompleted;

    public int OrderId { get; set; } // Додано для EF Core
    public Customer Customer { get; set; }
    public List<OrderItem> Items { get; set; }
    public bool IsDelivered { get; private set; }

    private IDeliveryService deliveryService;

    public Order() { }

    public Order(Customer customer, int orderId, IDeliveryService deliveryService)
    {
        Customer = customer;
        Items = new List<OrderItem>();
        IsDelivered = false;
        OrderId = orderId;
        this.deliveryService = deliveryService;
    }

    public void AddOrderItem(Product product, int quantity)
    {
        Items.Add(new OrderItem(product, quantity));
    }

    public void Deliver()
    {
        foreach (var orderItem in Items)
        {
            try
            {
                deliveryService.Deliver(orderItem.Product, Customer.Email);
                DeliveryCompleted?.Invoke(orderItem.Product, Customer.Email);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка доставки: {ex.Message}");
            }
        }

        IsDelivered = true;
    }
}

public class Store : IStore
{
    private Dictionary<int, Order> orders = new Dictionary<int, Order>();
    private Dictionary<IProduct, int> inventory = new Dictionary<IProduct, int>();

    public void AddOrder(Order order)
    {
        orders[order.OrderId] = order;
    }

    public void AddProductToInventory(IProduct product, int quantity)
    {
        if (inventory.ContainsKey(product))
        {
            inventory[product] += quantity;
        }
        else
        {
            inventory[product] = quantity;
        }
    }

    public Order GetOrder(int orderId)
    {
        if (orders.ContainsKey(orderId))
        {
            return orders[orderId];
        }
        return null;
    }
}

public class OrderManager<TProduct, TStore, TDeliveryService>
    where TProduct : IProduct
    where TStore : IStore
    where TDeliveryService : IDeliveryService
{
    private TStore store;
    private TDeliveryService deliveryService;

    public OrderManager(TStore store, TDeliveryService deliveryService)
    {
        this.store = store;
        this.deliveryService = deliveryService;
    }

    public void ProcessOrder(Customer customer, List<OrderItem> orderItems)
    {
        var orderId = Guid.NewGuid().GetHashCode();
        var order = new Order(customer, orderId, deliveryService);

        foreach (var orderItem in orderItems)
        {
            order.AddOrderItem(orderItem.Product, orderItem.Quantity);
        }

        store.AddOrder(order);
        order.Deliver(); // Викликаємо метод доставки замовлення

        if (store.GetOrder(order.OrderId).IsDelivered)
        {
            Console.WriteLine($"Замовлення для {customer.Name} доставлено успішно.");
        }
        else
        {
            Console.WriteLine($"Замовлення для {customer.Name} не доставлено.");
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("C:\\Users\\ivank\\source\\repos\\KPI-DELIVERY-APP\\KPI-DELIVERY-APP\\appsettings.json", optional: false, reloadOnChange: true)
        .Build();

        var optionsBuilder = new DbContextOptionsBuilder<MyStoreDbContext>();
        optionsBuilder.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));

        using (var dbContext = new MyStoreDbContext(optionsBuilder.Options))
        {
            // Створення нового продукту
            var newProduct = new Product("Ноутбук", 1500, "Новий ноутбук", 5);
            dbContext.Products.Add(newProduct);
            dbContext.SaveChanges();

            // Зміна існуючого продукту
            var existingProduct = dbContext.Products.FirstOrDefault(p => p.ProductId == 1);
            if (existingProduct != null)
            {
                existingProduct.Price = 1100; // Зміна ціни
                dbContext.SaveChanges();
            }

            // Видалення продукту
            var productToDelete = dbContext.Products.FirstOrDefault(p => p.ProductId == 2);
            if (productToDelete != null)
            {
                dbContext.Products.Remove(productToDelete);
                dbContext.SaveChanges();
            }
        }

        using (var dbContext = new MyStoreDbContext(optionsBuilder.Options))
        {
            ProcessSampleOrder(dbContext);
        }
    }

    static void ProcessSampleOrder(MyStoreDbContext dbContext)
    {
        var customer = new Customer("Іван Петров", "ivan@example.com");
        var deliveryService = new DeliveryService("ExpressDelivery");
        var store = new Store();

        store.AddProductToInventory(new Product("Лаптоп", 1200, "15-дюймовий лаптоп", 10), 10);
        store.AddProductToInventory(new Product("Смартфон", 500, "Android смартфон", 20), 20);

        var orderManager = new OrderManager<Product, Store, DeliveryService>(store, deliveryService);
        var orderItems = new List<OrderItem>
        {
            new OrderItem(new Product("Лаптоп", 1200, "15-дюймовий лаптоп", 10), 2),
            new OrderItem(new Product("Смартфон", 500, "Android смартфон", 20), 3)
        };

        orderManager.ProcessOrder(customer, orderItems);
    }
}
