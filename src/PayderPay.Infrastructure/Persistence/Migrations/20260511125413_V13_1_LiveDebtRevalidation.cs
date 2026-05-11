using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayderPay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class V13_1_LiveDebtRevalidation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_SubscriptionId_PeriodYear_PeriodMonth",
                table: "Payments");

            migrationBuilder.AddColumn<Guid>(
                name: "DebtId",
                table: "Payments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "DebtId",
                table: "DebtQueryResults",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "ProviderName",
                table: "DebtQueryResults",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SubscriberNumber",
                table: "DebtQueryResults",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_DebtId",
                table: "Payments",
                column: "DebtId",
                unique: true,
                filter: "\"Status\" = 'Successful' AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_SubscriptionId",
                table: "Payments",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_DebtQueryResults_SubscriptionId_DebtId",
                table: "DebtQueryResults",
                columns: new[] { "SubscriptionId", "DebtId" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_DebtId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_SubscriptionId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_DebtQueryResults_SubscriptionId_DebtId",
                table: "DebtQueryResults");

            migrationBuilder.DropColumn(
                name: "DebtId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "DebtId",
                table: "DebtQueryResults");

            migrationBuilder.DropColumn(
                name: "ProviderName",
                table: "DebtQueryResults");

            migrationBuilder.DropColumn(
                name: "SubscriberNumber",
                table: "DebtQueryResults");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_SubscriptionId_PeriodYear_PeriodMonth",
                table: "Payments",
                columns: new[] { "SubscriptionId", "PeriodYear", "PeriodMonth" },
                unique: true,
                filter: "\"Status\" = 'Successful' AND \"IsDeleted\" = false");
        }
    }
}
