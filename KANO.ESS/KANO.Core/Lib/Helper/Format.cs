using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace KANO.Core.Lib.Helper
{
    public static class Format
    {
        public static string FormatNumber(double numb)
        {
            return FormatNumber(numb, "{0:#,###}");
        }

        public static string FormatNumber(double numb, string format)
        {
            if (string.IsNullOrEmpty(format))
            {
                return null;
            }

            return string.Format(format, numb);
        }

        public static string StandarizeDate(DateTime dt)
        {
            return DateToString(dt, "dd MMMM yyyy");
        }

         public static string StandarizeMonth(DateTime dt)
        {
            return DateToString(dt, "MMMM");
        }


        public static string StandarizeTime(DateTime dt)
        {
            return DateToString(dt, "HH:mm");
        }

        public static string StandarizeDateTime(DateTime dt)
        {
            return DateToString(dt, "dd MMMM yyyy HH:mm");
        }

        public static string DateToString(DateTime dt, string format)
        {
            if (string.IsNullOrEmpty(format))
            {
                return null;
            }

            return dt.ToString(format);
        }

        public static string FormatFileSize(long bytes, byte percision = 1) 
        {
            var units = new string[] { "bytes", "kB", "MB", "GB", "TB", "PB"  };
            var number = Math.Floor(Math.Log(bytes) / Math.Log(1024));
            return Math.Round((bytes / Math.Pow(1024, Math.Floor(number))), percision) + ' ' + units[Convert.ToInt32(number)];
        }

        public static string TitleCase(this string input)
        {
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input.ToLower());
        }

        public static string ExceptionString(Exception e, bool verbose=false)
        {
            if(verbose)
                return $"{e.Message}\n{e.InnerException}";
            else
                return $"{e.Message}";
        }

    }

    public class Helper
    {
        public static string toExcelCol(ref int col, int row)
        {
            int dividend = col;
            string columnName = String.Empty;
            int modulo;

            while (dividend > 0)
            {
                modulo = (dividend - 1) % 26;
                columnName = toChar(modulo) + columnName;
                dividend = (int)((dividend - modulo) / 26);
            }
            col++;

            return string.Format("{0}{1}", columnName, row);
        }

        public static string toChar(int col)
        {
            return Convert.ToChar(col + 65).ToString();
        }

        public static DateTime unixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        public static DateTime secondsToDateTime( DateTime transactionDate, double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(transactionDate.Year, transactionDate.Month, transactionDate.Day, 0, 0, 0, 0, System.DateTimeKind.Local);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        public static int ConvertToUnixTimestamp(DateTime date)
        {
            // That's basically it. These are the methods I use to convert to and from Unix epoch time:
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan diff = date.ToUniversalTime() - origin;
            return Convert.ToInt32(Math.Floor(diff.TotalSeconds)) ;
        }

        public static int AXClock(DateTime date)
        {
            return (date.Hour * 3600) + (date.Minute * 60) + date.Second;
        }

    }

    public class Configuration {
        public static long UploadMaxFileSize(IConfiguration configuration)
        {
            var maxFilesize = configuration["Request:UploadMaxFileSize"];            
            return Convert.ToInt64(maxFilesize);
        }

        public static string[] UploadAllowedExtensions(IConfiguration configuration)
        {
            var section = configuration.GetSection("Request:UploadAllowedExtension");
            var allowedExtension = section.Get<string[]>();
            return allowedExtension;
        }

        public static string UploadPath(IConfiguration configuration, string defaultDirectory = "") 
        {
            var path = configuration["Path:UploadDirectory"] ?? ((defaultDirectory == "")?Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "assets", "appdata"):defaultDirectory);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return path;
        }
        public static string GetEmployeeImagePath(IConfiguration configuration, string defaultDirectory = "")
        {
            var path = configuration["Path:MobileEmployeeImage"] ?? ((defaultDirectory == "") ? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "assets", "appdata") : defaultDirectory);
            return path;
        }
        public static string GetAbsenceFilePath(IConfiguration configuration, string defaultDirectory = "")
        {
            var path = configuration["Path:AbsenceFilePath"] ?? ((defaultDirectory == "") ? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "assets", "appdata") : defaultDirectory);
            return path;
        }
        public static string GetIPAddressServer(IConfiguration configuration, string defaultDirectory = "")
        {
            var path = configuration["Path:IpServer"] ?? ((defaultDirectory == "") ? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "assets", "appdata") : defaultDirectory);
            return path;
        }
    }
}
