using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppAlumnos.Migrations
{
    /// <inheritdoc />
    public partial class AutogestionYArchivos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocenteId",
                table: "Materias",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RutaFoto",
                table: "AspNetUsers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Materias_DocenteId",
                table: "Materias",
                column: "DocenteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Materias_AspNetUsers_DocenteId",
                table: "Materias",
                column: "DocenteId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Materias_AspNetUsers_DocenteId",
                table: "Materias");

            migrationBuilder.DropIndex(
                name: "IX_Materias_DocenteId",
                table: "Materias");

            migrationBuilder.DropColumn(
                name: "DocenteId",
                table: "Materias");

            migrationBuilder.DropColumn(
                name: "RutaFoto",
                table: "AspNetUsers");
        }
    }
}
