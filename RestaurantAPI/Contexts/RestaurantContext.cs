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
    public DbSet<DiningSessionTable> DiningSessionTables { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(user =>
        {
            user.HasKey(u => u.Id).HasName("PK_User");

            user.Property(u => u.Role)
                .HasConversion<string>();

            user.Property(u => u.IsDeleted)
                .HasDefaultValue(false);

            user.Property(u => u.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");

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



        });
        modelBuilder.Entity<Category>(category =>
        {
            category.HasKey(c => c.Id)
                .HasName("PK_Category");

            category.Property(c => c.IsAvailable)
                .HasDefaultValue(true);

            category.Property(c => c.IsDeleted)
                .HasDefaultValue(false);

            category.Property(c => c.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
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
                ci.MenuItemId,
                ci.TableId
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

            cartItem.HasOne(ci => ci.Table)
                .WithMany()
                .HasForeignKey(ci => ci.TableId)
                .HasConstraintName("FK_CartItem_Table")
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
                oi.MenuItemId,
                oi.TableId
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

            orderItem.HasOne(oi => oi.Table)
                .WithMany()
                .HasForeignKey(oi => oi.TableId)
                .HasConstraintName("FK_OrderItem_Table")
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
        });

        modelBuilder.Entity<DiningSessionTable>(dst =>
        {
            dst.HasKey(x => x.Id).HasName("PK_DiningSessionTable");

            dst.HasOne(x => x.DiningSession)
                .WithMany(ds => ds.DiningSessionTables)
                .HasForeignKey(x => x.DiningSessionId)
                .HasConstraintName("FK_DiningSessionTable_DiningSession")
                .OnDelete(DeleteBehavior.Cascade);

            dst.HasOne(x => x.Table)
                .WithMany()
                .HasForeignKey(x => x.TableId)
                .HasConstraintName("FK_DiningSessionTable_Table")
                .OnDelete(DeleteBehavior.Restrict);

            dst.Property(x => x.LinkedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
        });
    }
}
