using Linkedin.Core.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Linkedin.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260831060000_ExpandIndustryOptions")]
    public partial class ExpandIndustryOptions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DECLARE @Industries TABLE ([Name] nvarchar(150), [NormalizedName] nvarchar(150));
                INSERT INTO @Industries ([Name], [NormalizedName]) VALUES
                    (N'Professional Training and Coaching', N'PROFESSIONAL TRAINING AND COACHING'),
                    (N'Higher Education', N'HIGHER EDUCATION'),
                    (N'E-Learning Providers', N'E-LEARNING PROVIDERS'),
                    (N'IT Services and IT Consulting', N'IT SERVICES AND IT CONSULTING'),
                    (N'Technology, Information and Internet', N'TECHNOLOGY, INFORMATION AND INTERNET'),
                    (N'Banking', N'BANKING'),
                    (N'Telecommunications', N'TELECOMMUNICATIONS'),
                    (N'Retail', N'RETAIL'),
                    (N'Human Resources Services', N'HUMAN RESOURCES SERVICES'),
                    (N'Business Consulting and Services', N'BUSINESS CONSULTING AND SERVICES');

                INSERT INTO [ProfileOptions]
                    ([Type], [Name], [NormalizedName], [IsApproved], [CreatedByUserId], [CreatedAt])
                SELECT 3, source.[Name], source.[NormalizedName], CAST(1 AS bit), NULL, SYSUTCDATETIME()
                FROM @Industries source
                WHERE NOT EXISTS
                (
                    SELECT 1
                    FROM [ProfileOptions] existing
                    WHERE existing.[Type] = 3
                      AND existing.[NormalizedName] = source.[NormalizedName]
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM [ProfileOptions]
                WHERE [Type] = 3
                  AND [CreatedByUserId] IS NULL
                  AND [NormalizedName] IN
                  (
                    N'PROFESSIONAL TRAINING AND COACHING',
                    N'HIGHER EDUCATION',
                    N'E-LEARNING PROVIDERS',
                    N'IT SERVICES AND IT CONSULTING',
                    N'TECHNOLOGY, INFORMATION AND INTERNET',
                    N'BANKING',
                    N'TELECOMMUNICATIONS',
                    N'RETAIL',
                    N'HUMAN RESOURCES SERVICES',
                    N'BUSINESS CONSULTING AND SERVICES'
                  );
                """);
        }
    }
}
