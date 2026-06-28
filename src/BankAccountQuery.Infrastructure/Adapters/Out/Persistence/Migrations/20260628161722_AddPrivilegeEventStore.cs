using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankAccountQuery.Infrastructure.Adapters.Out.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivilegeEventStore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PrivilegeEvents",
                columns: table => new
                {
                    StreamId = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    OccurredOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivilegeEvents", x => new { x.StreamId, x.Version });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrivilegeEvents");
        }
    }
}
