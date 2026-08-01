using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyRunshaw.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyBusSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationDeviceBusSubscriptions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotificationDeviceBusSubscriptions",
                columns: table => new
                {
                    NotificationDeviceId = table.Column<int>(type: "integer", nullable: false),
                    BusId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationDeviceBusSubscriptions", x => new { x.NotificationDeviceId, x.BusId });
                    table.ForeignKey(
                        name: "FK_NotificationDeviceBusSubscriptions_Buses_BusId",
                        column: x => x.BusId,
                        principalTable: "Buses",
                        principalColumn: "BusId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotificationDeviceBusSubscriptions_NotificationDevices_Noti~",
                        column: x => x.NotificationDeviceId,
                        principalTable: "NotificationDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeviceBusSubscriptions_BusId",
                table: "NotificationDeviceBusSubscriptions",
                column: "BusId");
        }
    }
}
