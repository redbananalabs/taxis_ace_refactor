using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaxiDispatch.Migrations
{
    /// <inheritdoc />
    public partial class MembershipFoundationPhase1_20260306 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountUserLinks",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    AccNo = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountUserLinks", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_AccountUserLinks_Accounts_AccNo",
                        column: x => x.AccNo,
                        principalTable: "Accounts",
                        principalColumn: "AccNo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountUserLinks_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DriverUserProfiles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RegNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ColorCodeRgb = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(9,7)", precision: 9, scale: 7, nullable: true),
                    Heading = table.Column<decimal>(type: "decimal(6,3)", precision: 6, scale: 3, nullable: true),
                    Speed = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    GpsLastUpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VehicleMake = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    VehicleModel = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    VehicleColour = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ShowAllBookings = table.Column<bool>(type: "bit", nullable: false),
                    ShowHvsBookings = table.Column<bool>(type: "bit", nullable: false),
                    CashCommissionRate = table.Column<int>(type: "int", nullable: false),
                    VehicleType = table.Column<int>(type: "int", nullable: false),
                    NonAce = table.Column<bool>(type: "bit", nullable: false),
                    CommsPlatform = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverUserProfiles", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_DriverUserProfiles_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantUsers",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LastLoginUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantUsers", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_TenantUsers_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserDeviceRegistrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DeviceType = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeviceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDeviceRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDeviceRegistrations_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountUserLinks_AccNo",
                table: "AccountUserLinks",
                column: "AccNo");

            migrationBuilder.CreateIndex(
                name: "IX_UserDeviceRegistrations_UserId_DeviceType",
                table: "UserDeviceRegistrations",
                columns: new[] { "UserId", "DeviceType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountUserLinks");

            migrationBuilder.DropTable(
                name: "DriverUserProfiles");

            migrationBuilder.DropTable(
                name: "TenantUsers");

            migrationBuilder.DropTable(
                name: "UserDeviceRegistrations");
        }
    }
}
