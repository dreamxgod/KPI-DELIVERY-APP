using System;
using System.Collections.Generic;

public interface IProduct
{
    string Name { get; }
    decimal Price { get; }
    string Description { get; }
    int StockQuantity { get; }
}

public interface IDeliveryService
{
    void Deliver(IProduct product, string address);
}

public class Product : IProduct
{
    public string Name { get; }
    public decimal Price { get; }
    public string Description { get; }
    public int StockQuantity { get; }

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
    public string CompanyName { get; }

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
    public string Name { get; }
    public string Email { get; }

    public Customer(string name, string email)
    {
        Name = name;
        Email = email;
    }
}

public class OrderItem
{
    public IProduct Product { get; }
    public int Quantity { get; }

    public OrderItem(IProduct product, int quantity)
    {
        Product = product;
        Quantity = quantity;
    }
}

public class Order
{
    public event Action<IProduct, string> DeliveryCompleted;

    public Customer Customer { get; }
    public List<OrderItem> Items { get; }
    public bool IsDelivered { get; private set; }

    public Order(Customer customer)
    {
        Customer = customer;
        Items = new List<OrderItem>();
        IsDelivered = false;
    }

    public void AddOrderItem(IProduct product, int quantity)
    {
        Items.Add(new OrderItem(product, quantity));
    }

    public void Deliver(IDeliveryService deliveryService)
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

public class OrderManager<TProduct> where TProduct : IProduct
{
    public void ProcessOrder(Customer customer, List<OrderItem> orderItems, IDeliveryService deliveryService)
    {
        var order = new Order(customer);

        foreach (var orderItem in orderItems)
        {
            order.AddOrderItem(orderItem.Product, orderItem.Quantity);
        }

        order.DeliveryCompleted += (p, email) => Console.WriteLine($"Замовлення {p.Name} доставлено на {email}");
        order.Deliver(deliveryService);

        if (order.IsDelivered)
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
        var customer = new Customer("Іван Петров", "ivan@example.com");
        var deliveryService = new DeliveryService("ExpressDelivery");

        var orderManager = new OrderManager<Product>();
        var orderItems = new List<OrderItem>
        {
            new OrderItem(new Product("Лаптоп", 1200, "15-дюймовий лаптоп", 10), 2),
            new OrderItem(new Product("Смартфон", 500, "Android смартфон", 20), 3)
        };

        orderManager.ProcessOrder(customer, orderItems, deliveryService);
    }
}
