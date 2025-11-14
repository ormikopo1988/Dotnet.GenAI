using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dotnet.GenAI.MyCareerAssistant.Migrations
{
    /// <inheritdoc />
    public partial class EnableVectorExtension : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS vector;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP EXTENSION IF EXISTS vector;");
        }
    }
}
