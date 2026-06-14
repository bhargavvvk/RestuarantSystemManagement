using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RestaurantAPI.Migrations
{
    /// <inheritdoc />
    public partial class seeding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EntityName = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    OldValues = table.Column<string>(type: "jsonb", nullable: true),
                    NewValues = table.Column<string>(type: "jsonb", nullable: true),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    PerformedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Category", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    EncryptedMobileNumber = table.Column<string>(type: "text", nullable: false),
                    MobileNumberHash = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customer", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AvailableQuantity = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Unit = table.Column<string>(type: "text", nullable: false),
                    MinimumStockThreshold = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItem", x => x.Id);
                    table.CheckConstraint("CK_InventoryItem_AvailableQuantity", "\"AvailableQuantity\" >= 0");
                    table.CheckConstraint("CK_InventoryItem_MinimumStockThreshold", "\"MinimumStockThreshold\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "TaxConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CgstPercentage = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    SgstPercentage = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    ServiceChargePercentage = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxConfiguration", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    Name = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    EncryptedMobileNumber = table.Column<string>(type: "text", nullable: false),
                    MobileNumberHash = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<byte[]>(type: "bytea", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    FoodType = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItem", x => x.Id);
                    table.CheckConstraint("CK_MenuItem_Price", "\"Price\" >= 0");
                    table.ForeignKey(
                        name: "FK_MenuItem_Category",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RestaurantTables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TableNumber = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    QrIdentifier = table.Column<string>(type: "text", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false, defaultValue: "Available"),
                    AssignedWaiterId = table.Column<int>(type: "integer", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantTable", x => x.Id);
                    table.CheckConstraint("CK_RestaurantTable_Capacity", "\"Capacity\" > 0");
                    table.ForeignKey(
                        name: "FK_RestaurantTable_User",
                        column: x => x.AssignedWaiterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DiningSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TableId = table.Column<int>(type: "integer", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    WaiterId = table.Column<int>(type: "integer", nullable: false),
                    SessionOtp = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false, defaultValue: "Active"),
                    StartedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    EndedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiningSession", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiningSession_Customer",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiningSession_Table",
                        column: x => x.TableId,
                        principalTable: "RestaurantTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiningSession_User",
                        column: x => x.WaiterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Bills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BillNumber = table.Column<string>(type: "text", nullable: false),
                    DiningSessionId = table.Column<int>(type: "integer", nullable: false),
                    TaxConfigurationId = table.Column<int>(type: "integer", nullable: false),
                    FoodTotal = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    CgstAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    SgstAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    ServiceChargeAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    GrandTotal = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    PaymentStatus = table.Column<string>(type: "text", nullable: false, defaultValue: "Pending"),
                    PaymentMethod = table.Column<string>(type: "text", nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    PaidAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bill", x => x.Id);
                    table.CheckConstraint("CK_CgstAmount", "\"CgstAmount\" >= 0");
                    table.CheckConstraint("CK_FoodTotal", "\"FoodTotal\" >= 0");
                    table.CheckConstraint("CK_GrandTotal", "\"GrandTotal\" >= 0");
                    table.CheckConstraint("CK_ServiceChargeAmount", "\"ServiceChargeAmount\" >= 0");
                    table.CheckConstraint("CK_SgstAmount", "\"SgstAmount\" >= 0");
                    table.ForeignKey(
                        name: "FK_Bill_DiningSession",
                        column: x => x.DiningSessionId,
                        principalTable: "DiningSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bills_TaxConfigurations_TaxConfigurationId",
                        column: x => x.TaxConfigurationId,
                        principalTable: "TaxConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Carts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DiningSessionId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cart", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cart_DiningSession",
                        column: x => x.DiningSessionId,
                        principalTable: "DiningSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DiningSessionId = table.Column<int>(type: "integer", nullable: false),
                    RequestType = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false, defaultValue: "Pending"),
                    RequestedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CompletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerRequest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerRequest_DiningSession",
                        column: x => x.DiningSessionId,
                        principalTable: "DiningSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DiningSessionId = table.Column<int>(type: "integer", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    PlacedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CancelledAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SpecialInstructions = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Order", x => x.Id);
                    table.CheckConstraint("CK_Order_TotalAmount", "\"TotalAmount\" >= 0");
                    table.ForeignKey(
                        name: "FK_Order_DiningSession",
                        column: x => x.DiningSessionId,
                        principalTable: "DiningSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CartItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CartId = table.Column<int>(type: "integer", nullable: false),
                    MenuItemId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartItem", x => x.Id);
                    table.CheckConstraint("CK_CartItem_Quantity", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_CartItem_Cart",
                        column: x => x.CartId,
                        principalTable: "Carts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CartItem_MenuItem",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    MenuItemId = table.Column<int>(type: "integer", nullable: false),
                    ItemName = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ItemPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false, defaultValue: "Placed")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItem", x => x.Id);
                    table.CheckConstraint("CK_OrderItem_ItemPrice", "\"ItemPrice\" >= 0");
                    table.CheckConstraint("CK_OrderItem_Quantity", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_OrderItem_MenuItem",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItem_Order",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Description", "IsAvailable", "Name" },
                values: new object[,]
                {
                    { 1, "Appetizers", true, "Starters" },
                    { 2, "Main dishes", true, "Main Course" },
                    { 3, "Sweet dishes", true, "Desserts" },
                    { 4, "Drinks", true, "Beverages" }
                });

            migrationBuilder.InsertData(
                table: "InventoryItems",
                columns: new[] { "Id", "AvailableQuantity", "MinimumStockThreshold", "Name", "Unit" },
                values: new object[,]
                {
                    { 1, 50m, 10m, "Rice", "Kg" },
                    { 2, 20m, 5m, "Paneer", "Kg" },
                    { 3, 25m, 5m, "Chicken", "Kg" },
                    { 4, 30m, 5m, "Cooking Oil", "Litre" }
                });

            migrationBuilder.InsertData(
                table: "TaxConfigurations",
                columns: new[] { "Id", "CgstPercentage", "EffectiveFrom", "IsActive", "ServiceChargePercentage", "SgstPercentage" },
                values: new object[] { 1, 2.5m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 5m, 2.5m });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "EncryptedMobileNumber", "IsActive", "MobileNumberHash", "Name", "PasswordHash", "Role", "Username" },
                values: new object[,]
                {
                    { 1, "ADMIN_ENCRYPTED", true, "ADMIN_HASH", "System Admin", new byte[] { 1, 2, 3 }, "Admin", "admin" },
                    { 2, "KITCHEN_ENCRYPTED", true, "KITCHEN_HASH", "Kitchen Staff", new byte[] { 1, 2, 3 }, "KitchenStaff", "kitchen" },
                    { 3, "WAITER1_ENCRYPTED", true, "WAITER1_HASH", "Ramesh", new byte[] { 1, 2, 3 }, "Waiter", "waiter1" },
                    { 4, "WAITER2_ENCRYPTED", true, "WAITER2_HASH", "Suresh", new byte[] { 1, 2, 3 }, "Waiter", "waiter2" }
                });

            migrationBuilder.InsertData(
                table: "MenuItems",
                columns: new[] { "Id", "CategoryId", "Description", "FoodType", "ImageUrl", "IsAvailable", "Name", "Price" },
                values: new object[,]
                {
                    { 1, 1, null, "Veg", null, true, "Paneer Tikka", 220m },
                    { 2, 1, null, "NonVeg", null, true, "Chicken 65", 260m },
                    { 3, 2, null, "Veg", null, true, "Veg Biryani", 180m },
                    { 4, 2, null, "NonVeg", null, true, "Chicken Biryani", 250m },
                    { 5, 2, null, "Veg", null, true, "Butter Naan", 40m },
                    { 6, 2, null, "Veg", null, true, "Paneer Butter", 210m },
                    { 7, 3, null, "Veg", null, true, "Brownie", 120m },
                    { 8, 3, null, "Veg", null, true, "Ice Cream", 90m },
                    { 9, 4, null, "Veg", null, true, "Coke", 50m },
                    { 10, 4, null, "Veg", null, true, "Lemon Soda", 60m }
                });

            migrationBuilder.InsertData(
                table: "RestaurantTables",
                columns: new[] { "Id", "AssignedWaiterId", "Capacity", "QrIdentifier", "TableNumber" },
                values: new object[,]
                {
                    { 1, 3, 4, "TBL_001", "T1" },
                    { 2, 3, 4, "TBL_002", "T2" },
                    { 3, 4, 6, "TBL_003", "T3" },
                    { 4, 4, 2, "TBL_004", "T4" },
                    { 5, 3, 8, "TBL_005", "T5" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bills_BillNumber",
                table: "Bills",
                column: "BillNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bills_DiningSessionId",
                table: "Bills",
                column: "DiningSessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bills_TaxConfigurationId",
                table: "Bills",
                column: "TaxConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId_MenuItemId",
                table: "CartItems",
                columns: new[] { "CartId", "MenuItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_MenuItemId",
                table: "CartItems",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Carts_DiningSessionId",
                table: "Carts",
                column: "DiningSessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerRequests_DiningSessionId",
                table: "CustomerRequests",
                column: "DiningSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_MobileNumberHash",
                table: "Customers",
                column: "MobileNumberHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiningSessions_CustomerId",
                table: "DiningSessions",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_DiningSessions_SessionOtp",
                table: "DiningSessions",
                column: "SessionOtp",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiningSessions_WaiterId",
                table: "DiningSessions",
                column: "WaiterId");

            migrationBuilder.CreateIndex(
                name: "UQ_Active_Table_Session",
                table: "DiningSessions",
                column: "TableId",
                unique: true,
                filter: "\"Status\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_Name",
                table: "InventoryItems",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_CategoryId",
                table: "MenuItems",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_MenuItemId",
                table: "OrderItems",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId_MenuItemId",
                table: "OrderItems",
                columns: new[] { "OrderId", "MenuItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_DiningSessionId",
                table: "Orders",
                column: "DiningSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantTables_AssignedWaiterId",
                table: "RestaurantTables",
                column: "AssignedWaiterId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantTables_QrIdentifier",
                table: "RestaurantTables",
                column: "QrIdentifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantTables_TableNumber",
                table: "RestaurantTables",
                column: "TableNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_MobileNumberHash",
                table: "Users",
                column: "MobileNumberHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "Bills");

            migrationBuilder.DropTable(
                name: "CartItems");

            migrationBuilder.DropTable(
                name: "CustomerRequests");

            migrationBuilder.DropTable(
                name: "InventoryItems");

            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "TaxConfigurations");

            migrationBuilder.DropTable(
                name: "Carts");

            migrationBuilder.DropTable(
                name: "MenuItems");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "DiningSessions");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "RestaurantTables");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
