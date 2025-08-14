using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenVPNGateMonitor.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class OpenVpnServerEventLog_AllParams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RawJson",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerEventLogs");

            migrationBuilder.AddColumn<string>(
                name: "Action",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerEventLogs",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "BytesReceived",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerEventLogs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "BytesSent",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerEventLogs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DisconnectedAt",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerEventLogs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DurationSec",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerEventLogs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EventTimeUtc",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerEventLogs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<string>(
                name: "IvGuiVer",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerEventLogs",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IvPlat",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerEventLogs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IvVer",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerEventLogs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SampleBytesIn",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerEventLogs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SampleBytesOut",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerEventLogs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScriptType",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerEventLogs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_ovpn_events_server_cn_time",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerEventLogs",
                columns: new[] { "VpnServerId", "CommonName", "EventTimeUtc" });

            migrationBuilder.CreateIndex(
                name: "ix_ovpn_events_server_time",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerEventLogs",
                columns: new[] { "VpnServerId", "EventTimeUtc" });

            migrationBuilder.CreateIndex(
                name: "ix_ovpn_events_server_type_time",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerEventLogs",
                columns: new[] { "VpnServerId", "EventType", "EventTimeUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_ovpn_events_server_cn_time",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerEventLogs");

            migrationBuilder.DropIndex(
                name: "ix_ovpn_events_server_time",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerEventLogs");

            migrationBuilder.DropIndex(
                name: "ix_ovpn_events_server_type_time",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerEventLogs");

            migrationBuilder.DropColumn(
                name: "Action",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerEventLogs");

            migrationBuilder.DropColumn(
                name: "BytesReceived",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerEventLogs");

            migrationBuilder.DropColumn(
                name: "BytesSent",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerEventLogs");

            migrationBuilder.DropColumn(
                name: "DisconnectedAt",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerEventLogs");

            migrationBuilder.DropColumn(
                name: "DurationSec",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerEventLogs");

            migrationBuilder.DropColumn(
                name: "EventTimeUtc",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerEventLogs");

            migrationBuilder.DropColumn(
                name: "IvGuiVer",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerEventLogs");

            migrationBuilder.DropColumn(
                name: "IvPlat",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerEventLogs");

            migrationBuilder.DropColumn(
                name: "IvVer",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerEventLogs");

            migrationBuilder.DropColumn(
                name: "SampleBytesIn",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerEventLogs");

            migrationBuilder.DropColumn(
                name: "SampleBytesOut",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerEventLogs");

            migrationBuilder.DropColumn(
                name: "ScriptType",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerEventLogs");

            migrationBuilder.AddColumn<string>(
                name: "RawJson",
                schema: "xgb_dashopnvpn",
                table: "OpenVpnServerEventLogs",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
