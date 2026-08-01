using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MyRunshaw.Infrastructure.Migrations;

public partial class AddNotificationDevices : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "NotificationDevices",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                StudentId = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                DeviceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                FcmToken = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                Platform = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                AppVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                NotificationsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                BusNotificationsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NotificationDevices", x => x.Id);
                table.ForeignKey(
                    name: "FK_NotificationDevices_Users_StudentId",
                    column: x => x.StudentId,
                    principalTable: "Users",
                    principalColumn: "StudentId",
                    onDelete: ReferentialAction.Cascade);
            });

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
                    name: "FK_NotificationDeviceBusSubscriptions_NotificationDevices_NotificationDeviceId",
                    column: x => x.NotificationDeviceId,
                    principalTable: "NotificationDevices",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_NotificationDevices_FcmToken", table: "NotificationDevices", column: "FcmToken", unique: true);
        migrationBuilder.CreateIndex(name: "IX_NotificationDevices_StudentId_DeviceId", table: "NotificationDevices", columns: new[] { "StudentId", "DeviceId" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_NotificationDeviceBusSubscriptions_BusId", table: "NotificationDeviceBusSubscriptions", column: "BusId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "NotificationDeviceBusSubscriptions");
        migrationBuilder.DropTable(name: "NotificationDevices");
    }
}
