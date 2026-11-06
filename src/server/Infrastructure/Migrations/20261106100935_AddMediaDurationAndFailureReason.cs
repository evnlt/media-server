using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaDurationAndFailureReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "DurationSeconds",
                table: "MediaFiles",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "MediaFiles",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            // Processing was split into Queued and Transcoding; startup recovery re-queues these rows.
            migrationBuilder.Sql("UPDATE MediaFiles SET Status = 'Queued' WHERE Status = 'Processing';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE MediaFiles SET Status = 'Processing' WHERE Status IN ('Queued', 'Transcoding');");

            migrationBuilder.DropColumn(
                name: "DurationSeconds",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "MediaFiles");
        }
    }
}
