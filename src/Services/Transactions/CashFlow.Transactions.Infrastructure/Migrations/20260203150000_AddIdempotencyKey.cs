using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CashFlow.Transactions.Infrastructure.Migrations
{
    /// <summary>
    /// Migration to add IdempotencyKey column and unique index
    /// This ensures idempotent transaction creation
    /// </summary>
    public partial class AddIdempotencyKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "idempotency_key",
                table: "transactions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_transactions_idempotency_key_unique",
                table: "transactions",
                column: "idempotency_key",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_transactions_idempotency_key_unique",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                table: "transactions");
        }
    }
}
