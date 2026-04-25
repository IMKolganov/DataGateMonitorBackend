using DataGateMonitor.Models;

namespace DataGateMonitor.DataBase.ConfigurationModels.Seeds;

public static class SettingSeedData
{ 
    public static readonly Setting[] Data =
    {
        new Setting
        {
            Id = 1,
            Key = "OpenVPN_Polling_Interval",
            ValueType = "int",
            IntValue = 120
        },
        new Setting
        {
            Id = 2, 
            Key = "OpenVPN_Polling_Interval_Type",
            ValueType = "string",
            StringValue = "int"
        },
        new Setting
        {
            Id = 3, 
            Key = "OpenVPN_Polling_Interval_Unit",
            ValueType = "string",
            StringValue = "seconds"
        },
        new Setting
        {
            Id = 4, 
            Key = "OpenVPN_Polling_Interval_Unit_Type",
            ValueType = "string",
            StringValue = "string"
        },
        
        new Setting
        {
            Id = 5, 
            Key = "GeoIp_Download_Url",
            ValueType = "string",
            StringValue = "https://download.maxmind.com/app/geoip_download?edition_id=GeoLite2-City&license_key={LicenseKey}&suffix=tar.gz"
        },
        new Setting
        {
            Id = 6, 
            Key = "GeoIp_Download_Url_Type",
            ValueType = "string",
            StringValue = "string"
        },
        
        new Setting
        {
            Id = 7, 
            Key = "GeoIp_Db_Path",
            ValueType = "string",
            StringValue = "resources/geo-lite2/geo-lite2-city.mmdb"
        },
        new Setting
        {
            Id = 8, 
            Key = "GeoIp_Db_Path_Type",
            ValueType = "string",
            StringValue = "string"
        },
        
        new Setting
        {
            Id = 9, 
            Key = "GeoIp_Account_ID",
            ValueType = "string",
            StringValue = "YOUR_ACCOUNT_ID"
        },
        new Setting
        {
            Id = 10, 
            Key = "GeoIp_Account_ID_Type",
            ValueType = "string",
            StringValue = "string"
        },
        
        new Setting
        {
            Id = 11, 
            Key = "GeoIp_License_Key",
            ValueType = "string",
            StringValue = "YOUR_LICENSE_KEY"
        },
        new Setting
        {
            Id = 12, 
            Key = "GeoIp_License_Key_Type",
            ValueType = "string",
            StringValue = "string"
        },
        
        new Setting
        {
            Id = 13,
            Key = "GeoIp_Auto_Update_Interval_Days",
            ValueType = "int",
            IntValue = 0
        },
        new Setting
        {
            Id = 14,
            Key = "GeoIp_Auto_Update_Interval_Days_Type",
            ValueType = "string",
            StringValue = "int"
        },
        new Setting
        {
            Id = 15,
            Key = "Auth_Require_Email_Confirmation_On_Register",
            ValueType = "bool",
            BoolValue = true
        },
        new Setting
        {
            Id = 16,
            Key = "Auth_Require_Email_Confirmation_On_Register_Type",
            ValueType = "string",
            StringValue = "bool"
        },
        new Setting
        {
            Id = 17,
            Key = "Auth_Email_Confirmation_Code_Ttl_Minutes",
            ValueType = "int",
            IntValue = 30
        },
        new Setting
        {
            Id = 18,
            Key = "Auth_Email_Confirmation_Code_Ttl_Minutes_Type",
            ValueType = "string",
            StringValue = "int"
        },
    };
}