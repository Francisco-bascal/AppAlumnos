using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppAlumnos.Migrations
{
    /// <inheritdoc />
    public partial class AdicionCursadasYAuditoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCreacion",
                table: "Materias",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaModificacion",
                table: "Materias",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "UsuarioCreacionId",
                table: "Materias",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioModificacionId",
                table: "Materias",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Rol",
                table: "AspNetUsers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Cursadas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MateriaId = table.Column<int>(type: "int", nullable: false),
                    AnioLectivo = table.Column<int>(type: "int", nullable: false),
                    Nota = table.Column<decimal>(type: "decimal(4,1)", precision: 4, scale: 1, nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioCreacionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioModificacionId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cursadas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cursadas_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cursadas_Materias_MateriaId",
                        column: x => x.MateriaId,
                        principalTable: "Materias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cursadas_MateriaId",
                table: "Cursadas",
                column: "MateriaId");

            migrationBuilder.CreateIndex(
                name: "IX_Cursadas_UsuarioId_MateriaId_AnioLectivo",
                table: "Cursadas",
                columns: new[] { "UsuarioId", "MateriaId", "AnioLectivo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cursadas");

            migrationBuilder.DropColumn(
                name: "FechaCreacion",
                table: "Materias");

            migrationBuilder.DropColumn(
                name: "FechaModificacion",
                table: "Materias");

            migrationBuilder.DropColumn(
                name: "UsuarioCreacionId",
                table: "Materias");

            migrationBuilder.DropColumn(
                name: "UsuarioModificacionId",
                table: "Materias");

            migrationBuilder.DropColumn(
                name: "Rol",
                table: "AspNetUsers");
        }
    }
}
