using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayderPay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMainAccountAndBalanceControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MainAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Iban = table.Column<string>(type: "character varying(34)", maxLength: 34, nullable: false),
                    Balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MainAccounts", x => x.Id);
                    table.CheckConstraint("CK_MainAccounts_Balance", "\"Balance\" >= 0");
                    table.ForeignKey(
                        name: "FK_MainAccounts_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MainAccounts_CustomerId",
                table: "MainAccounts",
                column: "CustomerId",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_MainAccounts_Iban",
                table: "MainAccounts",
                column: "Iban",
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO "MainAccounts"
                    ("Id", "CustomerId", "Iban", "Balance", "CreatedAtUtc", "UpdatedAtUtc", "IsDeleted", "DeletedAtUtc")
                SELECT
                    md5(random()::text || clock_timestamp()::text || c."Id"::text)::uuid,
                    c."Id",
                    'TR99' || lpad((row_number() OVER (ORDER BY c."Id"))::text, 22, '0'),
                    0,
                    timezone('utc', now()),
                    timezone('utc', now()),
                    false,
                    NULL
                FROM "Customers" c
                LEFT JOIN "MainAccounts" ma
                    ON ma."CustomerId" = c."Id"
                   AND ma."IsDeleted" = false
                WHERE c."IsDeleted" = false
                  AND ma."Id" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MainAccounts");
        }
    }
}
