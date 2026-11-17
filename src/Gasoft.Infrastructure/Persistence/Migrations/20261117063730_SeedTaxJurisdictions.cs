using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Gasoft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedTaxJurisdictions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 4L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 8L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 10L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 12L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 16L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 20L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 24L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 28L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 31L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 32L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 36L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 40L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 44L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 48L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 50L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 51L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 52L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 56L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 60L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 64L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 68L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 70L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 72L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 74L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 76L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 84L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 86L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 90L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 92L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 96L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 100L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 104L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 108L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 112L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 116L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 120L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 124L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 132L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 136L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 140L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 144L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 148L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 152L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 156L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 158L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 162L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 166L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 170L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 174L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 175L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 178L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 180L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 184L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 188L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 191L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 192L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 196L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 203L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 204L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 208L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 212L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 214L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 218L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 222L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 226L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 231L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 232L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 233L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 234L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 238L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 239L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 242L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 246L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 248L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 250L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 254L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 258L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 260L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 262L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 266L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 268L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 270L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 275L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 276L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 288L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 292L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 296L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 300L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 304L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 308L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 312L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 316L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 320L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 324L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 328L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 332L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 334L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 336L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 340L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 344L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 348L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 352L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 356L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 360L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 364L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 368L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 372L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 376L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 380L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 384L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 388L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 392L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 398L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 400L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 404L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 408L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 410L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 414L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 417L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 418L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 422L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 426L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 428L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 430L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 434L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 438L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 440L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 442L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 446L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 450L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 454L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 458L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 462L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 466L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 470L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 474L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 478L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 480L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 484L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 492L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 496L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 498L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 499L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 500L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 504L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 508L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 512L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 516L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 520L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 524L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 528L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 531L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 533L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 534L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 535L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 540L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 548L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 554L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 558L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 562L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 566L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 570L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 574L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 578L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 580L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 581L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 583L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 584L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 585L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 586L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 591L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 598L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 600L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 604L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 608L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 612L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 616L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 620L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 624L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 626L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 630L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 634L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 638L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 642L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 643L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 646L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 652L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 654L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 659L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 660L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 662L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 663L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 666L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 670L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 674L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 678L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 682L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 686L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 688L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 690L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 694L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 702L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 703L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 704L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 705L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 706L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 710L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 716L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 724L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 728L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 729L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 732L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 740L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 744L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 748L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 752L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 756L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 760L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 762L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 764L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 768L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 772L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 776L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 780L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 784L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 788L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 792L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 795L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 796L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 798L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 800L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 804L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 807L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 818L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 826L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 831L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 832L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 833L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 834L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 840L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 850L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 854L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 858L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 860L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 862L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 876L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 882L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 887L);

            migrationBuilder.DeleteData(
                table: "TaxJurisdictions",
                keyColumn: "Id",
                keyValue: 894L);
        }
    }
}
