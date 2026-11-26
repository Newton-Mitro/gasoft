using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Gasoft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ProfileImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<long>(type: "bigint", nullable: true),
                    ActorUserId = table.Column<long>(type: "bigint", nullable: true),
                    Action = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DetailsJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Currencies",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DecimalPlaces = table.Column<byte>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.Code);
                    table.CheckConstraint("CK_Currencies_DecimalPlaces", "\"DecimalPlaces\" <= 4");
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Link = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DismissedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxCategories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxJurisdictions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CountryCode = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxJurisdictions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnitsOfMeasure",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    DecimalPlaces = table.Column<byte>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitsOfMeasure", x => x.Code);
                    table.CheckConstraint("CK_UnitsOfMeasure_DecimalPlaces", "\"DecimalPlaces\" <= 6");
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    RoleId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExchangeRates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SourceCurrencyCode = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    TargetCurrencyCode = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    EffectiveOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Rate = table.Column<decimal>(type: "numeric(20,10)", precision: 20, scale: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeRates", x => x.Id);
                    table.CheckConstraint("CK_ExchangeRates_Rate", "\"Rate\" > 0");
                    table.ForeignKey(
                        name: "FK_ExchangeRates_Currencies_SourceCurrencyCode",
                        column: x => x.SourceCurrencyCode,
                        principalTable: "Currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExchangeRates_Currencies_TargetCurrencyCode",
                        column: x => x.TargetCurrencyCode,
                        principalTable: "Currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    LegalName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    FunctionalCurrencyCode = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    Phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    Website = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Address = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    CityRegion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    PostalCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TaxId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                    table.UniqueConstraint("AK_Companies_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Companies_Currencies_FunctionalCurrencyCode",
                        column: x => x.FunctionalCurrencyCode,
                        principalTable: "Currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Companies_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConsolidationGroups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ReportingCurrencyCode = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsolidationGroups", x => x.Id);
                    table.UniqueConstraint("AK_ConsolidationGroups_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ConsolidationGroups_Currencies_ReportingCurrencyCode",
                        column: x => x.ReportingCurrencyCode,
                        principalTable: "Currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsolidationGroups_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItemCategories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemCategories", x => x.Id);
                    table.UniqueConstraint("AK_ItemCategories_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ItemCategories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Parties",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PartyNo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    IsCustomer = table.Column<bool>(type: "boolean", nullable: false),
                    IsSupplier = table.Column<bool>(type: "boolean", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    Phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    DefaultCurrencyCode = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parties", x => x.Id);
                    table.UniqueConstraint("AK_Parties_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Parties_Currencies_DefaultCurrencyCode",
                        column: x => x.DefaultCurrencyCode,
                        principalTable: "Currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Parties_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantUsers",
                columns: table => new
                {
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Role = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantUsers", x => new { x.TenantId, x.UserId });
                    table.ForeignKey(
                        name: "FK_TenantUsers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TenantUsers_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountGroups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    AccountType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NormalBalance = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountGroups", x => x.Id);
                    table.UniqueConstraint("AK_AccountGroups_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_AccountGroups_Companies_TenantId_CompanyId",
                        columns: x => new { x.TenantId, x.CompanyId },
                        principalTable: "Companies",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CostCenters",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostCenters", x => x.Id);
                    table.UniqueConstraint("AK_CostCenters_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_CostCenters_Companies_TenantId_CompanyId",
                        columns: x => new { x.TenantId, x.CompanyId },
                        principalTable: "Companies",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FiscalYears",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    StartsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    EndsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalYears", x => x.Id);
                    table.UniqueConstraint("AK_FiscalYears_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.CheckConstraint("CK_FiscalYears_DateRange", "\"EndsOn\" >= \"StartsOn\"");
                    table.ForeignKey(
                        name: "FK_FiscalYears_Companies_TenantId_CompanyId",
                        columns: x => new { x.TenantId, x.CompanyId },
                        principalTable: "Companies",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Journals",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    JournalType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Journals", x => x.Id);
                    table.UniqueConstraint("AK_Journals_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_Journals_Companies_TenantId_CompanyId",
                        columns: x => new { x.TenantId, x.CompanyId },
                        principalTable: "Companies",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaxCodes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JurisdictionId = table.Column<long>(type: "bigint", nullable: false),
                    CategoryId = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: true),
                    CompanyId = table.Column<long>(type: "bigint", nullable: true),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TaxType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxCodes", x => x.Id);
                    table.CheckConstraint("CK_TaxCodes_CompanyScope", "(\"TenantId\" IS NULL AND \"CompanyId\" IS NULL) OR (\"TenantId\" IS NOT NULL AND \"CompanyId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_TaxCodes_Companies_TenantId_CompanyId",
                        columns: x => new { x.TenantId, x.CompanyId },
                        principalTable: "Companies",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaxCodes_TaxCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "TaxCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaxCodes_TaxJurisdictions_JurisdictionId",
                        column: x => x.JurisdictionId,
                        principalTable: "TaxJurisdictions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantTaxRegistrations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    JurisdictionId = table.Column<long>(type: "bigint", nullable: false),
                    RegistrationNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantTaxRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantTaxRegistrations_Companies_TenantId_CompanyId",
                        columns: x => new { x.TenantId, x.CompanyId },
                        principalTable: "Companies",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantTaxRegistrations_TaxJurisdictions_JurisdictionId",
                        column: x => x.JurisdictionId,
                        principalTable: "TaxJurisdictions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Warehouses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouses", x => x.Id);
                    table.UniqueConstraint("AK_Warehouses_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_Warehouses_Companies_TenantId_CompanyId",
                        columns: x => new { x.TenantId, x.CompanyId },
                        principalTable: "Companies",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConsolidationGroupCompanies",
                columns: table => new
                {
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    ConsolidationGroupId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsolidationGroupCompanies", x => new { x.TenantId, x.ConsolidationGroupId, x.CompanyId });
                    table.ForeignKey(
                        name: "FK_ConsolidationGroupCompanies_Companies_TenantId_CompanyId",
                        columns: x => new { x.TenantId, x.CompanyId },
                        principalTable: "Companies",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsolidationGroupCompanies_ConsolidationGroups_TenantId_Co~",
                        columns: x => new { x.TenantId, x.ConsolidationGroupId },
                        principalTable: "ConsolidationGroups",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsolidationRuns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConsolidationGroupId = table.Column<long>(type: "bigint", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsolidationRuns", x => x.Id);
                    table.UniqueConstraint("AK_ConsolidationRuns_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ConsolidationRuns_ConsolidationGroups_TenantId_Consolidatio~",
                        columns: x => new { x.TenantId, x.ConsolidationGroupId },
                        principalTable: "ConsolidationGroups",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CategoryId = table.Column<long>(type: "bigint", nullable: false),
                    BaseUnitCode = table.Column<string>(type: "character varying(12)", nullable: false),
                    Sku = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CostingMethod = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                    table.UniqueConstraint("AK_Items_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Items_ItemCategories_TenantId_CategoryId",
                        columns: x => new { x.TenantId, x.CategoryId },
                        principalTable: "ItemCategories",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Items_UnitsOfMeasure_BaseUnitCode",
                        column: x => x.BaseUnitCode,
                        principalTable: "UnitsOfMeasure",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PartyAddresses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PartyId = table.Column<long>(type: "bigint", nullable: false),
                    AddressType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Line1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Line2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Region = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CountryCode = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartyAddresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartyAddresses_Parties_TenantId_PartyId",
                        columns: x => new { x.TenantId, x.PartyId },
                        principalTable: "Parties",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompanyUsers",
                columns: table => new
                {
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Role = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyUsers", x => new { x.TenantId, x.CompanyId, x.UserId });
                    table.ForeignKey(
                        name: "FK_CompanyUsers_Companies_TenantId_CompanyId",
                        columns: x => new { x.TenantId, x.CompanyId },
                        principalTable: "Companies",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanyUsers_TenantUsers_TenantId_UserId",
                        columns: x => new { x.TenantId, x.UserId },
                        principalTable: "TenantUsers",
                        principalColumns: new[] { "TenantId", "UserId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChartOfAccounts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    AccountGroupId = table.Column<long>(type: "bigint", nullable: false),
                    ParentId = table.Column<long>(type: "bigint", nullable: true),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    AllowPosting = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CashFlowCategory = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChartOfAccounts", x => x.Id);
                    table.UniqueConstraint("AK_ChartOfAccounts_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_ChartOfAccounts_AccountGroups_TenantId_CompanyId_AccountGro~",
                        columns: x => new { x.TenantId, x.CompanyId, x.AccountGroupId },
                        principalTable: "AccountGroups",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChartOfAccounts_ChartOfAccounts_TenantId_CompanyId_ParentId",
                        columns: x => new { x.TenantId, x.CompanyId, x.ParentId },
                        principalTable: "ChartOfAccounts",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountingPeriods",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    FiscalYearId = table.Column<long>(type: "bigint", nullable: false),
                    StartsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    EndsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingPeriods", x => x.Id);
                    table.UniqueConstraint("AK_AccountingPeriods_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.CheckConstraint("CK_AccountingPeriods_DateRange", "\"EndsOn\" >= \"StartsOn\"");
                    table.ForeignKey(
                        name: "FK_AccountingPeriods_FiscalYears_TenantId_CompanyId_FiscalYear~",
                        columns: x => new { x.TenantId, x.CompanyId, x.FiscalYearId },
                        principalTable: "FiscalYears",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Budgets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    FiscalYearId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Budgets", x => x.Id);
                    table.UniqueConstraint("AK_Budgets_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_Budgets_FiscalYears_TenantId_CompanyId_FiscalYearId",
                        columns: x => new { x.TenantId, x.CompanyId, x.FiscalYearId },
                        principalTable: "FiscalYears",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FiscalYearStatusChanges",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    FiscalYearId = table.Column<long>(type: "bigint", nullable: false),
                    ChangedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ToStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalYearStatusChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FiscalYearStatusChanges_AspNetUsers_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FiscalYearStatusChanges_FiscalYears_TenantId_CompanyId_Fisc~",
                        columns: x => new { x.TenantId, x.CompanyId, x.FiscalYearId },
                        principalTable: "FiscalYears",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaxRates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TaxCodeId = table.Column<long>(type: "bigint", nullable: false),
                    RatePercent = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxRates", x => x.Id);
                    table.CheckConstraint("CK_TaxRates_RatePercent", "\"RatePercent\" >= 0 AND \"RatePercent\" <= 100");
                    table.ForeignKey(
                        name: "FK_TaxRates_TaxCodes_TaxCodeId",
                        column: x => x.TaxCodeId,
                        principalTable: "TaxCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    SupplierId = table.Column<long>(type: "bigint", nullable: false),
                    WarehouseId = table.Column<long>(type: "bigint", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    OrderDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpectedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CurrencyCode = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(20,10)", precision: 20, scale: 10, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                    table.UniqueConstraint("AK_PurchaseOrders_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.CheckConstraint("CK_PurchaseOrders_ExchangeRate", "\"ExchangeRate\" > 0");
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Companies_TenantId_CompanyId",
                        columns: x => new { x.TenantId, x.CompanyId },
                        principalTable: "Companies",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Currencies_CurrencyCode",
                        column: x => x.CurrencyCode,
                        principalTable: "Currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Parties_TenantId_SupplierId",
                        columns: x => new { x.TenantId, x.SupplierId },
                        principalTable: "Parties",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Warehouses_TenantId_CompanyId_WarehouseId",
                        columns: x => new { x.TenantId, x.CompanyId, x.WarehouseId },
                        principalTable: "Warehouses",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseLocations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    WarehouseId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseLocations", x => x.Id);
                    table.UniqueConstraint("AK_WarehouseLocations_TenantId_CompanyId_WarehouseId_Id", x => new { x.TenantId, x.CompanyId, x.WarehouseId, x.Id });
                    table.ForeignKey(
                        name: "FK_WarehouseLocations_Warehouses_TenantId_CompanyId_WarehouseId",
                        columns: x => new { x.TenantId, x.CompanyId, x.WarehouseId },
                        principalTable: "Warehouses",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConsolidationJournalLines",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConsolidationRunId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<long>(type: "bigint", nullable: true),
                    AccountCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    AccountName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Debit = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Credit = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(20,10)", precision: 20, scale: 10, nullable: false),
                    IsElimination = table.Column<bool>(type: "boolean", nullable: false),
                    IsTranslationAdjustment = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsolidationJournalLines", x => x.Id);
                    table.CheckConstraint("CK_ConsolidationJournalLines_Amounts", "\"Debit\" >= 0 AND \"Credit\" >= 0 AND ((\"Debit\" > 0 AND \"Credit\" = 0) OR (\"Credit\" > 0 AND \"Debit\" = 0))");
                    table.ForeignKey(
                        name: "FK_ConsolidationJournalLines_Companies_TenantId_CompanyId",
                        columns: x => new { x.TenantId, x.CompanyId },
                        principalTable: "Companies",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsolidationJournalLines_ConsolidationRuns_TenantId_Consol~",
                        columns: x => new { x.TenantId, x.ConsolidationRunId },
                        principalTable: "ConsolidationRuns",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsolidationTranslationRates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConsolidationRunId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    SourceCurrencyCode = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    TargetCurrencyCode = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    ClosingRate = table.Column<decimal>(type: "numeric(20,10)", precision: 20, scale: 10, nullable: false),
                    AverageRate = table.Column<decimal>(type: "numeric(20,10)", precision: 20, scale: 10, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsolidationTranslationRates", x => x.Id);
                    table.CheckConstraint("CK_ConsolidationTranslationRates_Rates", "\"ClosingRate\" > 0 AND \"AverageRate\" > 0");
                    table.ForeignKey(
                        name: "FK_ConsolidationTranslationRates_Companies_TenantId_CompanyId",
                        columns: x => new { x.TenantId, x.CompanyId },
                        principalTable: "Companies",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsolidationTranslationRates_ConsolidationRuns_TenantId_Co~",
                        columns: x => new { x.TenantId, x.ConsolidationRunId },
                        principalTable: "ConsolidationRuns",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConsolidationTranslationRates_Currencies_SourceCurrencyCode",
                        column: x => x.SourceCurrencyCode,
                        principalTable: "Currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsolidationTranslationRates_Currencies_TargetCurrencyCode",
                        column: x => x.TargetCurrencyCode,
                        principalTable: "Currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BankAccounts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    ChartOfAccountId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    BankName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    AccountNumberLastFour = table.Column<string>(type: "character(4)", fixedLength: true, maxLength: 4, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccounts", x => x.Id);
                    table.UniqueConstraint("AK_BankAccounts_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_BankAccounts_ChartOfAccounts_TenantId_CompanyId_ChartOfAcco~",
                        columns: x => new { x.TenantId, x.CompanyId, x.ChartOfAccountId },
                        principalTable: "ChartOfAccounts",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BankAccounts_Companies_TenantId_CompanyId",
                        columns: x => new { x.TenantId, x.CompanyId },
                        principalTable: "Companies",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BankAccounts_Currencies_CurrencyCode",
                        column: x => x.CurrencyCode,
                        principalTable: "Currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountingPeriodStatusChanges",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    AccountingPeriodId = table.Column<long>(type: "bigint", nullable: false),
                    ChangedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ToStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingPeriodStatusChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountingPeriodStatusChanges_AccountingPeriods_TenantId_Co~",
                        columns: x => new { x.TenantId, x.CompanyId, x.AccountingPeriodId },
                        principalTable: "AccountingPeriods",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountingPeriodStatusChanges_AspNetUsers_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JournalEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    JournalId = table.Column<long>(type: "bigint", nullable: false),
                    AccountingPeriodId = table.Column<long>(type: "bigint", nullable: false),
                    EntryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TotalDebit = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    TotalCredit = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntries", x => x.Id);
                    table.UniqueConstraint("AK_JournalEntries_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_JournalEntries_AccountingPeriods_TenantId_CompanyId_Account~",
                        columns: x => new { x.TenantId, x.CompanyId, x.AccountingPeriodId },
                        principalTable: "AccountingPeriods",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalEntries_Journals_TenantId_CompanyId_JournalId",
                        columns: x => new { x.TenantId, x.CompanyId, x.JournalId },
                        principalTable: "Journals",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BudgetLines",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    BudgetId = table.Column<long>(type: "bigint", nullable: false),
                    AccountingPeriodId = table.Column<long>(type: "bigint", nullable: false),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetLines", x => x.Id);
                    table.CheckConstraint("CK_BudgetLines_Amount", "\"Amount\" >= 0");
                    table.ForeignKey(
                        name: "FK_BudgetLines_AccountingPeriods_TenantId_CompanyId_Accounting~",
                        columns: x => new { x.TenantId, x.CompanyId, x.AccountingPeriodId },
                        principalTable: "AccountingPeriods",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BudgetLines_Budgets_TenantId_CompanyId_BudgetId",
                        columns: x => new { x.TenantId, x.CompanyId, x.BudgetId },
                        principalTable: "Budgets",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BudgetLines_ChartOfAccounts_TenantId_CompanyId_AccountId",
                        columns: x => new { x.TenantId, x.CompanyId, x.AccountId },
                        principalTable: "ChartOfAccounts",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderLines",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    PurchaseOrderId = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderLines", x => x.Id);
                    table.UniqueConstraint("AK_PurchaseOrderLines_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.CheckConstraint("CK_PurchaseOrderLines_Quantities", "\"Quantity\" > 0 AND \"ReceivedQuantity\" >= 0 AND \"ReceivedQuantity\" <= \"Quantity\" AND \"UnitCost\" > 0");
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLines_Items_TenantId_ItemId",
                        columns: x => new { x.TenantId, x.ItemId },
                        principalTable: "Items",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLines_PurchaseOrders_TenantId_CompanyId_Purcha~",
                        columns: x => new { x.TenantId, x.CompanyId, x.PurchaseOrderId },
                        principalTable: "PurchaseOrders",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BankStatementTransactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    BankAccountId = table.Column<long>(type: "bigint", nullable: false),
                    TransactionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    ImportKey = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankStatementTransactions", x => x.Id);
                    table.UniqueConstraint("AK_BankStatementTransactions_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.CheckConstraint("CK_BankStatementTransactions_Amount", "\"Amount\" <> 0");
                    table.ForeignKey(
                        name: "FK_BankStatementTransactions_BankAccounts_TenantId_CompanyId_B~",
                        columns: x => new { x.TenantId, x.CompanyId, x.BankAccountId },
                        principalTable: "BankAccounts",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseReports",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    EmployeeUserId = table.Column<long>(type: "bigint", nullable: false),
                    ReviewerUserId = table.Column<long>(type: "bigint", nullable: true),
                    JournalEntryId = table.Column<long>(type: "bigint", nullable: true),
                    ReimbursementJournalEntryId = table.Column<long>(type: "bigint", nullable: true),
                    EmployeePayableAccountId = table.Column<long>(type: "bigint", nullable: true),
                    ReimbursementCashAccountId = table.Column<long>(type: "bigint", nullable: true),
                    ReportNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ReviewNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PostedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReimbursedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseReports", x => x.Id);
                    table.UniqueConstraint("AK_ExpenseReports_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.CheckConstraint("CK_ExpenseReports_TotalAmount", "\"TotalAmount\" > 0");
                    table.ForeignKey(
                        name: "FK_ExpenseReports_ChartOfAccounts_TenantId_CompanyId_EmployeeP~",
                        columns: x => new { x.TenantId, x.CompanyId, x.EmployeePayableAccountId },
                        principalTable: "ChartOfAccounts",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpenseReports_ChartOfAccounts_TenantId_CompanyId_Reimburse~",
                        columns: x => new { x.TenantId, x.CompanyId, x.ReimbursementCashAccountId },
                        principalTable: "ChartOfAccounts",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpenseReports_Companies_TenantId_CompanyId",
                        columns: x => new { x.TenantId, x.CompanyId },
                        principalTable: "Companies",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpenseReports_Currencies_CurrencyCode",
                        column: x => x.CurrencyCode,
                        principalTable: "Currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpenseReports_JournalEntries_TenantId_CompanyId_JournalEnt~",
                        columns: x => new { x.TenantId, x.CompanyId, x.JournalEntryId },
                        principalTable: "JournalEntries",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpenseReports_JournalEntries_TenantId_CompanyId_Reimbursem~",
                        columns: x => new { x.TenantId, x.CompanyId, x.ReimbursementJournalEntryId },
                        principalTable: "JournalEntries",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FiscalYearClosings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    FiscalYearId = table.Column<long>(type: "bigint", nullable: false),
                    JournalEntryId = table.Column<long>(type: "bigint", nullable: false),
                    RetainedEarningsAccountId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalYearClosings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FiscalYearClosings_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FiscalYearClosings_ChartOfAccounts_TenantId_CompanyId_Retai~",
                        columns: x => new { x.TenantId, x.CompanyId, x.RetainedEarningsAccountId },
                        principalTable: "ChartOfAccounts",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FiscalYearClosings_FiscalYears_TenantId_CompanyId_FiscalYea~",
                        columns: x => new { x.TenantId, x.CompanyId, x.FiscalYearId },
                        principalTable: "FiscalYears",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FiscalYearClosings_JournalEntries_TenantId_CompanyId_Journa~",
                        columns: x => new { x.TenantId, x.CompanyId, x.JournalEntryId },
                        principalTable: "JournalEntries",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FixedAssets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    AssetNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AcquisitionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    InServiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AcquisitionCost = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    ResidualValue = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    UsefulLifeMonths = table.Column<int>(type: "integer", nullable: false),
                    AssetAccountId = table.Column<long>(type: "bigint", nullable: false),
                    AccumulatedDepreciationAccountId = table.Column<long>(type: "bigint", nullable: false),
                    DepreciationExpenseAccountId = table.Column<long>(type: "bigint", nullable: false),
                    AcquisitionJournalEntryId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DisposalDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DisposalProceeds = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: true),
                    DisposalJournalEntryId = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FixedAssets", x => x.Id);
                    table.UniqueConstraint("AK_FixedAssets_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.CheckConstraint("CK_FixedAssets_Amounts", "\"AcquisitionCost\" > 0 AND \"ResidualValue\" >= 0 AND \"ResidualValue\" <= \"AcquisitionCost\" AND \"UsefulLifeMonths\" > 0");
                    table.ForeignKey(
                        name: "FK_FixedAssets_ChartOfAccounts_TenantId_CompanyId_AccumulatedD~",
                        columns: x => new { x.TenantId, x.CompanyId, x.AccumulatedDepreciationAccountId },
                        principalTable: "ChartOfAccounts",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FixedAssets_ChartOfAccounts_TenantId_CompanyId_AssetAccount~",
                        columns: x => new { x.TenantId, x.CompanyId, x.AssetAccountId },
                        principalTable: "ChartOfAccounts",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FixedAssets_ChartOfAccounts_TenantId_CompanyId_Depreciation~",
                        columns: x => new { x.TenantId, x.CompanyId, x.DepreciationExpenseAccountId },
                        principalTable: "ChartOfAccounts",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FixedAssets_Companies_TenantId_CompanyId",
                        columns: x => new { x.TenantId, x.CompanyId },
                        principalTable: "Companies",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FixedAssets_JournalEntries_TenantId_CompanyId_AcquisitionJo~",
                        columns: x => new { x.TenantId, x.CompanyId, x.AcquisitionJournalEntryId },
                        principalTable: "JournalEntries",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FixedAssets_JournalEntries_TenantId_CompanyId_DisposalJourn~",
                        columns: x => new { x.TenantId, x.CompanyId, x.DisposalJournalEntryId },
                        principalTable: "JournalEntries",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryCounts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    WarehouseId = table.Column<long>(type: "bigint", nullable: false),
                    SnapshotMovementId = table.Column<long>(type: "bigint", nullable: false),
                    JournalEntryId = table.Column<long>(type: "bigint", nullable: true),
                    CountNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CountDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCounts", x => x.Id);
                    table.UniqueConstraint("AK_InventoryCounts_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_InventoryCounts_JournalEntries_TenantId_CompanyId_JournalEn~",
                        columns: x => new { x.TenantId, x.CompanyId, x.JournalEntryId },
                        principalTable: "JournalEntries",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryCounts_Warehouses_TenantId_CompanyId_WarehouseId",
                        columns: x => new { x.TenantId, x.CompanyId, x.WarehouseId },
                        principalTable: "Warehouses",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JournalLines",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    JournalEntryId = table.Column<long>(type: "bigint", nullable: false),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    CostCenterId = table.Column<long>(type: "bigint", nullable: true),
                    TransactionCurrencyCode = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    Debit = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Credit = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    FunctionalDebit = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    FunctionalCredit = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(20,10)", precision: 20, scale: 10, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalLines", x => x.Id);
                    table.UniqueConstraint("AK_JournalLines_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.CheckConstraint("CK_JournalLines_Amounts", "\"Debit\" >= 0 AND \"Credit\" >= 0 AND ((\"Debit\" > 0 AND \"Credit\" = 0) OR (\"Credit\" > 0 AND \"Debit\" = 0))");
                    table.ForeignKey(
                        name: "FK_JournalLines_ChartOfAccounts_TenantId_CompanyId_AccountId",
                        columns: x => new { x.TenantId, x.CompanyId, x.AccountId },
                        principalTable: "ChartOfAccounts",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalLines_CostCenters_TenantId_CompanyId_CostCenterId",
                        columns: x => new { x.TenantId, x.CompanyId, x.CostCenterId },
                        principalTable: "CostCenters",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalLines_Currencies_TransactionCurrencyCode",
                        column: x => x.TransactionCurrencyCode,
                        principalTable: "Currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalLines_JournalEntries_TenantId_CompanyId_JournalEntry~",
                        columns: x => new { x.TenantId, x.CompanyId, x.JournalEntryId },
                        principalTable: "JournalEntries",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    PartyId = table.Column<long>(type: "bigint", nullable: false),
                    JournalEntryId = table.Column<long>(type: "bigint", nullable: true),
                    PaymentNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Direction = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    PaymentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(20,10)", precision: 20, scale: 10, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.UniqueConstraint("AK_Payments_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.CheckConstraint("CK_Payments_Amounts", "\"Amount\" > 0 AND \"AllocatedAmount\" >= 0 AND \"AllocatedAmount\" <= \"Amount\"");
                    table.CheckConstraint("CK_Payments_Direction", "\"Direction\" IN ('Receive', 'Pay')");
                    table.ForeignKey(
                        name: "FK_Payments_Companies_TenantId_CompanyId",
                        columns: x => new { x.TenantId, x.CompanyId },
                        principalTable: "Companies",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_Currencies_CurrencyCode",
                        column: x => x.CurrencyCode,
                        principalTable: "Currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_JournalEntries_TenantId_CompanyId_JournalEntryId",
                        columns: x => new { x.TenantId, x.CompanyId, x.JournalEntryId },
                        principalTable: "JournalEntries",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_Parties_TenantId_PartyId",
                        columns: x => new { x.TenantId, x.PartyId },
                        principalTable: "Parties",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseBills",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    SupplierId = table.Column<long>(type: "bigint", nullable: false),
                    JournalEntryId = table.Column<long>(type: "bigint", nullable: true),
                    BillNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    BillDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(20,10)", precision: 20, scale: 10, nullable: false),
                    NetTotal = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    TaxTotal = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    GrandTotal = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    AllocatedTotal = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseBills", x => x.Id);
                    table.UniqueConstraint("AK_PurchaseBills_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.CheckConstraint("CK_PurchaseBills_Totals", "\"NetTotal\" >= 0 AND \"TaxTotal\" >= 0 AND \"GrandTotal\" = \"NetTotal\" + \"TaxTotal\" AND \"AllocatedTotal\" >= 0 AND \"AllocatedTotal\" <= \"GrandTotal\"");
                    table.ForeignKey(
                        name: "FK_PurchaseBills_Companies_TenantId_CompanyId",
                        columns: x => new { x.TenantId, x.CompanyId },
                        principalTable: "Companies",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseBills_Currencies_CurrencyCode",
                        column: x => x.CurrencyCode,
                        principalTable: "Currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseBills_JournalEntries_TenantId_CompanyId_JournalEntr~",
                        columns: x => new { x.TenantId, x.CompanyId, x.JournalEntryId },
                        principalTable: "JournalEntries",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseBills_Parties_TenantId_SupplierId",
                        columns: x => new { x.TenantId, x.SupplierId },
                        principalTable: "Parties",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesInvoices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    JournalEntryId = table.Column<long>(type: "bigint", nullable: true),
                    InvoiceNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(20,10)", precision: 20, scale: 10, nullable: false),
                    NetTotal = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    TaxTotal = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    GrandTotal = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    AllocatedTotal = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesInvoices", x => x.Id);
                    table.UniqueConstraint("AK_SalesInvoices_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.CheckConstraint("CK_SalesInvoices_Totals", "\"NetTotal\" >= 0 AND \"TaxTotal\" >= 0 AND \"GrandTotal\" = \"NetTotal\" + \"TaxTotal\" AND \"AllocatedTotal\" >= 0 AND \"AllocatedTotal\" <= \"GrandTotal\"");
                    table.ForeignKey(
                        name: "FK_SalesInvoices_Companies_TenantId_CompanyId",
                        columns: x => new { x.TenantId, x.CompanyId },
                        principalTable: "Companies",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesInvoices_Currencies_CurrencyCode",
                        column: x => x.CurrencyCode,
                        principalTable: "Currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesInvoices_JournalEntries_TenantId_CompanyId_JournalEntr~",
                        columns: x => new { x.TenantId, x.CompanyId, x.JournalEntryId },
                        principalTable: "JournalEntries",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesInvoices_Parties_TenantId_CustomerId",
                        columns: x => new { x.TenantId, x.CustomerId },
                        principalTable: "Parties",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<long>(type: "bigint", nullable: false),
                    WarehouseId = table.Column<long>(type: "bigint", nullable: false),
                    WarehouseLocationId = table.Column<long>(type: "bigint", nullable: true),
                    JournalEntryId = table.Column<long>(type: "bigint", nullable: true),
                    PurchaseOrderLineId = table.Column<long>(type: "bigint", nullable: true),
                    MovementNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    MovementType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MovementDate = table.Column<DateOnly>(type: "date", nullable: false),
                    QuantityIn = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    QuantityOut = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                    table.UniqueConstraint("AK_StockMovements_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.CheckConstraint("CK_StockMovements_Cost", "\"UnitCost\" >= 0 AND \"TotalCost\" >= 0");
                    table.CheckConstraint("CK_StockMovements_Quantities", "\"QuantityIn\" >= 0 AND \"QuantityOut\" >= 0 AND ((\"QuantityIn\" > 0 AND \"QuantityOut\" = 0) OR (\"QuantityOut\" > 0 AND \"QuantityIn\" = 0))");
                    table.ForeignKey(
                        name: "FK_StockMovements_Items_TenantId_ItemId",
                        columns: x => new { x.TenantId, x.ItemId },
                        principalTable: "Items",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_JournalEntries_TenantId_CompanyId_JournalEnt~",
                        columns: x => new { x.TenantId, x.CompanyId, x.JournalEntryId },
                        principalTable: "JournalEntries",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_PurchaseOrderLines_TenantId_CompanyId_Purcha~",
                        columns: x => new { x.TenantId, x.CompanyId, x.PurchaseOrderLineId },
                        principalTable: "PurchaseOrderLines",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_WarehouseLocations_TenantId_CompanyId_Wareho~",
                        columns: x => new { x.TenantId, x.CompanyId, x.WarehouseId, x.WarehouseLocationId },
                        principalTable: "WarehouseLocations",
                        principalColumns: new[] { "TenantId", "CompanyId", "WarehouseId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_Warehouses_TenantId_CompanyId_WarehouseId",
                        columns: x => new { x.TenantId, x.CompanyId, x.WarehouseId },
                        principalTable: "Warehouses",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseReportLines",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    ExpenseReportId = table.Column<long>(type: "bigint", nullable: false),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    ExpenseDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Merchant = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    ReceiptFileName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseReportLines", x => x.Id);
                    table.CheckConstraint("CK_ExpenseReportLines_Amount", "\"Amount\" > 0");
                    table.ForeignKey(
                        name: "FK_ExpenseReportLines_ChartOfAccounts_TenantId_CompanyId_Accou~",
                        columns: x => new { x.TenantId, x.CompanyId, x.AccountId },
                        principalTable: "ChartOfAccounts",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpenseReportLines_ExpenseReports_TenantId_CompanyId_Expens~",
                        columns: x => new { x.TenantId, x.CompanyId, x.ExpenseReportId },
                        principalTable: "ExpenseReports",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FixedAssetDepreciations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    FixedAssetId = table.Column<long>(type: "bigint", nullable: false),
                    AccountingPeriodId = table.Column<long>(type: "bigint", nullable: false),
                    JournalEntryId = table.Column<long>(type: "bigint", nullable: false),
                    DepreciationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FixedAssetDepreciations", x => x.Id);
                    table.CheckConstraint("CK_FixedAssetDepreciations_Amount", "\"Amount\" > 0");
                    table.ForeignKey(
                        name: "FK_FixedAssetDepreciations_AccountingPeriods_TenantId_CompanyI~",
                        columns: x => new { x.TenantId, x.CompanyId, x.AccountingPeriodId },
                        principalTable: "AccountingPeriods",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FixedAssetDepreciations_FixedAssets_TenantId_CompanyId_Fixe~",
                        columns: x => new { x.TenantId, x.CompanyId, x.FixedAssetId },
                        principalTable: "FixedAssets",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FixedAssetDepreciations_JournalEntries_TenantId_CompanyId_J~",
                        columns: x => new { x.TenantId, x.CompanyId, x.JournalEntryId },
                        principalTable: "JournalEntries",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryCountLines",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    InventoryCountId = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<long>(type: "bigint", nullable: false),
                    SystemQuantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    SystemValue = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    CountedQuantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    AdjustmentUnitCost = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: true),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCountLines", x => x.Id);
                    table.CheckConstraint("CK_InventoryCountLines_Quantities", "\"SystemQuantity\" >= 0 AND \"SystemValue\" >= 0 AND \"CountedQuantity\" >= 0 AND (\"AdjustmentUnitCost\" IS NULL OR \"AdjustmentUnitCost\" > 0)");
                    table.ForeignKey(
                        name: "FK_InventoryCountLines_InventoryCounts_TenantId_CompanyId_Inve~",
                        columns: x => new { x.TenantId, x.CompanyId, x.InventoryCountId },
                        principalTable: "InventoryCounts",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryCountLines_Items_TenantId_ItemId",
                        columns: x => new { x.TenantId, x.ItemId },
                        principalTable: "Items",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BankReconciliationMatches",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    BankAccountId = table.Column<long>(type: "bigint", nullable: false),
                    BankStatementTransactionId = table.Column<long>(type: "bigint", nullable: false),
                    JournalLineId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankReconciliationMatches", x => x.Id);
                    table.UniqueConstraint("AK_BankReconciliationMatches_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.CheckConstraint("CK_BankReconciliationMatches_Amount", "\"Amount\" <> 0");
                    table.ForeignKey(
                        name: "FK_BankReconciliationMatches_BankAccounts_TenantId_CompanyId_B~",
                        columns: x => new { x.TenantId, x.CompanyId, x.BankAccountId },
                        principalTable: "BankAccounts",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BankReconciliationMatches_BankStatementTransactions_TenantI~",
                        columns: x => new { x.TenantId, x.CompanyId, x.BankStatementTransactionId },
                        principalTable: "BankStatementTransactions",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BankReconciliationMatches_JournalLines_TenantId_CompanyId_J~",
                        columns: x => new { x.TenantId, x.CompanyId, x.JournalLineId },
                        principalTable: "JournalLines",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseBillLines",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    PurchaseBillId = table.Column<long>(type: "bigint", nullable: false),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    TaxCodeId = table.Column<long>(type: "bigint", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    TaxRatePercent = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    GrossAmount = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseBillLines", x => x.Id);
                    table.CheckConstraint("CK_PurchaseBillLines_Amounts", "\"Quantity\" > 0 AND \"UnitPrice\" >= 0 AND \"NetAmount\" >= 0 AND \"TaxAmount\" >= 0 AND \"GrossAmount\" = \"NetAmount\" + \"TaxAmount\"");
                    table.ForeignKey(
                        name: "FK_PurchaseBillLines_ChartOfAccounts_TenantId_CompanyId_Accoun~",
                        columns: x => new { x.TenantId, x.CompanyId, x.AccountId },
                        principalTable: "ChartOfAccounts",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseBillLines_PurchaseBills_TenantId_CompanyId_Purchase~",
                        columns: x => new { x.TenantId, x.CompanyId, x.PurchaseBillId },
                        principalTable: "PurchaseBills",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseBillLines_TaxCodes_TaxCodeId",
                        column: x => x.TaxCodeId,
                        principalTable: "TaxCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentAllocations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    PaymentId = table.Column<long>(type: "bigint", nullable: false),
                    SalesInvoiceId = table.Column<long>(type: "bigint", nullable: true),
                    PurchaseBillId = table.Column<long>(type: "bigint", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAllocations", x => x.Id);
                    table.CheckConstraint("CK_PaymentAllocations_Amount", "\"Amount\" > 0");
                    table.CheckConstraint("CK_PaymentAllocations_Target", "(\"SalesInvoiceId\" IS NOT NULL AND \"PurchaseBillId\" IS NULL) OR (\"SalesInvoiceId\" IS NULL AND \"PurchaseBillId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_PaymentAllocations_Payments_TenantId_CompanyId_PaymentId",
                        columns: x => new { x.TenantId, x.CompanyId, x.PaymentId },
                        principalTable: "Payments",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentAllocations_PurchaseBills_TenantId_CompanyId_Purchas~",
                        columns: x => new { x.TenantId, x.CompanyId, x.PurchaseBillId },
                        principalTable: "PurchaseBills",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentAllocations_SalesInvoices_TenantId_CompanyId_SalesIn~",
                        columns: x => new { x.TenantId, x.CompanyId, x.SalesInvoiceId },
                        principalTable: "SalesInvoices",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesCreditNotes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    SalesInvoiceId = table.Column<long>(type: "bigint", nullable: false),
                    JournalEntryId = table.Column<long>(type: "bigint", nullable: true),
                    ReceivableAccountId = table.Column<long>(type: "bigint", nullable: true),
                    CreditNoteNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreditNoteDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    NetTotal = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    TaxTotal = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    GrandTotal = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesCreditNotes", x => x.Id);
                    table.UniqueConstraint("AK_SalesCreditNotes_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.CheckConstraint("CK_SalesCreditNotes_Totals", "\"NetTotal\" >= 0 AND \"TaxTotal\" >= 0 AND \"GrandTotal\" = \"NetTotal\" + \"TaxTotal\"");
                    table.ForeignKey(
                        name: "FK_SalesCreditNotes_ChartOfAccounts_TenantId_CompanyId_Receiva~",
                        columns: x => new { x.TenantId, x.CompanyId, x.ReceivableAccountId },
                        principalTable: "ChartOfAccounts",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesCreditNotes_Companies_TenantId_CompanyId",
                        columns: x => new { x.TenantId, x.CompanyId },
                        principalTable: "Companies",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesCreditNotes_JournalEntries_TenantId_CompanyId_JournalE~",
                        columns: x => new { x.TenantId, x.CompanyId, x.JournalEntryId },
                        principalTable: "JournalEntries",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesCreditNotes_SalesInvoices_TenantId_CompanyId_SalesInvo~",
                        columns: x => new { x.TenantId, x.CompanyId, x.SalesInvoiceId },
                        principalTable: "SalesInvoices",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesInvoiceLines",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    SalesInvoiceId = table.Column<long>(type: "bigint", nullable: false),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    TaxCodeId = table.Column<long>(type: "bigint", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    TaxRatePercent = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    GrossAmount = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesInvoiceLines", x => x.Id);
                    table.UniqueConstraint("AK_SalesInvoiceLines_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.CheckConstraint("CK_SalesInvoiceLines_Amounts", "\"Quantity\" > 0 AND \"UnitPrice\" >= 0 AND \"NetAmount\" >= 0 AND \"TaxAmount\" >= 0 AND \"GrossAmount\" = \"NetAmount\" + \"TaxAmount\"");
                    table.ForeignKey(
                        name: "FK_SalesInvoiceLines_ChartOfAccounts_TenantId_CompanyId_Accoun~",
                        columns: x => new { x.TenantId, x.CompanyId, x.AccountId },
                        principalTable: "ChartOfAccounts",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesInvoiceLines_SalesInvoices_TenantId_CompanyId_SalesInv~",
                        columns: x => new { x.TenantId, x.CompanyId, x.SalesInvoiceId },
                        principalTable: "SalesInvoices",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesInvoiceLines_TaxCodes_TaxCodeId",
                        column: x => x.TaxCodeId,
                        principalTable: "TaxCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesQuotes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    SalesInvoiceId = table.Column<long>(type: "bigint", nullable: true),
                    QuoteNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    QuoteDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidUntil = table.Column<DateOnly>(type: "date", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(20,10)", precision: 20, scale: 10, nullable: false),
                    NetTotal = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    TaxTotal = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    GrandTotal = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SentTo = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesQuotes", x => x.Id);
                    table.UniqueConstraint("AK_SalesQuotes_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.CheckConstraint("CK_SalesQuotes_Totals", "\"NetTotal\" >= 0 AND \"TaxTotal\" >= 0 AND \"GrandTotal\" = \"NetTotal\" + \"TaxTotal\"");
                    table.ForeignKey(
                        name: "FK_SalesQuotes_Companies_TenantId_CompanyId",
                        columns: x => new { x.TenantId, x.CompanyId },
                        principalTable: "Companies",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesQuotes_Currencies_CurrencyCode",
                        column: x => x.CurrencyCode,
                        principalTable: "Currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesQuotes_Parties_TenantId_CustomerId",
                        columns: x => new { x.TenantId, x.CustomerId },
                        principalTable: "Parties",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesQuotes_SalesInvoices_TenantId_CompanyId_SalesInvoiceId",
                        columns: x => new { x.TenantId, x.CompanyId, x.SalesInvoiceId },
                        principalTable: "SalesInvoices",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryValuationLayers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    StockMovementId = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<long>(type: "bigint", nullable: false),
                    WarehouseId = table.Column<long>(type: "bigint", nullable: false),
                    RemainingQuantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryValuationLayers", x => x.Id);
                    table.CheckConstraint("CK_InventoryValuationLayers_Amounts", "\"RemainingQuantity\" >= 0 AND \"UnitCost\" >= 0");
                    table.ForeignKey(
                        name: "FK_InventoryValuationLayers_Items_TenantId_ItemId",
                        columns: x => new { x.TenantId, x.ItemId },
                        principalTable: "Items",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryValuationLayers_StockMovements_TenantId_CompanyId_~",
                        columns: x => new { x.TenantId, x.CompanyId, x.StockMovementId },
                        principalTable: "StockMovements",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryValuationLayers_Warehouses_TenantId_CompanyId_Ware~",
                        columns: x => new { x.TenantId, x.CompanyId, x.WarehouseId },
                        principalTable: "Warehouses",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerRefunds",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    SalesCreditNoteId = table.Column<long>(type: "bigint", nullable: false),
                    JournalEntryId = table.Column<long>(type: "bigint", nullable: false),
                    RefundNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RefundDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    FunctionalAmount = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerRefunds", x => x.Id);
                    table.UniqueConstraint("AK_CustomerRefunds_TenantId_CompanyId_Id", x => new { x.TenantId, x.CompanyId, x.Id });
                    table.CheckConstraint("CK_CustomerRefunds_Amounts", "\"Amount\" > 0 AND \"FunctionalAmount\" > 0");
                    table.ForeignKey(
                        name: "FK_CustomerRefunds_JournalEntries_TenantId_CompanyId_JournalEn~",
                        columns: x => new { x.TenantId, x.CompanyId, x.JournalEntryId },
                        principalTable: "JournalEntries",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerRefunds_SalesCreditNotes_TenantId_CompanyId_SalesCr~",
                        columns: x => new { x.TenantId, x.CompanyId, x.SalesCreditNoteId },
                        principalTable: "SalesCreditNotes",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesCreditNoteLines",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    SalesCreditNoteId = table.Column<long>(type: "bigint", nullable: false),
                    SalesInvoiceLineId = table.Column<long>(type: "bigint", nullable: false),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    TaxCodeId = table.Column<long>(type: "bigint", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    TaxRatePercent = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    GrossAmount = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesCreditNoteLines", x => x.Id);
                    table.CheckConstraint("CK_SalesCreditNoteLines_Amounts", "\"Quantity\" > 0 AND \"UnitPrice\" >= 0 AND \"NetAmount\" >= 0 AND \"TaxAmount\" >= 0 AND \"GrossAmount\" = \"NetAmount\" + \"TaxAmount\"");
                    table.ForeignKey(
                        name: "FK_SalesCreditNoteLines_ChartOfAccounts_TenantId_CompanyId_Acc~",
                        columns: x => new { x.TenantId, x.CompanyId, x.AccountId },
                        principalTable: "ChartOfAccounts",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesCreditNoteLines_SalesCreditNotes_TenantId_CompanyId_Sa~",
                        columns: x => new { x.TenantId, x.CompanyId, x.SalesCreditNoteId },
                        principalTable: "SalesCreditNotes",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesCreditNoteLines_SalesInvoiceLines_TenantId_CompanyId_S~",
                        columns: x => new { x.TenantId, x.CompanyId, x.SalesInvoiceLineId },
                        principalTable: "SalesInvoiceLines",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesCreditNoteLines_TaxCodes_TaxCodeId",
                        column: x => x.TaxCodeId,
                        principalTable: "TaxCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesQuoteLines",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    SalesQuoteId = table.Column<long>(type: "bigint", nullable: false),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    TaxCodeId = table.Column<long>(type: "bigint", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    TaxRatePercent = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    GrossAmount = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesQuoteLines", x => x.Id);
                    table.CheckConstraint("CK_SalesQuoteLines_Amounts", "\"Quantity\" > 0 AND \"UnitPrice\" >= 0 AND \"NetAmount\" >= 0 AND \"TaxAmount\" >= 0 AND \"GrossAmount\" = \"NetAmount\" + \"TaxAmount\"");
                    table.ForeignKey(
                        name: "FK_SalesQuoteLines_ChartOfAccounts_TenantId_CompanyId_AccountId",
                        columns: x => new { x.TenantId, x.CompanyId, x.AccountId },
                        principalTable: "ChartOfAccounts",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesQuoteLines_SalesQuotes_TenantId_CompanyId_SalesQuoteId",
                        columns: x => new { x.TenantId, x.CompanyId, x.SalesQuoteId },
                        principalTable: "SalesQuotes",
                        principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesQuoteLines_TaxCodes_TaxCodeId",
                        column: x => x.TaxCodeId,
                        principalTable: "TaxCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Currencies",
                columns: new[] { "Code", "DecimalPlaces", "Name" },
                values: new object[,]
                {
                    { "AUD", (byte)2, "Australian Dollar" },
                    { "BDT", (byte)2, "Bangladeshi Taka" },
                    { "CAD", (byte)2, "Canadian Dollar" },
                    { "CHF", (byte)2, "Swiss Franc" },
                    { "EUR", (byte)2, "Euro" },
                    { "GBP", (byte)2, "Pound Sterling" },
                    { "INR", (byte)2, "Indian Rupee" },
                    { "JPY", (byte)0, "Japanese Yen" },
                    { "SGD", (byte)2, "Singapore Dollar" },
                    { "USD", (byte)2, "US Dollar" }
                });

            migrationBuilder.InsertData(
                table: "TaxJurisdictions",
                columns: new[] { "Id", "Code", "CountryCode" },
                values: new object[,]
                {
                    { 4L, "AF", "AF" },
                    { 8L, "AL", "AL" },
                    { 10L, "AQ", "AQ" },
                    { 12L, "DZ", "DZ" },
                    { 16L, "AS", "AS" },
                    { 20L, "AD", "AD" },
                    { 24L, "AO", "AO" },
                    { 28L, "AG", "AG" },
                    { 31L, "AZ", "AZ" },
                    { 32L, "AR", "AR" },
                    { 36L, "AU", "AU" },
                    { 40L, "AT", "AT" },
                    { 44L, "BS", "BS" },
                    { 48L, "BH", "BH" },
                    { 50L, "BD", "BD" },
                    { 51L, "AM", "AM" },
                    { 52L, "BB", "BB" },
                    { 56L, "BE", "BE" },
                    { 60L, "BM", "BM" },
                    { 64L, "BT", "BT" },
                    { 68L, "BO", "BO" },
                    { 70L, "BA", "BA" },
                    { 72L, "BW", "BW" },
                    { 74L, "BV", "BV" },
                    { 76L, "BR", "BR" },
                    { 84L, "BZ", "BZ" },
                    { 86L, "IO", "IO" },
                    { 90L, "SB", "SB" },
                    { 92L, "VG", "VG" },
                    { 96L, "BN", "BN" },
                    { 100L, "BG", "BG" },
                    { 104L, "MM", "MM" },
                    { 108L, "BI", "BI" },
                    { 112L, "BY", "BY" },
                    { 116L, "KH", "KH" },
                    { 120L, "CM", "CM" },
                    { 124L, "CA", "CA" },
                    { 132L, "CV", "CV" },
                    { 136L, "KY", "KY" },
                    { 140L, "CF", "CF" },
                    { 144L, "LK", "LK" },
                    { 148L, "TD", "TD" },
                    { 152L, "CL", "CL" },
                    { 156L, "CN", "CN" },
                    { 158L, "TW", "TW" },
                    { 162L, "CX", "CX" },
                    { 166L, "CC", "CC" },
                    { 170L, "CO", "CO" },
                    { 174L, "KM", "KM" },
                    { 175L, "YT", "YT" },
                    { 178L, "CG", "CG" },
                    { 180L, "CD", "CD" },
                    { 184L, "CK", "CK" },
                    { 188L, "CR", "CR" },
                    { 191L, "HR", "HR" },
                    { 192L, "CU", "CU" },
                    { 196L, "CY", "CY" },
                    { 203L, "CZ", "CZ" },
                    { 204L, "BJ", "BJ" },
                    { 208L, "DK", "DK" },
                    { 212L, "DM", "DM" },
                    { 214L, "DO", "DO" },
                    { 218L, "EC", "EC" },
                    { 222L, "SV", "SV" },
                    { 226L, "GQ", "GQ" },
                    { 231L, "ET", "ET" },
                    { 232L, "ER", "ER" },
                    { 233L, "EE", "EE" },
                    { 234L, "FO", "FO" },
                    { 238L, "FK", "FK" },
                    { 239L, "GS", "GS" },
                    { 242L, "FJ", "FJ" },
                    { 246L, "FI", "FI" },
                    { 248L, "AX", "AX" },
                    { 250L, "FR", "FR" },
                    { 254L, "GF", "GF" },
                    { 258L, "PF", "PF" },
                    { 260L, "TF", "TF" },
                    { 262L, "DJ", "DJ" },
                    { 266L, "GA", "GA" },
                    { 268L, "GE", "GE" },
                    { 270L, "GM", "GM" },
                    { 275L, "PS", "PS" },
                    { 276L, "DE", "DE" },
                    { 288L, "GH", "GH" },
                    { 292L, "GI", "GI" },
                    { 296L, "KI", "KI" },
                    { 300L, "GR", "GR" },
                    { 304L, "GL", "GL" },
                    { 308L, "GD", "GD" },
                    { 312L, "GP", "GP" },
                    { 316L, "GU", "GU" },
                    { 320L, "GT", "GT" },
                    { 324L, "GN", "GN" },
                    { 328L, "GY", "GY" },
                    { 332L, "HT", "HT" },
                    { 334L, "HM", "HM" },
                    { 336L, "VA", "VA" },
                    { 340L, "HN", "HN" },
                    { 344L, "HK", "HK" },
                    { 348L, "HU", "HU" },
                    { 352L, "IS", "IS" },
                    { 356L, "IN", "IN" },
                    { 360L, "ID", "ID" },
                    { 364L, "IR", "IR" },
                    { 368L, "IQ", "IQ" },
                    { 372L, "IE", "IE" },
                    { 376L, "IL", "IL" },
                    { 380L, "IT", "IT" },
                    { 384L, "CI", "CI" },
                    { 388L, "JM", "JM" },
                    { 392L, "JP", "JP" },
                    { 398L, "KZ", "KZ" },
                    { 400L, "JO", "JO" },
                    { 404L, "KE", "KE" },
                    { 408L, "KP", "KP" },
                    { 410L, "KR", "KR" },
                    { 414L, "KW", "KW" },
                    { 417L, "KG", "KG" },
                    { 418L, "LA", "LA" },
                    { 422L, "LB", "LB" },
                    { 426L, "LS", "LS" },
                    { 428L, "LV", "LV" },
                    { 430L, "LR", "LR" },
                    { 434L, "LY", "LY" },
                    { 438L, "LI", "LI" },
                    { 440L, "LT", "LT" },
                    { 442L, "LU", "LU" },
                    { 446L, "MO", "MO" },
                    { 450L, "MG", "MG" },
                    { 454L, "MW", "MW" },
                    { 458L, "MY", "MY" },
                    { 462L, "MV", "MV" },
                    { 466L, "ML", "ML" },
                    { 470L, "MT", "MT" },
                    { 474L, "MQ", "MQ" },
                    { 478L, "MR", "MR" },
                    { 480L, "MU", "MU" },
                    { 484L, "MX", "MX" },
                    { 492L, "MC", "MC" },
                    { 496L, "MN", "MN" },
                    { 498L, "MD", "MD" },
                    { 499L, "ME", "ME" },
                    { 500L, "MS", "MS" },
                    { 504L, "MA", "MA" },
                    { 508L, "MZ", "MZ" },
                    { 512L, "OM", "OM" },
                    { 516L, "NA", "NA" },
                    { 520L, "NR", "NR" },
                    { 524L, "NP", "NP" },
                    { 528L, "NL", "NL" },
                    { 531L, "CW", "CW" },
                    { 533L, "AW", "AW" },
                    { 534L, "SX", "SX" },
                    { 535L, "BQ", "BQ" },
                    { 540L, "NC", "NC" },
                    { 548L, "VU", "VU" },
                    { 554L, "NZ", "NZ" },
                    { 558L, "NI", "NI" },
                    { 562L, "NE", "NE" },
                    { 566L, "NG", "NG" },
                    { 570L, "NU", "NU" },
                    { 574L, "NF", "NF" },
                    { 578L, "NO", "NO" },
                    { 580L, "MP", "MP" },
                    { 581L, "UM", "UM" },
                    { 583L, "FM", "FM" },
                    { 584L, "MH", "MH" },
                    { 585L, "PW", "PW" },
                    { 586L, "PK", "PK" },
                    { 591L, "PA", "PA" },
                    { 598L, "PG", "PG" },
                    { 600L, "PY", "PY" },
                    { 604L, "PE", "PE" },
                    { 608L, "PH", "PH" },
                    { 612L, "PN", "PN" },
                    { 616L, "PL", "PL" },
                    { 620L, "PT", "PT" },
                    { 624L, "GW", "GW" },
                    { 626L, "TL", "TL" },
                    { 630L, "PR", "PR" },
                    { 634L, "QA", "QA" },
                    { 638L, "RE", "RE" },
                    { 642L, "RO", "RO" },
                    { 643L, "RU", "RU" },
                    { 646L, "RW", "RW" },
                    { 652L, "BL", "BL" },
                    { 654L, "SH", "SH" },
                    { 659L, "KN", "KN" },
                    { 660L, "AI", "AI" },
                    { 662L, "LC", "LC" },
                    { 663L, "MF", "MF" },
                    { 666L, "PM", "PM" },
                    { 670L, "VC", "VC" },
                    { 674L, "SM", "SM" },
                    { 678L, "ST", "ST" },
                    { 682L, "SA", "SA" },
                    { 686L, "SN", "SN" },
                    { 688L, "RS", "RS" },
                    { 690L, "SC", "SC" },
                    { 694L, "SL", "SL" },
                    { 702L, "SG", "SG" },
                    { 703L, "SK", "SK" },
                    { 704L, "VN", "VN" },
                    { 705L, "SI", "SI" },
                    { 706L, "SO", "SO" },
                    { 710L, "ZA", "ZA" },
                    { 716L, "ZW", "ZW" },
                    { 724L, "ES", "ES" },
                    { 728L, "SS", "SS" },
                    { 729L, "SD", "SD" },
                    { 732L, "EH", "EH" },
                    { 740L, "SR", "SR" },
                    { 744L, "SJ", "SJ" },
                    { 748L, "SZ", "SZ" },
                    { 752L, "SE", "SE" },
                    { 756L, "CH", "CH" },
                    { 760L, "SY", "SY" },
                    { 762L, "TJ", "TJ" },
                    { 764L, "TH", "TH" },
                    { 768L, "TG", "TG" },
                    { 772L, "TK", "TK" },
                    { 776L, "TO", "TO" },
                    { 780L, "TT", "TT" },
                    { 784L, "AE", "AE" },
                    { 788L, "TN", "TN" },
                    { 792L, "TR", "TR" },
                    { 795L, "TM", "TM" },
                    { 796L, "TC", "TC" },
                    { 798L, "TV", "TV" },
                    { 800L, "UG", "UG" },
                    { 804L, "UA", "UA" },
                    { 807L, "MK", "MK" },
                    { 818L, "EG", "EG" },
                    { 826L, "GB", "GB" },
                    { 831L, "GG", "GG" },
                    { 832L, "JE", "JE" },
                    { 833L, "IM", "IM" },
                    { 834L, "TZ", "TZ" },
                    { 840L, "US", "US" },
                    { 850L, "VI", "VI" },
                    { 854L, "BF", "BF" },
                    { 858L, "UY", "UY" },
                    { 860L, "UZ", "UZ" },
                    { 862L, "VE", "VE" },
                    { 876L, "WF", "WF" },
                    { 882L, "WS", "WS" },
                    { 887L, "YE", "YE" },
                    { 894L, "ZM", "ZM" }
                });

            migrationBuilder.InsertData(
                table: "UnitsOfMeasure",
                columns: new[] { "Code", "DecimalPlaces", "Name" },
                values: new object[,]
                {
                    { "BOX", (byte)0, "Box" },
                    { "EA", (byte)0, "Each" },
                    { "HOUR", (byte)2, "Hour" },
                    { "KG", (byte)3, "Kilogram" },
                    { "L", (byte)3, "Litre" },
                    { "M", (byte)3, "Metre" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountGroups_TenantId_CompanyId_Code",
                table: "AccountGroups",
                columns: new[] { "TenantId", "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPeriods_TenantId_CompanyId_FiscalYearId_StartsOn",
                table: "AccountingPeriods",
                columns: new[] { "TenantId", "CompanyId", "FiscalYearId", "StartsOn" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPeriodStatusChanges_ChangedByUserId",
                table: "AccountingPeriodStatusChanges",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPeriodStatusChanges_TenantId_CompanyId_Accounting~",
                table: "AccountingPeriodStatusChanges",
                columns: new[] { "TenantId", "CompanyId", "AccountingPeriodId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TenantId_CreatedAt",
                table: "AuditLogs",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_CurrencyCode",
                table: "BankAccounts",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_TenantId_CompanyId_ChartOfAccountId",
                table: "BankAccounts",
                columns: new[] { "TenantId", "CompanyId", "ChartOfAccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankReconciliationMatches_TenantId_CompanyId_BankAccountId",
                table: "BankReconciliationMatches",
                columns: new[] { "TenantId", "CompanyId", "BankAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_BankReconciliationMatches_TenantId_CompanyId_BankStatementT~",
                table: "BankReconciliationMatches",
                columns: new[] { "TenantId", "CompanyId", "BankStatementTransactionId", "JournalLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankReconciliationMatches_TenantId_CompanyId_JournalLineId",
                table: "BankReconciliationMatches",
                columns: new[] { "TenantId", "CompanyId", "JournalLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_BankStatementTransactions_TenantId_CompanyId_BankAccountId_~",
                table: "BankStatementTransactions",
                columns: new[] { "TenantId", "CompanyId", "BankAccountId", "ImportKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLines_TenantId_CompanyId_AccountId",
                table: "BudgetLines",
                columns: new[] { "TenantId", "CompanyId", "AccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLines_TenantId_CompanyId_AccountingPeriodId",
                table: "BudgetLines",
                columns: new[] { "TenantId", "CompanyId", "AccountingPeriodId" });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLines_TenantId_CompanyId_BudgetId_AccountingPeriodId_~",
                table: "BudgetLines",
                columns: new[] { "TenantId", "CompanyId", "BudgetId", "AccountingPeriodId", "AccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_TenantId_CompanyId_FiscalYearId_Name",
                table: "Budgets",
                columns: new[] { "TenantId", "CompanyId", "FiscalYearId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChartOfAccounts_TenantId_CompanyId_AccountGroupId",
                table: "ChartOfAccounts",
                columns: new[] { "TenantId", "CompanyId", "AccountGroupId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChartOfAccounts_TenantId_CompanyId_Code",
                table: "ChartOfAccounts",
                columns: new[] { "TenantId", "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChartOfAccounts_TenantId_CompanyId_ParentId",
                table: "ChartOfAccounts",
                columns: new[] { "TenantId", "CompanyId", "ParentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Companies_FunctionalCurrencyCode",
                table: "Companies",
                column: "FunctionalCurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_TenantId_Code",
                table: "Companies",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyUsers_TenantId_UserId",
                table: "CompanyUsers",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidationGroupCompanies_TenantId_CompanyId",
                table: "ConsolidationGroupCompanies",
                columns: new[] { "TenantId", "CompanyId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidationGroups_ReportingCurrencyCode",
                table: "ConsolidationGroups",
                column: "ReportingCurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidationGroups_TenantId_Code",
                table: "ConsolidationGroups",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidationJournalLines_TenantId_CompanyId",
                table: "ConsolidationJournalLines",
                columns: new[] { "TenantId", "CompanyId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidationJournalLines_TenantId_ConsolidationRunId",
                table: "ConsolidationJournalLines",
                columns: new[] { "TenantId", "ConsolidationRunId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidationRuns_TenantId_ConsolidationGroupId_PeriodEnd",
                table: "ConsolidationRuns",
                columns: new[] { "TenantId", "ConsolidationGroupId", "PeriodEnd" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidationTranslationRates_SourceCurrencyCode",
                table: "ConsolidationTranslationRates",
                column: "SourceCurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidationTranslationRates_TargetCurrencyCode",
                table: "ConsolidationTranslationRates",
                column: "TargetCurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidationTranslationRates_TenantId_CompanyId",
                table: "ConsolidationTranslationRates",
                columns: new[] { "TenantId", "CompanyId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidationTranslationRates_TenantId_ConsolidationRunId_C~",
                table: "ConsolidationTranslationRates",
                columns: new[] { "TenantId", "ConsolidationRunId", "CompanyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CostCenters_TenantId_CompanyId_Code",
                table: "CostCenters",
                columns: new[] { "TenantId", "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerRefunds_TenantId_CompanyId_JournalEntryId",
                table: "CustomerRefunds",
                columns: new[] { "TenantId", "CompanyId", "JournalEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerRefunds_TenantId_CompanyId_RefundNumber",
                table: "CustomerRefunds",
                columns: new[] { "TenantId", "CompanyId", "RefundNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerRefunds_TenantId_CompanyId_SalesCreditNoteId",
                table: "CustomerRefunds",
                columns: new[] { "TenantId", "CompanyId", "SalesCreditNoteId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_SourceCurrencyCode_TargetCurrencyCode_Effecti~",
                table: "ExchangeRates",
                columns: new[] { "SourceCurrencyCode", "TargetCurrencyCode", "EffectiveOn" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_TargetCurrencyCode",
                table: "ExchangeRates",
                column: "TargetCurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseReportLines_TenantId_CompanyId_AccountId",
                table: "ExpenseReportLines",
                columns: new[] { "TenantId", "CompanyId", "AccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseReportLines_TenantId_CompanyId_ExpenseReportId",
                table: "ExpenseReportLines",
                columns: new[] { "TenantId", "CompanyId", "ExpenseReportId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseReports_CurrencyCode",
                table: "ExpenseReports",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseReports_TenantId_CompanyId_EmployeePayableAccountId",
                table: "ExpenseReports",
                columns: new[] { "TenantId", "CompanyId", "EmployeePayableAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseReports_TenantId_CompanyId_JournalEntryId",
                table: "ExpenseReports",
                columns: new[] { "TenantId", "CompanyId", "JournalEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseReports_TenantId_CompanyId_ReimbursementCashAccountId",
                table: "ExpenseReports",
                columns: new[] { "TenantId", "CompanyId", "ReimbursementCashAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseReports_TenantId_CompanyId_ReimbursementJournalEntry~",
                table: "ExpenseReports",
                columns: new[] { "TenantId", "CompanyId", "ReimbursementJournalEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseReports_TenantId_CompanyId_ReportNumber",
                table: "ExpenseReports",
                columns: new[] { "TenantId", "CompanyId", "ReportNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FiscalYearClosings_CreatedByUserId",
                table: "FiscalYearClosings",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalYearClosings_TenantId_CompanyId_FiscalYearId",
                table: "FiscalYearClosings",
                columns: new[] { "TenantId", "CompanyId", "FiscalYearId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FiscalYearClosings_TenantId_CompanyId_JournalEntryId",
                table: "FiscalYearClosings",
                columns: new[] { "TenantId", "CompanyId", "JournalEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_FiscalYearClosings_TenantId_CompanyId_RetainedEarningsAccou~",
                table: "FiscalYearClosings",
                columns: new[] { "TenantId", "CompanyId", "RetainedEarningsAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_FiscalYears_TenantId_CompanyId_StartsOn",
                table: "FiscalYears",
                columns: new[] { "TenantId", "CompanyId", "StartsOn" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FiscalYearStatusChanges_ChangedByUserId",
                table: "FiscalYearStatusChanges",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalYearStatusChanges_TenantId_CompanyId_FiscalYearId_Cha~",
                table: "FiscalYearStatusChanges",
                columns: new[] { "TenantId", "CompanyId", "FiscalYearId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssetDepreciations_TenantId_CompanyId_AccountingPeriod~",
                table: "FixedAssetDepreciations",
                columns: new[] { "TenantId", "CompanyId", "AccountingPeriodId" });

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssetDepreciations_TenantId_CompanyId_FixedAssetId_Acc~",
                table: "FixedAssetDepreciations",
                columns: new[] { "TenantId", "CompanyId", "FixedAssetId", "AccountingPeriodId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssetDepreciations_TenantId_CompanyId_JournalEntryId",
                table: "FixedAssetDepreciations",
                columns: new[] { "TenantId", "CompanyId", "JournalEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_TenantId_CompanyId_AccumulatedDepreciationAccou~",
                table: "FixedAssets",
                columns: new[] { "TenantId", "CompanyId", "AccumulatedDepreciationAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_TenantId_CompanyId_AcquisitionJournalEntryId",
                table: "FixedAssets",
                columns: new[] { "TenantId", "CompanyId", "AcquisitionJournalEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_TenantId_CompanyId_AssetAccountId",
                table: "FixedAssets",
                columns: new[] { "TenantId", "CompanyId", "AssetAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_TenantId_CompanyId_AssetNumber",
                table: "FixedAssets",
                columns: new[] { "TenantId", "CompanyId", "AssetNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_TenantId_CompanyId_DepreciationExpenseAccountId",
                table: "FixedAssets",
                columns: new[] { "TenantId", "CompanyId", "DepreciationExpenseAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_TenantId_CompanyId_DisposalJournalEntryId",
                table: "FixedAssets",
                columns: new[] { "TenantId", "CompanyId", "DisposalJournalEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountLines_TenantId_CompanyId_InventoryCountId",
                table: "InventoryCountLines",
                columns: new[] { "TenantId", "CompanyId", "InventoryCountId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountLines_TenantId_ItemId",
                table: "InventoryCountLines",
                columns: new[] { "TenantId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCounts_TenantId_CompanyId_CountNumber",
                table: "InventoryCounts",
                columns: new[] { "TenantId", "CompanyId", "CountNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCounts_TenantId_CompanyId_JournalEntryId",
                table: "InventoryCounts",
                columns: new[] { "TenantId", "CompanyId", "JournalEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCounts_TenantId_CompanyId_WarehouseId",
                table: "InventoryCounts",
                columns: new[] { "TenantId", "CompanyId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryValuationLayers_TenantId_CompanyId_StockMovementId",
                table: "InventoryValuationLayers",
                columns: new[] { "TenantId", "CompanyId", "StockMovementId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryValuationLayers_TenantId_CompanyId_WarehouseId",
                table: "InventoryValuationLayers",
                columns: new[] { "TenantId", "CompanyId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryValuationLayers_TenantId_ItemId",
                table: "InventoryValuationLayers",
                columns: new[] { "TenantId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemCategories_TenantId_Code",
                table: "ItemCategories",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_BaseUnitCode",
                table: "Items",
                column: "BaseUnitCode");

            migrationBuilder.CreateIndex(
                name: "IX_Items_TenantId_CategoryId",
                table: "Items",
                columns: new[] { "TenantId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_Items_TenantId_Sku",
                table: "Items",
                columns: new[] { "TenantId", "Sku" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_TenantId_CompanyId_AccountingPeriodId",
                table: "JournalEntries",
                columns: new[] { "TenantId", "CompanyId", "AccountingPeriodId" });

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_TenantId_CompanyId_JournalId",
                table: "JournalEntries",
                columns: new[] { "TenantId", "CompanyId", "JournalId" });

            migrationBuilder.CreateIndex(
                name: "IX_JournalLines_TenantId_CompanyId_AccountId",
                table: "JournalLines",
                columns: new[] { "TenantId", "CompanyId", "AccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_JournalLines_TenantId_CompanyId_CostCenterId",
                table: "JournalLines",
                columns: new[] { "TenantId", "CompanyId", "CostCenterId" });

            migrationBuilder.CreateIndex(
                name: "IX_JournalLines_TenantId_CompanyId_JournalEntryId",
                table: "JournalLines",
                columns: new[] { "TenantId", "CompanyId", "JournalEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_JournalLines_TransactionCurrencyCode",
                table: "JournalLines",
                column: "TransactionCurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_Journals_TenantId_CompanyId_Code",
                table: "Journals",
                columns: new[] { "TenantId", "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TenantId_UserId_DismissedAt_CreatedAt",
                table: "Notifications",
                columns: new[] { "TenantId", "UserId", "DismissedAt", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Parties_DefaultCurrencyCode",
                table: "Parties",
                column: "DefaultCurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_Parties_TenantId_PartyNo",
                table: "Parties",
                columns: new[] { "TenantId", "PartyNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartyAddresses_TenantId_PartyId",
                table: "PartyAddresses",
                columns: new[] { "TenantId", "PartyId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_TenantId_CompanyId_PaymentId",
                table: "PaymentAllocations",
                columns: new[] { "TenantId", "CompanyId", "PaymentId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_TenantId_CompanyId_PurchaseBillId",
                table: "PaymentAllocations",
                columns: new[] { "TenantId", "CompanyId", "PurchaseBillId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_TenantId_CompanyId_SalesInvoiceId",
                table: "PaymentAllocations",
                columns: new[] { "TenantId", "CompanyId", "SalesInvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CurrencyCode",
                table: "Payments",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantId_CompanyId_JournalEntryId",
                table: "Payments",
                columns: new[] { "TenantId", "CompanyId", "JournalEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantId_CompanyId_PaymentNumber",
                table: "Payments",
                columns: new[] { "TenantId", "CompanyId", "PaymentNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantId_PartyId",
                table: "Payments",
                columns: new[] { "TenantId", "PartyId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseBillLines_TaxCodeId",
                table: "PurchaseBillLines",
                column: "TaxCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseBillLines_TenantId_CompanyId_AccountId",
                table: "PurchaseBillLines",
                columns: new[] { "TenantId", "CompanyId", "AccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseBillLines_TenantId_CompanyId_PurchaseBillId",
                table: "PurchaseBillLines",
                columns: new[] { "TenantId", "CompanyId", "PurchaseBillId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseBills_CurrencyCode",
                table: "PurchaseBills",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseBills_TenantId_CompanyId_BillNumber",
                table: "PurchaseBills",
                columns: new[] { "TenantId", "CompanyId", "BillNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseBills_TenantId_CompanyId_JournalEntryId",
                table: "PurchaseBills",
                columns: new[] { "TenantId", "CompanyId", "JournalEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseBills_TenantId_SupplierId",
                table: "PurchaseBills",
                columns: new[] { "TenantId", "SupplierId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_TenantId_CompanyId_PurchaseOrderId",
                table: "PurchaseOrderLines",
                columns: new[] { "TenantId", "CompanyId", "PurchaseOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_TenantId_ItemId",
                table: "PurchaseOrderLines",
                columns: new[] { "TenantId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_CurrencyCode",
                table: "PurchaseOrders",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_TenantId_CompanyId_OrderNumber",
                table: "PurchaseOrders",
                columns: new[] { "TenantId", "CompanyId", "OrderNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_TenantId_CompanyId_WarehouseId",
                table: "PurchaseOrders",
                columns: new[] { "TenantId", "CompanyId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_TenantId_SupplierId",
                table: "PurchaseOrders",
                columns: new[] { "TenantId", "SupplierId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesCreditNoteLines_TaxCodeId",
                table: "SalesCreditNoteLines",
                column: "TaxCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesCreditNoteLines_TenantId_CompanyId_AccountId",
                table: "SalesCreditNoteLines",
                columns: new[] { "TenantId", "CompanyId", "AccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesCreditNoteLines_TenantId_CompanyId_SalesCreditNoteId",
                table: "SalesCreditNoteLines",
                columns: new[] { "TenantId", "CompanyId", "SalesCreditNoteId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesCreditNoteLines_TenantId_CompanyId_SalesInvoiceLineId",
                table: "SalesCreditNoteLines",
                columns: new[] { "TenantId", "CompanyId", "SalesInvoiceLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesCreditNotes_TenantId_CompanyId_CreditNoteNumber",
                table: "SalesCreditNotes",
                columns: new[] { "TenantId", "CompanyId", "CreditNoteNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesCreditNotes_TenantId_CompanyId_JournalEntryId",
                table: "SalesCreditNotes",
                columns: new[] { "TenantId", "CompanyId", "JournalEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesCreditNotes_TenantId_CompanyId_ReceivableAccountId",
                table: "SalesCreditNotes",
                columns: new[] { "TenantId", "CompanyId", "ReceivableAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesCreditNotes_TenantId_CompanyId_SalesInvoiceId",
                table: "SalesCreditNotes",
                columns: new[] { "TenantId", "CompanyId", "SalesInvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceLines_TaxCodeId",
                table: "SalesInvoiceLines",
                column: "TaxCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceLines_TenantId_CompanyId_AccountId",
                table: "SalesInvoiceLines",
                columns: new[] { "TenantId", "CompanyId", "AccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceLines_TenantId_CompanyId_SalesInvoiceId",
                table: "SalesInvoiceLines",
                columns: new[] { "TenantId", "CompanyId", "SalesInvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_CurrencyCode",
                table: "SalesInvoices",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_TenantId_CompanyId_InvoiceNumber",
                table: "SalesInvoices",
                columns: new[] { "TenantId", "CompanyId", "InvoiceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_TenantId_CompanyId_JournalEntryId",
                table: "SalesInvoices",
                columns: new[] { "TenantId", "CompanyId", "JournalEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_TenantId_CustomerId",
                table: "SalesInvoices",
                columns: new[] { "TenantId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuoteLines_TaxCodeId",
                table: "SalesQuoteLines",
                column: "TaxCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuoteLines_TenantId_CompanyId_AccountId",
                table: "SalesQuoteLines",
                columns: new[] { "TenantId", "CompanyId", "AccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuoteLines_TenantId_CompanyId_SalesQuoteId",
                table: "SalesQuoteLines",
                columns: new[] { "TenantId", "CompanyId", "SalesQuoteId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotes_CurrencyCode",
                table: "SalesQuotes",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotes_TenantId_CompanyId_QuoteNumber",
                table: "SalesQuotes",
                columns: new[] { "TenantId", "CompanyId", "QuoteNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotes_TenantId_CompanyId_SalesInvoiceId",
                table: "SalesQuotes",
                columns: new[] { "TenantId", "CompanyId", "SalesInvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotes_TenantId_CustomerId",
                table: "SalesQuotes",
                columns: new[] { "TenantId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantId_CompanyId_JournalEntryId",
                table: "StockMovements",
                columns: new[] { "TenantId", "CompanyId", "JournalEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantId_CompanyId_MovementNumber",
                table: "StockMovements",
                columns: new[] { "TenantId", "CompanyId", "MovementNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantId_CompanyId_PurchaseOrderLineId",
                table: "StockMovements",
                columns: new[] { "TenantId", "CompanyId", "PurchaseOrderLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantId_CompanyId_WarehouseId_WarehouseLoca~",
                table: "StockMovements",
                columns: new[] { "TenantId", "CompanyId", "WarehouseId", "WarehouseLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantId_ItemId",
                table: "StockMovements",
                columns: new[] { "TenantId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxCategories_Code",
                table: "TaxCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxCodes_CategoryId",
                table: "TaxCodes",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxCodes_JurisdictionId",
                table: "TaxCodes",
                column: "JurisdictionId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxCodes_TenantId_CompanyId_JurisdictionId_Code",
                table: "TaxCodes",
                columns: new[] { "TenantId", "CompanyId", "JurisdictionId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxJurisdictions_Code",
                table: "TaxJurisdictions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxRates_TaxCodeId",
                table: "TaxRates",
                column: "TaxCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Slug",
                table: "Tenants",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantTaxRegistrations_JurisdictionId",
                table: "TenantTaxRegistrations",
                column: "JurisdictionId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantTaxRegistrations_TenantId_CompanyId_JurisdictionId",
                table: "TenantTaxRegistrations",
                columns: new[] { "TenantId", "CompanyId", "JurisdictionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantUsers_UserId",
                table: "TenantUsers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocations_TenantId_CompanyId_WarehouseId_Code",
                table: "WarehouseLocations",
                columns: new[] { "TenantId", "CompanyId", "WarehouseId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_TenantId_CompanyId_Code",
                table: "Warehouses",
                columns: new[] { "TenantId", "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateTable(
                name: "PosSettings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    WarehouseId = table.Column<long>(type: "bigint", nullable: false),
                    JournalId = table.Column<long>(type: "bigint", nullable: false),
                    AccountingPeriodId = table.Column<long>(type: "bigint", nullable: false),
                    RevenueAccountId = table.Column<long>(type: "bigint", nullable: false),
                    ReceivableAccountId = table.Column<long>(type: "bigint", nullable: false),
                    SettlementAccountId = table.Column<long>(type: "bigint", nullable: false),
                    TaxCodeId = table.Column<long>(type: "bigint", nullable: true),
                    TaxAccountId = table.Column<long>(type: "bigint", nullable: true),
                    InventoryAccountId = table.Column<long>(type: "bigint", nullable: false),
                    CostOfGoodsSoldAccountId = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PosSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PosSettings_Companies_TenantId_CompanyId",
                        columns: x => new { x.TenantId, x.CompanyId },
                        principalTable: "Companies",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PosSettings_TenantId_CompanyId",
                table: "PosSettings",
                columns: new[] { "TenantId", "CompanyId" },
                unique: true);

            migrationBuilder.CreateTable(
                name: "CompanyItemPrices",
                columns: table => new
                {
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<long>(type: "bigint", nullable: false),
                    UnitSellingPrice = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: true),
                    CurrencyCode = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyItemPrices", x => new { x.TenantId, x.CompanyId, x.ItemId });
                    table.CheckConstraint("CK_CompanyItemPrices_UnitSellingPrice", "\"UnitSellingPrice\" IS NULL OR \"UnitSellingPrice\" >= 0");
                    table.ForeignKey(
                        name: "FK_CompanyItemPrices_Companies_TenantId_CompanyId",
                        columns: x => new { x.TenantId, x.CompanyId },
                        principalTable: "Companies",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanyItemPrices_Currencies_CurrencyCode",
                        column: x => x.CurrencyCode,
                        principalTable: "Currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanyItemPrices_Items_TenantId_ItemId",
                        columns: x => new { x.TenantId, x.ItemId },
                        principalTable: "Items",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyItemPrices_CurrencyCode",
                table: "CompanyItemPrices",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyItemPrices_TenantId_ItemId",
                table: "CompanyItemPrices",
                columns: new[] { "TenantId", "ItemId" });

            migrationBuilder.AddColumn<long>(
                name: "ReversalOfEntryId",
                table: "JournalEntries",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_JournalEntries_TenantId_CompanyId_ReversalOfEntryId",
                table: "JournalEntries",
                columns: new[] { "TenantId", "CompanyId", "ReversalOfEntryId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntries_JournalEntries_TenantId_CompanyId_ReversalOf~",
                table: "JournalEntries",
                columns: new[] { "TenantId", "CompanyId", "ReversalOfEntryId" },
                principalTable: "JournalEntries",
                principalColumns: new[] { "TenantId", "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "Notifications",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "legacy");

            migrationBuilder.CreateTable(
                name: "NotificationPreferences",
                columns: table => new
                {
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    InAppEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    EmailEnabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationPreferences", x => new { x.TenantId, x.UserId, x.EventType });
                    table.ForeignKey(
                        name: "FK_NotificationPreferences_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_UserId",
                table: "NotificationPreferences",
                column: "UserId");

            migrationBuilder.CreateTable(
                name: "TenantEmailConfigurations",
                columns: table => new
                {
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SmtpHost = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: false),
                    SmtpPort = table.Column<int>(type: "integer", nullable: false),
                    SmtpUsername = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ProtectedSmtpPassword = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: true),
                    FromEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    FromName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantEmailConfigurations", x => x.TenantId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationPreferences");

            migrationBuilder.DropTable(
                name: "TenantEmailConfigurations");

            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntries_JournalEntries_TenantId_CompanyId_ReversalOf~",
                table: "JournalEntries");

            migrationBuilder.DropIndex(
                name: "UX_JournalEntries_TenantId_CompanyId_ReversalOfEntryId",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "ReversalOfEntryId",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "EventType",
                table: "Notifications");

            migrationBuilder.DropTable(
                name: "CompanyItemPrices");

            migrationBuilder.DropTable(
                name: "PosSettings");

            migrationBuilder.DropTable(
                name: "AccountingPeriodStatusChanges");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "BankReconciliationMatches");

            migrationBuilder.DropTable(
                name: "BudgetLines");

            migrationBuilder.DropTable(
                name: "CompanyUsers");

            migrationBuilder.DropTable(
                name: "ConsolidationGroupCompanies");

            migrationBuilder.DropTable(
                name: "ConsolidationJournalLines");

            migrationBuilder.DropTable(
                name: "ConsolidationTranslationRates");

            migrationBuilder.DropTable(
                name: "CustomerRefunds");

            migrationBuilder.DropTable(
                name: "ExchangeRates");

            migrationBuilder.DropTable(
                name: "ExpenseReportLines");

            migrationBuilder.DropTable(
                name: "FiscalYearClosings");

            migrationBuilder.DropTable(
                name: "FiscalYearStatusChanges");

            migrationBuilder.DropTable(
                name: "FixedAssetDepreciations");

            migrationBuilder.DropTable(
                name: "InventoryCountLines");

            migrationBuilder.DropTable(
                name: "InventoryValuationLayers");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PartyAddresses");

            migrationBuilder.DropTable(
                name: "PaymentAllocations");

            migrationBuilder.DropTable(
                name: "PurchaseBillLines");

            migrationBuilder.DropTable(
                name: "SalesCreditNoteLines");

            migrationBuilder.DropTable(
                name: "SalesQuoteLines");

            migrationBuilder.DropTable(
                name: "TaxRates");

            migrationBuilder.DropTable(
                name: "TenantTaxRegistrations");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "BankStatementTransactions");

            migrationBuilder.DropTable(
                name: "JournalLines");

            migrationBuilder.DropTable(
                name: "Budgets");

            migrationBuilder.DropTable(
                name: "TenantUsers");

            migrationBuilder.DropTable(
                name: "ConsolidationRuns");

            migrationBuilder.DropTable(
                name: "ExpenseReports");

            migrationBuilder.DropTable(
                name: "FixedAssets");

            migrationBuilder.DropTable(
                name: "InventoryCounts");

            migrationBuilder.DropTable(
                name: "StockMovements");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "PurchaseBills");

            migrationBuilder.DropTable(
                name: "SalesCreditNotes");

            migrationBuilder.DropTable(
                name: "SalesInvoiceLines");

            migrationBuilder.DropTable(
                name: "SalesQuotes");

            migrationBuilder.DropTable(
                name: "BankAccounts");

            migrationBuilder.DropTable(
                name: "CostCenters");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "ConsolidationGroups");

            migrationBuilder.DropTable(
                name: "PurchaseOrderLines");

            migrationBuilder.DropTable(
                name: "WarehouseLocations");

            migrationBuilder.DropTable(
                name: "TaxCodes");

            migrationBuilder.DropTable(
                name: "SalesInvoices");

            migrationBuilder.DropTable(
                name: "ChartOfAccounts");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "PurchaseOrders");

            migrationBuilder.DropTable(
                name: "TaxCategories");

            migrationBuilder.DropTable(
                name: "TaxJurisdictions");

            migrationBuilder.DropTable(
                name: "JournalEntries");

            migrationBuilder.DropTable(
                name: "AccountGroups");

            migrationBuilder.DropTable(
                name: "ItemCategories");

            migrationBuilder.DropTable(
                name: "UnitsOfMeasure");

            migrationBuilder.DropTable(
                name: "Parties");

            migrationBuilder.DropTable(
                name: "Warehouses");

            migrationBuilder.DropTable(
                name: "AccountingPeriods");

            migrationBuilder.DropTable(
                name: "Journals");

            migrationBuilder.DropTable(
                name: "FiscalYears");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropTable(
                name: "Currencies");

            migrationBuilder.DropTable(
                name: "Tenants");
        }
    }
}
