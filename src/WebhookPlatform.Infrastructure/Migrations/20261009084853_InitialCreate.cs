using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebhookPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "webhook_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_webhook_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "webhook_subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    TargetUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SecretKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SubscribedEventTypes = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RateLimitPerMinute = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_webhook_subscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "webhook_delivery_attempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WebhookEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    WebhookSubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    HttpStatusCode = table.Column<int>(type: "integer", nullable: true),
                    ResponseTimeMs = table.Column<long>(type: "bigint", nullable: false),
                    ResponseBody = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AttemptedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NextRetryAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_webhook_delivery_attempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_webhook_delivery_attempts_webhook_events_WebhookEventId",
                        column: x => x.WebhookEventId,
                        principalTable: "webhook_events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_webhook_delivery_attempts_webhook_subscriptions_WebhookSubs~",
                        column: x => x.WebhookSubscriptionId,
                        principalTable: "webhook_subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_webhook_delivery_attempts_AttemptedAtUtc",
                table: "webhook_delivery_attempts",
                column: "AttemptedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_webhook_delivery_attempts_NextRetryAtUtc",
                table: "webhook_delivery_attempts",
                column: "NextRetryAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_webhook_delivery_attempts_Status",
                table: "webhook_delivery_attempts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_webhook_delivery_attempts_WebhookEventId",
                table: "webhook_delivery_attempts",
                column: "WebhookEventId");

            migrationBuilder.CreateIndex(
                name: "IX_webhook_delivery_attempts_WebhookSubscriptionId",
                table: "webhook_delivery_attempts",
                column: "WebhookSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_webhook_events_CreatedAtUtc",
                table: "webhook_events",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_webhook_events_EventType",
                table: "webhook_events",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_webhook_events_IdempotencyKey",
                table: "webhook_events",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_webhook_subscriptions_IsActive",
                table: "webhook_subscriptions",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "webhook_delivery_attempts");

            migrationBuilder.DropTable(
                name: "webhook_events");

            migrationBuilder.DropTable(
                name: "webhook_subscriptions");
        }
    }
}
