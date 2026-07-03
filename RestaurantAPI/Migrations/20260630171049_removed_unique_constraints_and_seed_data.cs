using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RestaurantAPI.Migrations
{
    /// <inheritdoc />
    public partial class removed_unique_constraints_and_seed_data : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_Name",
                table: "InventoryItems");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Name",
                table: "Categories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                columns: new[] { "Id", "EncryptedMobileNumber", "HashKey", "IsActive", "MobileNumberHash", "MustChangePassword", "Name", "PasswordHash", "Role", "Username" },
                values: new object[,]
                {
                    { 1, "ADMIN_ENCRYPTED", new byte[0], true, "ADMIN_HASH", true, "System Admin", new byte[] { 1, 2, 3 }, "Admin", "admin" },
                    { 2, "KITCHEN_ENCRYPTED", new byte[0], true, "KITCHEN_HASH", true, "Kitchen Staff", new byte[] { 1, 2, 3 }, "KitchenStaff", "kitchen" },
                    { 3, "WAITER1_ENCRYPTED", new byte[0], true, "WAITER1_HASH", true, "Ramesh", new byte[] { 1, 2, 3 }, "Waiter", "waiter1" },
                    { 4, "WAITER2_ENCRYPTED", new byte[0], true, "WAITER2_HASH", true, "Suresh", new byte[] { 1, 2, 3 }, "Waiter", "waiter2" }
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
                columns: new[] { "Id", "AssignedWaiterId", "Capacity", "QrIdentifier", "Status", "TableNumber" },
                values: new object[,]
                {
                    { 1, 3, 4, "TBL_001", "Available", "T1" },
                    { 2, 3, 4, "TBL_002", "Available", "T2" },
                    { 3, 4, 6, "TBL_003", "Available", "T3" },
                    { 4, 4, 2, "TBL_004", "Available", "T4" },
                    { 5, 3, 8, "TBL_005", "Available", "T5" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_Name",
                table: "InventoryItems",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);
        }
    }
}
