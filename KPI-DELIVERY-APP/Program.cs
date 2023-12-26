using KPI_DELIVERY_APP.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
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

public class ProductStatistics
{
    public int ProductId { get; set; }
    public decimal AveragePrice { get; set; }
    
}

public class Order
{
    public event Action<IProduct, string> DeliveryCompleted;

    public int OrderId { get; set; } // Додано для EF Core
    public virtual Customer Customer { get; set; }
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
    private static readonly object LockObject = new object();
    private static SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);
    private static int ProductCounter = 1;
    private static int ProductCounterTPL = 1;
    static void Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("C:\\Users\\ivank\\source\\repos\\KPI-DELIVERY-APP\\KPI-DELIVERY-APP\\appsettings.json", optional: false, reloadOnChange: true)
        .Build();

        var optionsBuilder = new DbContextOptionsBuilder<MyStoreDbContext>();
        optionsBuilder
            .UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
            .UseLazyLoadingProxies();

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

        using (var dbContext = new MyStoreDbContext(optionsBuilder.Options))
        {
            var expensiveProducts = dbContext.Products.Where(p => p.Price > 1000);
            var inStockProducts = dbContext.Products.Where(p => p.StockQuantity > 0);

            // Union
            var unionResult = expensiveProducts.Union(inStockProducts).ToList();

            // Except
            var exceptResult = expensiveProducts.Except(inStockProducts).ToList();

            // Intersect
            var intersectResult = expensiveProducts.Intersect(inStockProducts).ToList();

            var joinResult = dbContext.Orders
                 .Join(dbContext.Customers,
                       order => order.Customer.Email,
                       customer => customer.Email,
                       (order, customer) => new { CustomerName = customer.Name, OrderId = order.OrderId })
                 .ToList();

            var distinctProductNames = dbContext.Products.Select(p => p.Name).Distinct().ToList();

            var productGroups = dbContext.Products
                     .GroupBy(p => p.Price)
                     .Select(g => new { Price = g.Key, Count = g.Count() })
                     .ToList();

            var averagePriceProducts = dbContext.Products
                .GroupBy(p => p.StockQuantity)
                .Select(g => new { StockQuantity = g.Key, AveragePrice = g.Average(p => p.Price) })
                .ToList();

            var maxPriceProducts = dbContext.Products
                .GroupBy(p => p.StockQuantity)
                .Select(g => new { StockQuantity = g.Key, MaxPrice = g.Max(p => p.Price) })
                .ToList();
            //Стратегії Завантаження Зв'язаних Даних

            var ordersWithEagerLoading = dbContext.Orders.Include(o => o.Customer).ToList();//Eager Loading 

            var order = dbContext.Orders.FirstOrDefault(); //Explicit Loading
            dbContext.Entry(order).Reference(o => o.Customer).Load();

            //Lazy Loading
            var lazyLoadedOrder = dbContext.Orders.FirstOrDefault();
            var customerName = lazyLoadedOrder.Customer.Name; //Lazy loading автоматично завантажить virtual 

            //Середня вартість товару який в нас купували найчастіше

            var untrackedProducts = dbContext.Products.AsNoTracking().ToList();

            var product = untrackedProducts.First();
            product.Price = 2000;
            dbContext.Update(product);
            dbContext.SaveChanges();

            var productId = 1;
            var storedProcedureResult = dbContext.Products
                .FromSqlRaw("EXEC GetProductById @p0", productId)
                .ToList();

            var functionName = "CalculateProductStatistics";
            var functionResult = dbContext.ProductStatistics
                .FromSqlRaw($"SELECT * FROM {functionName}()")
                .ToList();




            var mostFrequentlyPurchasedProduct = dbContext.OrderItems
                .GroupBy(oi => oi.Product.ProductId)
                .OrderByDescending(g => g.Count())
                .Select(g => new { ProductId = g.Key, PurchaseCount = g.Count() })
                .FirstOrDefault();

            if (mostFrequentlyPurchasedProduct != null)
            {
                var averagePrice = dbContext.Products
                    .Where(p => p.ProductId == mostFrequentlyPurchasedProduct.ProductId)
                    .Average(p => p.Price);

            }
        }
        StartProductCreationThreads(10);
        AsyncMain(args).GetAwaiter().GetResult();

    }
    public static async Task AsyncMain(string[] args)
    {
        await StartProductCreationTasksAsync(10);
        await DisplayDataConcurrently();
    }

    private static void StartProductCreationThreads(int numberOfThreads)
    {
        var threads = new Thread[numberOfThreads];
        for (int i = 0; i < threads.Length; i++)
        {
            threads[i] = new Thread(() => CreateAndSaveProduct(new MyStoreDbContext(/* параметри */)));
            threads[i].Start();
        }

        foreach (var thread in threads)
        {
            thread.Join();
        }
    }

    private static void CreateAndSaveProduct(MyStoreDbContext dbContext)
    {
        var product = GenerateUniqueProduct();
        dbContext.Products.Add(product);
        dbContext.SaveChanges();
    }

    private static Product GenerateUniqueProduct()
    {
        lock (LockObject)
        {
            return new Product(
                $"product-{ProductCounter++}",
                100,                         
                "some description",              
                10                            
            );
        }
    }

    private static async Task StartProductCreationTasksAsync(int numberOfTasks)
    {
        var tasks = new List<Task>();
        for (int i = 0; i < numberOfTasks; i++)
        {
            tasks.Add(CreateAndSaveProductAsync(new MyStoreDbContext(/* параметри */)));
        }

        await Task.WhenAll(tasks);
    }

    private static async Task CreateAndSaveProductAsync(MyStoreDbContext dbContext)
    {
        var product = await GenerateUniqueProductAsync();
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();
    }

    private static async Task<Product> GenerateUniqueProductAsync()
    {
        await Semaphore.WaitAsync();
        try
        {
            return new Product(
                $"product-{ProductCounterTPL++}",
                100,
                "some desc",
                10
            );
        }
        finally
        {
            Semaphore.Release();
        }
    }

    private static async Task DisplayDataConcurrently()
    {
        var displayProductsTask = DisplayProductsAsync();
        var displayOrdersTask = DisplayOrdersAsync();

        await Task.WhenAll(displayProductsTask, displayOrdersTask);
    }

    private static async Task DisplayProductsAsync()
    {
        using (var dbContext = new MyStoreDbContext(/* параметри */))
        {
            var products = await dbContext.Products.ToListAsync();
            Console.WriteLine("Список усіх продуктів:");
            foreach (var product in products)
            {
                Console.WriteLine($"ID: {product.ProductId}, Назва: {product.Name}, Ціна: {product.Price}, Опис: {product.Description}, Кількість на складі: {product.StockQuantity}");
            }
        }
    }

    private static async Task DisplayOrdersAsync()
    {
        using (var dbContext = new MyStoreDbContext(/* параметри */))
        {
            var orders = await dbContext.Orders
                                        .Include(o => o.Customer)
                                        .Include(o => o.Items)
                                        .ThenInclude(oi => oi.Product)
                                        .ToListAsync();

            Console.WriteLine("\nСписок усіх замовлень:");
            foreach (var order in orders)
            {
                Console.WriteLine($"Замовлення ID: {order.OrderId}, Клієнт: {order.Customer.Name}, Електронна пошта: {order.Customer.Email}");
                foreach (var item in order.Items)
                {
                    Console.WriteLine($"\tПродукт: {item.Product.Name}, Кількість: {item.Quantity}");
                }
            }
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
