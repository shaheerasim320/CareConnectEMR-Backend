using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareConnectEMR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFollowUpColumnsToAppointments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MRN",
                table: "Patients",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValueSql: "FORMAT(NEXT VALUE FOR PatientNumbers, 'MRN-0000')",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<DateTime>(
                name: "FollowUpDate",
                table: "Appointments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresFollowUp",
                table: "Appointments",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FollowUpDate",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "RequiresFollowUp",
                table: "Appointments");

            migrationBuilder.AlterColumn<string>(
                name: "MRN",
                table: "Patients",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValueSql: "FORMAT(NEXT VALUE FOR PatientNumbers, 'MRN-0000')");
        }
    }
}
