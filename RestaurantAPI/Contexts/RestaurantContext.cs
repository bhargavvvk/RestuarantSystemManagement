using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Models;

namespace RestaurantAPI.Contexts;

public class RestaurantContext: DbContext
{
    public RestaurantContext(DbContextOptions options): base(options)
    {
    }
    public DbSet<User> Users { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<RestaurantTable> RestaurantTables { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<MenuItem> MenuItems { get; set; }
    public DbSet<DiningSession> DiningSessions { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<CustomerRequest> CustomerRequests { get; set; }
    public DbSet<Bill> Bills { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<TaxConfiguration> TaxConfigurations { get; set; }
    public DbSet<InventoryItem> InventoryItems { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(user =>
        {
            user.HasKey(u => u.Id).HasName("PK_User");

            user.Property(u => u.Role)
                .HasConversion<string>();

            user.HasIndex(u => u.Username)
                .IsUnique();

            user.Property(u => u.IsDeleted)
                .HasDefaultValue(false);

            user.HasIndex(u => u.MobileNumberHash)
                .IsUnique();

            user.Property(u => u.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");

            user.HasData(
                new User
                {
                    Id = 1,
                    Username = "admin",
                    Name = "System Admin",
                    EncryptedMobileNumber = "ADMIN_ENCRYPTED",
                    MobileNumberHash = "ADMIN_HASH",
                    IsActive = true,
                    IsDeleted = false,
                    PasswordHash = new byte[] { 1, 2, 3 },
                    Role = UserRole.Admin
                },
                new User
                {
                    Id = 2,
                    Username = "kitchen",
                    Name = "Kitchen Staff",
                    EncryptedMobileNumber = "KITCHEN_ENCRYPTED",
                    MobileNumberHash = "KITCHEN_HASH",
                    IsActive = true,
                    IsDeleted = false,
                    PasswordHash = new byte[] { 1, 2, 3 },
                    Role = UserRole.KitchenStaff
                },
                new User
                {
                    Id = 3,
                    Username = "waiter1",
                    Name = "Ramesh",
                    EncryptedMobileNumber = "WAITER1_ENCRYPTED",
                    MobileNumberHash = "WAITER1_HASH",
                    IsActive = true,
                    IsDeleted = false,
                    PasswordHash = new byte[] { 1, 2, 3 },
                    Role = UserRole.Waiter
                },
                new User
                {
                    Id = 4,
                    Username = "waiter2",
                    Name = "Suresh",
                    EncryptedMobileNumber = "WAITER2_ENCRYPTED",
                    MobileNumberHash = "WAITER2_HASH",
                    IsActive = true,
                    IsDeleted = false,
                    PasswordHash = new byte[] { 1, 2, 3 },
                    Role = UserRole.Waiter
                }
            );
        });
        modelBuilder.Entity<Customer>(customer =>
        {
            customer.HasKey(c => c.Id)
                .HasName("PK_Customer");

            customer.HasIndex(c => c.MobileNumberHash)
                .IsUnique();

            customer.Property(c => c.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
        });
        modelBuilder.Entity<RestaurantTable>(table =>
        {
            table.HasKey(t => t.Id)
                .HasName("PK_RestaurantTable");

            table.HasIndex(t => t.TableNumber)
                .IsUnique();

            table.HasIndex(t => t.QrIdentifier)
                .IsUnique();

            table.Property(t => t.Capacity)
                .IsRequired();

            table.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_RestaurantTable_Capacity",
                    "\"Capacity\" > 0");
            });

            table.Property(t => t.Status)
                .HasConversion<string>()
                .HasDefaultValue(TableStatus.Available);

            table.Property(t => t.IsDeleted)
                .HasDefaultValue(false);

            table.Property(t => t.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");


            table.HasData(
                new RestaurantTable
                {
                    Id = 1,
                    TableNumber = "T1",
                    QrIdentifier = "TBL_001",
                    Capacity = 4,
                    AssignedWaiterId = 3,
                    Status = TableStatus.Available,
                    IsDeleted = false
                },
                new RestaurantTable
                {
                    Id = 2,
                    TableNumber = "T2",
                    QrIdentifier = "TBL_002",
                    Capacity = 4,
                    AssignedWaiterId = 3,
                    Status = TableStatus.Available,
                    IsDeleted = false
                },
                new RestaurantTable
                {
                    Id = 3,
                    TableNumber = "T3",
                    QrIdentifier = "TBL_003",
                    Capacity = 6,
                    AssignedWaiterId = 4,
                    Status = TableStatus.Available,
                    IsDeleted = false
                },
                new RestaurantTable
                {
                    Id = 4,
                    TableNumber = "T4",
                    QrIdentifier = "TBL_004",
                    Capacity = 2,
                    AssignedWaiterId = 4,
                    Status = TableStatus.Available,
                    IsDeleted = false
                },
                new RestaurantTable
                {
                    Id = 5,
                    TableNumber = "T5",
                    QrIdentifier = "TBL_005",
                    Capacity = 8,
                    AssignedWaiterId = 3,
                    Status = TableStatus.Available,
                    IsDeleted = false
                }
            );
        });
        modelBuilder.Entity<Category>(category =>
        {
            category.HasKey(c => c.Id)
                .HasName("PK_Category");

            category.HasIndex(c => c.Name)
                .IsUnique();

            category.Property(c => c.IsAvailable)
                .HasDefaultValue(true);

            category.Property(c => c.IsDeleted)
                .HasDefaultValue(false);

            category.Property(c => c.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");

            category.HasData(
                new Category
                {
                    Id = 1,
                    Name = "Starters",
                    Description = "Appetizers",
                    IsAvailable = true,
                    IsDeleted = false
                },
                new Category
                {
                    Id = 2,
                    Name = "Main Course",
                    Description = "Main dishes",
                    IsAvailable = true,
                    IsDeleted = false
                },
                new Category
                {
                    Id = 3,
                    Name = "Desserts",
                    Description = "Sweet dishes",
                    IsAvailable = true,
                    IsDeleted = false
                },
                new Category
                {
                    Id = 4,
                    Name = "Beverages",
                    Description = "Drinks",
                    IsAvailable = true,
                    IsDeleted = false
                }
            );
        });
        modelBuilder.Entity<MenuItem>(item =>
        {
            item.HasKey(m => m.Id)
                .HasName("PK_MenuItem");

            item.Property(m => m.Price)
                .HasColumnType("numeric(10,2)");

            item.Property(m => m.FoodType)
                .HasConversion<string>();

            item.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_MenuItem_Price",
                    "\"Price\" >= 0");
            });

            item.Property(m => m.IsAvailable)
                .HasDefaultValue(true);

            item.Property(m => m.IsDeleted)
                .HasDefaultValue(false);

            item.Property(m => m.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");

            item.HasOne(m => m.Category)
                .WithMany(c => c.MenuItems)
                .HasForeignKey(m => m.CategoryId)
                .HasConstraintName("FK_MenuItem_Category")
                .OnDelete(DeleteBehavior.Restrict);

            item.HasData(
                new MenuItem
                {
                    Id = 1,
                    CategoryId = 1,
                    Name = "Paneer Tikka",
                    Price = 220m,
                    FoodType = FoodType.Veg,
                    IsAvailable = true,
                    IsDeleted = false
                },
                new MenuItem
                {
                    Id = 2,
                    CategoryId = 1,
                    Name = "Chicken 65",
                    Price = 260m,
                    FoodType = FoodType.NonVeg,
                    IsAvailable = true,
                    IsDeleted = false
                },
                new MenuItem
                {
                    Id = 3,
                    CategoryId = 2,
                    Name = "Veg Biryani",
                    Price = 180m,
                    FoodType = FoodType.Veg,
                    IsAvailable = true,
                    IsDeleted = false
                },
                new MenuItem
                {
                    Id = 4,
                    CategoryId = 2,
                    Name = "Chicken Biryani",
                    Price = 250m,
                    FoodType = FoodType.NonVeg,
                    IsAvailable = true,
                    IsDeleted = false
                },
                new MenuItem
                {
                    Id = 5,
                    CategoryId = 2,
                    Name = "Butter Naan",
                    Price = 40m,
                    FoodType = FoodType.Veg,
                    IsAvailable = true,
                    IsDeleted = false
                },
                new MenuItem
                {
                    Id = 6,
                    CategoryId = 2,
                    Name = "Paneer Butter",
                    Price = 210m,
                    FoodType = FoodType.Veg,
                    IsAvailable = true,
                    IsDeleted = false
                },
                new MenuItem
                {
                    Id = 7,
                    CategoryId = 3,
                    Name = "Brownie",
                    Price = 120m,
                    FoodType = FoodType.Veg,
                    IsAvailable = true,
                    IsDeleted = false
                },
                new MenuItem
                {
                    Id = 8,
                    CategoryId = 3,
                    Name = "Ice Cream",
                    Price = 90m,
                    FoodType = FoodType.Veg,
                    IsAvailable = true,
                    IsDeleted = false
                },
                new MenuItem
                {
                    Id = 9,
                    CategoryId = 4,
                    Name = "Coke",
                    Price = 50m,
                    FoodType = FoodType.Veg,
                    IsAvailable = true,
                    IsDeleted = false
                },
                new MenuItem
                {
                    Id = 10,
                    CategoryId = 4,
                    Name = "Lemon Soda",
                    Price = 60m,
                    FoodType = FoodType.Veg,
                    IsAvailable = true,
                    IsDeleted = false
                }
            );
        });
        modelBuilder.Entity<DiningSession>(session =>
        {
            session.HasKey(ds => ds.Id)
                .HasName("PK_DiningSession");

            session.HasIndex(ds => ds.SessionOtp)
                .IsUnique();

            session.Property(ds => ds.Status)
                .HasConversion<string>()
                .HasDefaultValue(DiningSessionStatus.Active);

            session.Property(ds => ds.StartedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");

            session.Property(ds => ds.EndedAt)
                .HasColumnType("timestamp without time zone");

            session.HasOne(ds => ds.Table)
                .WithMany(t => t.DiningSessions)
                .HasForeignKey(ds => ds.TableId)
                .HasConstraintName("FK_DiningSession_Table")
                .OnDelete(DeleteBehavior.Restrict);

            session.HasOne(ds => ds.Customer)
                .WithMany(c => c.DiningSessions)
                .HasForeignKey(ds => ds.CustomerId)
                .HasConstraintName("FK_DiningSession_Customer")
                .OnDelete(DeleteBehavior.Restrict);

            session.HasOne(ds => ds.Waiter)
                .WithMany(u => u.DiningSessions)
                .HasForeignKey(ds => ds.WaiterId)
                .HasConstraintName("FK_DiningSession_User")
                .OnDelete(DeleteBehavior.Restrict);

            session.HasIndex(ds => ds.TableId)
                .HasDatabaseName("UQ_Active_Table_Session")
                .IsUnique()
                .HasFilter("\"Status\" = 'Active'");
        });
        modelBuilder.Entity<Cart>(cart =>
        {
            cart.HasKey(c => c.Id)
                .HasName("PK_Cart");

            cart.Property(c => c.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");

            cart.HasIndex(c => c.DiningSessionId)
                .IsUnique();

            cart.HasOne(c => c.DiningSession)
                .WithOne(ds => ds.Cart)
                .HasForeignKey<Cart>(c => c.DiningSessionId)
                .HasConstraintName("FK_Cart_DiningSession")
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<CartItem>(cartItem =>
        {
            cartItem.HasKey(ci => ci.Id)
                .HasName("PK_CartItem");
            cartItem.HasIndex(ci => new
            {
                ci.CartId,
                ci.MenuItemId
            })
            .IsUnique();
            cartItem.ToTable(t =>
            {
                t.HasCheckConstraint("CK_CartItem_Quantity",
                    "\"Quantity\" > 0");
            });

            cartItem.HasOne(ci => ci.Cart)
                .WithMany(c => c.CartItems)
                .HasForeignKey(ci => ci.CartId)
                .HasConstraintName("FK_CartItem_Cart")
                .OnDelete(DeleteBehavior.Cascade);

            cartItem.HasOne(ci => ci.MenuItem)
                .WithMany(mi => mi.CartItems)
                .HasForeignKey(ci => ci.MenuItemId)
                .HasConstraintName("FK_CartItem_MenuItem")
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Order>(order =>
        {
            order.HasKey(o => o.Id)
                .HasName("PK_Order");

            order.Property(o => o.TotalAmount)
                .HasColumnType("numeric(10,2)");
            order.HasIndex(o => o.OrderNumber)
                .IsUnique();

            order.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Order_TotalAmount",
                    "\"TotalAmount\" >= 0");
            });

            order.Property(o => o.PlacedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");

            order.Property(o => o.CancelledAt)
                .HasColumnType("timestamp without time zone");

            order.HasOne(o => o.DiningSession)
                .WithMany(ds => ds.Orders)
                .HasForeignKey(o => o.DiningSessionId)
                .HasConstraintName("FK_Order_DiningSession")
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<OrderItem>(orderItem =>
        {
            orderItem.HasKey(oi => oi.Id)
                .HasName("PK_OrderItem");


            orderItem.Property(oi => oi.ItemPrice)
                .HasColumnType("numeric(10,2)");
            orderItem.HasIndex(oi => new
            {
                oi.OrderId,
                oi.MenuItemId
            })
            .IsUnique();

            orderItem.ToTable(t =>
            {
                t.HasCheckConstraint("CK_OrderItem_Quantity",
                    "\"Quantity\" > 0");

                t.HasCheckConstraint("CK_OrderItem_ItemPrice",
                    "\"ItemPrice\" >= 0");
            });
            orderItem.Property(o=>o.Status)
                .HasConversion<string>()
                .HasDefaultValue(OrderItemStatus.Placed);
            orderItem.HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .HasConstraintName("FK_OrderItem_Order")
                .OnDelete(DeleteBehavior.Cascade);

            orderItem.HasOne(oi => oi.MenuItem)
                .WithMany(mi => mi.OrderItems)
                .HasForeignKey(oi => oi.MenuItemId)
                .HasConstraintName("FK_OrderItem_MenuItem")
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<CustomerRequest>(request =>
        {
            request.HasKey(cr => cr.Id)
                .HasName("PK_CustomerRequest");

            request.Property(cr => cr.RequestType)
                .HasConversion<string>();
            request.Property(cr => cr.Status)
                .HasConversion<string>()
                .HasDefaultValue(CustomerRequestStatus.Pending);

            request.Property(cr => cr.RequestedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");

            request.Property(cr => cr.CompletedAt)
                .HasColumnType("timestamp without time zone");

            request.HasOne(cr => cr.DiningSession)
                .WithMany(ds => ds.CustomerRequests)
                .HasForeignKey(cr => cr.DiningSessionId)
                .HasConstraintName("FK_CustomerRequest_DiningSession")
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Bill>(bill =>
        {
            bill.HasKey(b => b.Id)
                .HasName("PK_Bill");
            bill.HasIndex(b => b.BillNumber)
                .IsUnique();
            bill.Property(b => b.FoodTotal)
                .HasColumnType("numeric(10,2)");
            bill.Property(b => b.GrandTotal)
                .HasColumnType("numeric(10,2)");
            bill.Property(b => b.ServiceChargeAmount)
                .HasColumnType("numeric(10,2)");
            bill.Property(b => b.SgstAmount)
                .HasColumnType("numeric(10,2)");
            bill.Property(b => b.CgstAmount)
                .HasColumnType("numeric(10,2)");

            bill.ToTable(t =>
            {
                t.HasCheckConstraint("CK_FoodTotal",
                    "\"FoodTotal\" >= 0");

                t.HasCheckConstraint("CK_GrandTotal",
                    "\"GrandTotal\" >= 0");

                t.HasCheckConstraint("CK_ServiceChargeAmount",
                    "\"ServiceChargeAmount\" >= 0");

                t.HasCheckConstraint("CK_SgstAmount",
                    "\"SgstAmount\" >= 0");

                t.HasCheckConstraint("CK_CgstAmount",
                    "\"CgstAmount\" >= 0");
            });
            bill.Property(b => b.PaymentStatus)
                .HasConversion<string>()
                .HasDefaultValue(PaymentStatus.Pending);

            bill.Property(b => b.PaymentMethod)
                .HasConversion<string>();

            bill.Property(b => b.GeneratedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");

            bill.Property(b => b.PaidAt)
                .HasColumnType("timestamp without time zone");

            bill.HasIndex(b => b.DiningSessionId)
                .IsUnique();

            bill.HasOne(b => b.DiningSession)
                .WithOne(ds => ds.Bill)
                .HasForeignKey<Bill>(b => b.DiningSessionId)
                .HasConstraintName("FK_Bill_DiningSession")
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Bill>()
                .HasOne(b => b.TaxConfiguration)
                .WithMany()
                .HasForeignKey(b => b.TaxConfigurationId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<AuditLog>(audit =>
        {
            audit.HasKey(a => a.Id)
                .HasName("PK_AuditLog");

            audit.Property(a => a.Action)
                .HasConversion<string>();

            audit.Property(a => a.OldValues)
                .HasColumnType("jsonb");

            audit.Property(a => a.NewValues)
                .HasColumnType("jsonb");

            audit.Property(a => a.PerformedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
        });
        modelBuilder.Entity<InventoryItem>(inventory =>
        {
            inventory.HasKey(i => i.Id)
                .HasName("PK_InventoryItem");

            inventory.HasIndex(i => i.Name)
                .IsUnique();

            inventory.Property(i => i.AvailableQuantity)
                .HasColumnType("numeric(10,2)");

            inventory.Property(i => i.MinimumStockThreshold)
                .HasColumnType("numeric(10,2)");

            inventory.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_InventoryItem_AvailableQuantity",
                    "\"AvailableQuantity\" >= 0");

                t.HasCheckConstraint(
                    "CK_InventoryItem_MinimumStockThreshold",
                    "\"MinimumStockThreshold\" >= 0");
            });

            inventory.Property(i => i.IsDeleted)
                .HasDefaultValue(false);

            inventory.Property(i => i.LastUpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");

            inventory.HasData(
                new InventoryItem
                {
                    Id = 1,
                    Name = "Rice",
                    AvailableQuantity = 50m,
                    Unit = "Kg",
                    MinimumStockThreshold = 10m,
                    IsDeleted = false
                },
                new InventoryItem
                {
                    Id = 2,
                    Name = "Paneer",
                    AvailableQuantity = 20m,
                    Unit = "Kg",
                    MinimumStockThreshold = 5m,
                    IsDeleted = false
                },
                new InventoryItem
                {
                    Id = 3,
                    Name = "Chicken",
                    AvailableQuantity = 25m,
                    Unit = "Kg",
                    MinimumStockThreshold = 5m,
                    IsDeleted = false
                },
                new InventoryItem
                {
                    Id = 4,
                    Name = "Cooking Oil",
                    AvailableQuantity = 30m,
                    Unit = "Litre",
                    MinimumStockThreshold = 5m,
                    IsDeleted = false
                }
            );
        });
        modelBuilder.Entity<TaxConfiguration>(entity =>
        {
            entity.HasKey(tc => tc.Id)
                .HasName("PK_TaxConfiguration");

            entity.Property(tc => tc.CgstPercentage)
                .HasPrecision(4, 2);

            entity.Property(tc => tc.SgstPercentage)
                .HasPrecision(4, 2);

            entity.Property(tc => tc.ServiceChargePercentage)
                .HasPrecision(4, 2);

            entity.Property(tc => tc.IsActive)
                .HasDefaultValue(false);

            entity.Property(tc => tc.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            modelBuilder.Entity<TaxConfiguration>().HasData(
            new TaxConfiguration
            {
                Id = 1,
                CgstPercentage = 2.5m,
                SgstPercentage = 2.5m,
                ServiceChargePercentage = 5m,
                EffectiveFrom = new DateTime(
                    2026,
                    1,
                    1,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc),
                IsActive = true
            }
        );
        });

    }
}
