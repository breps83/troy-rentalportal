using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalPortal.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameApplicationStatusHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationStatusChanges_AspNetUsers_ChangedById",
                table: "ApplicationStatusChanges");

            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationStatusChanges_RentalApplications_RentalApplicationId",
                table: "ApplicationStatusChanges");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ApplicationStatusChanges",
                table: "ApplicationStatusChanges");

            migrationBuilder.RenameTable(
                name: "ApplicationStatusChanges",
                newName: "ApplicationStatusHistory");

            migrationBuilder.RenameIndex(
                name: "IX_ApplicationStatusChanges_RentalApplicationId",
                table: "ApplicationStatusHistory",
                newName: "IX_ApplicationStatusHistory_RentalApplicationId");

            migrationBuilder.RenameIndex(
                name: "IX_ApplicationStatusChanges_ChangedById",
                table: "ApplicationStatusHistory",
                newName: "IX_ApplicationStatusHistory_ChangedById");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ApplicationStatusHistory",
                table: "ApplicationStatusHistory",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationStatusHistory_AspNetUsers_ChangedById",
                table: "ApplicationStatusHistory",
                column: "ChangedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationStatusHistory_RentalApplications_RentalApplicationId",
                table: "ApplicationStatusHistory",
                column: "RentalApplicationId",
                principalTable: "RentalApplications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationStatusHistory_AspNetUsers_ChangedById",
                table: "ApplicationStatusHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationStatusHistory_RentalApplications_RentalApplicationId",
                table: "ApplicationStatusHistory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ApplicationStatusHistory",
                table: "ApplicationStatusHistory");

            migrationBuilder.RenameTable(
                name: "ApplicationStatusHistory",
                newName: "ApplicationStatusChanges");

            migrationBuilder.RenameIndex(
                name: "IX_ApplicationStatusHistory_RentalApplicationId",
                table: "ApplicationStatusChanges",
                newName: "IX_ApplicationStatusChanges_RentalApplicationId");

            migrationBuilder.RenameIndex(
                name: "IX_ApplicationStatusHistory_ChangedById",
                table: "ApplicationStatusChanges",
                newName: "IX_ApplicationStatusChanges_ChangedById");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ApplicationStatusChanges",
                table: "ApplicationStatusChanges",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationStatusChanges_AspNetUsers_ChangedById",
                table: "ApplicationStatusChanges",
                column: "ChangedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationStatusChanges_RentalApplications_RentalApplicationId",
                table: "ApplicationStatusChanges",
                column: "RentalApplicationId",
                principalTable: "RentalApplications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
