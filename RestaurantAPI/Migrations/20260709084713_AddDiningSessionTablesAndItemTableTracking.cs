using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RestaurantAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddDiningSessionTablesAndItemTableTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderItems_OrderId_MenuItemId",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_CartId_MenuItemId",
                table: "CartItems");

            migrationBuilder.AddColumn<int>(
                name: "TableId",
                table: "OrderItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TableId",
                table: "CartItems",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DiningSessionTables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DiningSessionId = table.Column<int>(type: "integer", nullable: false),
                    TableId = table.Column<int>(type: "integer", nullable: false),
                    LinkedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiningSessionTable", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiningSessionTable_DiningSession",
                        column: x => x.DiningSessionId,
                        principalTable: "DiningSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiningSessionTable_Table",
                        column: x => x.TableId,
                        principalTable: "RestaurantTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId_MenuItemId_TableId",
                table: "OrderItems",
                columns: new[] { "OrderId", "MenuItemId", "TableId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_TableId",
                table: "OrderItems",
                column: "TableId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId_MenuItemId_TableId",
                table: "CartItems",
                columns: new[] { "CartId", "MenuItemId", "TableId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_TableId",
                table: "CartItems",
                column: "TableId");

            migrationBuilder.CreateIndex(
                name: "IX_DiningSessionTables_DiningSessionId",
                table: "DiningSessionTables",
                column: "DiningSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_DiningSessionTables_TableId",
                table: "DiningSessionTables",
                column: "TableId");

            migrationBuilder.AddForeignKey(
                name: "FK_CartItem_Table",
                table: "CartItems",
                column: "TableId",
                principalTable: "RestaurantTables",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItem_Table",
                table: "OrderItems",
                column: "TableId",
                principalTable: "RestaurantTables",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CartItem_Table",
                table: "CartItems");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItem_Table",
                table: "OrderItems");

            migrationBuilder.DropTable(
                name: "DiningSessionTables");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_OrderId_MenuItemId_TableId",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_TableId",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_CartId_MenuItemId_TableId",
                table: "CartItems");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_TableId",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "TableId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "TableId",
                table: "CartItems");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId_MenuItemId",
                table: "OrderItems",
                columns: new[] { "OrderId", "MenuItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId_MenuItemId",
                table: "CartItems",
                columns: new[] { "CartId", "MenuItemId" },
                unique: true);
        }
    }
}
