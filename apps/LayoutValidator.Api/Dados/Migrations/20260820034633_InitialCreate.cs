using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LayoutValidator.Api.Dados.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Layouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    Delimitador = table.Column<string>(type: "TEXT", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Layouts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CampoCadastrado",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LayoutId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    Ordem = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampoCadastrado", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampoCadastrado_Layouts_LayoutId",
                        column: x => x.LayoutId,
                        principalTable: "Layouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegraCampoCadastrada",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CampoId = table.Column<int>(type: "INTEGER", nullable: false),
                    ChaveRegra = table.Column<string>(type: "TEXT", nullable: false),
                    ParametrosJson = table.Column<string>(type: "TEXT", nullable: true),
                    Ordem = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegraCampoCadastrada", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegraCampoCadastrada_CampoCadastrado_CampoId",
                        column: x => x.CampoId,
                        principalTable: "CampoCadastrado",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CampoCadastrado_LayoutId",
                table: "CampoCadastrado",
                column: "LayoutId");

            migrationBuilder.CreateIndex(
                name: "IX_Layouts_Codigo",
                table: "Layouts",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegraCampoCadastrada_CampoId",
                table: "RegraCampoCadastrada",
                column: "CampoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegraCampoCadastrada");

            migrationBuilder.DropTable(
                name: "CampoCadastrado");

            migrationBuilder.DropTable(
                name: "Layouts");
        }
    }
}
