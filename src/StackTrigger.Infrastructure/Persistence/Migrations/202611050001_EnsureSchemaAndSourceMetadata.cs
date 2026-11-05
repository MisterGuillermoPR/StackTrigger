using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using StackTrigger.Infrastructure.Persistence;

namespace StackTrigger.Infrastructure.Persistence.Migrations;

[DbContext(typeof(StackTriggerDbContext))]
[Migration("202611050001_EnsureSchemaAndSourceMetadata")]
public sealed class EnsureSchemaAndSourceMetadata : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // IF NOT EXISTS allows this initial migration to adopt databases whose tables
        // were created before migrations were added to the repository.
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS "intent_signals" (
                "Id" uuid NOT NULL PRIMARY KEY,
                "CompanyName" character varying(250) NOT NULL,
                "JobTitle" character varying(300) NOT NULL,
                "TargetTechnologies" text[] NOT NULL,
                "SourceUrl" character varying(2048) NOT NULL,
                "DateDetected" timestamp with time zone NOT NULL,
                "SourceDate" timestamp with time zone NULL,
                "RelevanceScore" integer NOT NULL DEFAULT 0,
                "DispatchStatus" character varying(30) NOT NULL
            );

            ALTER TABLE "intent_signals"
                ADD COLUMN IF NOT EXISTS "SourceDate" timestamp with time zone NULL;
            ALTER TABLE "intent_signals"
                ADD COLUMN IF NOT EXISTS "RelevanceScore" integer NOT NULL DEFAULT 0;

            CREATE UNIQUE INDEX IF NOT EXISTS "ix_intent_signals_source_url_unique"
                ON "intent_signals" ("SourceUrl");
            CREATE INDEX IF NOT EXISTS "ix_intent_signals_dispatch_status"
                ON "intent_signals" ("DispatchStatus");

            CREATE TABLE IF NOT EXISTS "client_subscriptions" (
                "Id" uuid NOT NULL PRIMARY KEY,
                "CompanyName" character varying(250) NOT NULL,
                "WebhookUrl" character varying(2048) NOT NULL,
                "TargetKeywords" text[] NOT NULL,
                "IsActive" boolean NOT NULL
            );

            CREATE INDEX IF NOT EXISTS "ix_client_subscriptions_is_active"
                ON "client_subscriptions" ("IsActive");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Keep baseline tables and data intact; only remove columns introduced here.
        migrationBuilder.Sql("""
            ALTER TABLE IF EXISTS "intent_signals" DROP COLUMN IF EXISTS "SourceDate";
            ALTER TABLE IF EXISTS "intent_signals" DROP COLUMN IF EXISTS "RelevanceScore";
            """);
    }
}
