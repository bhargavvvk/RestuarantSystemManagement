using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantAPI.Migrations
{
    /// <inheritdoc />
    public partial class updatedusermodel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RestaurantTable_User",
                table: "RestaurantTables");

            migrationBuilder.AddForeignKey(
                name: "FK_RestaurantTables_Users_AssignedWaiterId",
                table: "RestaurantTables",
                column: "AssignedWaiterId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RestaurantTables_Users_AssignedWaiterId",
                table: "RestaurantTables");

            migrationBuilder.AddForeignKey(
                name: "FK_RestaurantTable_User",
                table: "RestaurantTables",
                column: "AssignedWaiterId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
