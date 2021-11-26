using KANO.Core.Model;
using KANO.Core.Service.AX;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace KANO.Core.Service
{
    public static class Tools
    {
        public static string ToMD5(this string s)
        {
            using (var provider = System.Security.Cryptography.MD5.Create())
            {
                StringBuilder builder = new StringBuilder();

                foreach (byte b in provider.ComputeHash(Encoding.UTF8.GetBytes(s)))
                    builder.Append(b.ToString("x2").ToLower());

                return builder.ToString();
            }
        }

        public static DateTime ToUTC(DateTime d)
        {
            return new DateTime(d.Ticks, DateTimeKind.Utc);
        }

        public static DateTime ToUTC(DateTime d, bool dateOnly)
        {
            var dt = new DateTime(d.Ticks, DateTimeKind.Utc);
            if (dateOnly) dt = dt.Date;
            return dt;
        }

        public static DateTime? ToUTC(DateTime? d, DateTime? def = null)
        {
            if (def == null) def = (DateTime?)DateTime.Now;
            DateTime d1 = (DateTime)(d == null ? DateTime.Now : d);
            return (DateTime?)new DateTime(d1.Ticks, DateTimeKind.Utc);
        }
        public static DateTime DefaultDate
        {
            get
            {
                return new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            }
        }
        public static DateTime FirstDateOfMonth(DateTime d)
        {
            var dt = new DateTime(d.Year, d.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            return dt;
        }
        public static DateTime EndDateOfMonth(DateTime d, bool onlyDatePart = true)
        {
            var dt = onlyDatePart == true ?
                FirstDateOfMonth(d).AddMonths(1).AddDays(-1) :
                FirstDateOfMonth(d).AddMonths(1).AddMilliseconds(-1);
            return dt;
        }

        public static string SanitizeFileName(string fileName, char replaceInvalidWith = '_')
        {
            string safeFileNameCharset = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 -_%.,+()[]";
            fileName = fileName ?? "";
            var res = "";
            foreach (var c in fileName)
            {
                if (safeFileNameCharset.Contains(c))
                    res += c;
                else if (replaceInvalidWith != '\0')
                    res += replaceInvalidWith;
            }
            return res;
        }

        public static string CalculateMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        public static string CalculateMD5(IFormFile file)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = file.OpenReadStream())
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        public static bool IsFormDataEmpty(string value)
        {
            if (value != null)
            {
                var v = value.Trim().ToLower();
                return string.IsNullOrWhiteSpace(v) || v == "null" || v == "undefined";
            }
            return false;
        }

        public static string EmptyFormDataFilter(string value)
        {
            return IsFormDataEmpty(value) ? "" : value;
        }

        public static object ConfigChecksum(IConfiguration configuration)
        {
            return new
            {
                AX = JsonConvert.SerializeObject(AXConfiguration(configuration)).ToMD5(),
                PID = Process.GetCurrentProcess().Id,
            };

        }

        public static Credential AXConfiguration(IConfiguration configuration)
        {
            if (configuration != null)
            {
                var list = new Dictionary<string, Credential>();
                foreach (var con in configuration.GetSection("AX:ConnectionLists").GetChildren())
                {
                    list.Add(con.Key, con.Get<Credential>());
                }
                var defaultConnection = configuration.GetValue<string>("AX:DefaultConnection");
                var ax = list[defaultConnection];
                if (ax != null)
                {
                    return ax;
                }
            }

            throw new Exception("Unable to define AX connection");
        }

        public static string UploadPathConfiguration(IConfiguration configuration)
        {
            var path = configuration["Path:UploadDirectory"] ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "assets", "appdata");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return path;
        }

        public static string GetUploadMaxFileSize(IConfiguration configuration)
        {
            var path = configuration["Path:UploadDirectory"] ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "assets", "appdata");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return path;
        }

        public static bool GetLogActivation(IConfiguration configuration)
        {
            return Convert.ToBoolean(configuration["Request:Log:Activated"]);
        }

        public static bool Equals(object data1, object data2)
        {
            if (data1 == null && data2 == null)
            {
                return true;
            }

            if (data1 == null && data2 != null || data1 != null && data2 == null)
            {
                return false;
            }

            if (data1 != null && data2 != null)
            {
                return data1.Equals(data2);
            }

            return false;

        }

        public static string GetLogTableName(IConfiguration configuration)
        {
            var tableName = configuration["Request:Log:Table"];
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return "Logs";
            }
            return tableName;
        }

        public static int RandomInt(int max = 9999999)
        {
            Random r = new Random();
            return r.Next(1, max);
        }

        public static bool IsDraft(BaseUpdateRequest x)
        {
            return (x.Status == UpdateRequestStatus.InReview);
        }

        public static string FileToBase64(string path)
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                Byte[] bytes = File.ReadAllBytes(path);
                return Convert.ToBase64String(bytes);
            }
            return "";
        }

        public static string AddPrefixToFile(string path, string prefix, string newDirectoryName = "")
        {
            if (File.Exists(path) && !string.IsNullOrWhiteSpace(path))
            {
                var filename = $"{prefix}{Path.GetFileName(path)}";
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(newDirectoryName))
                {
                    var newDirectory = Path.Combine(directory, newDirectoryName);
                    if (!Directory.Exists(newDirectory))
                    {
                        var newDirectoryInfo = Directory.CreateDirectory(newDirectory);
                        if (newDirectoryInfo.Exists)
                        {
                            directory = newDirectory;
                        }
                    }
                    else
                    {
                        directory = newDirectory;
                    }
                }

                var newPath = Path.Combine(directory, filename);
                File.Move(path, newPath);
                return newPath;
            }
            return "";
        }

        public static string DeleteFile(string path)
        {
            return AddPrefixToFile(path, "OLD_", "deleted");
        }

        public static string ArchiveFile(string path)
        {
            return AddPrefixToFile(path, "DONE_", "archived");
        }

        public static KESSWRServices.KESSWorkerRequestPurpose ActionToPurpose(ActionType action)
        {
            switch (action)
            {
                case ActionType.Create:
                    return KESSWRServices.KESSWorkerRequestPurpose.Insert;
                case ActionType.Update:
                    return KESSWRServices.KESSWorkerRequestPurpose.Update;
                case ActionType.Delete:
                    return KESSWRServices.KESSWorkerRequestPurpose.Delete;
                default:
                    return KESSWRServices.KESSWorkerRequestPurpose.None;
            }
        }

        public static NormalDateRange normalizeFilter(DateRange param)
        {
            if (param.Start > param.Finish)
            {
                var temp = param.Start;
                param.Start = param.Finish;
                param.Finish = temp;
            }

            var start = default(DateTime);
            if (param.Start != default)
                start = new DateTime(param.Start.Year, param.Start.Month, param.Start.Day, 0, 0, 0, DateTimeKind.Utc);
            

            var finish = default(DateTime);
            if (param.Finish != default)
                finish = new DateTime(param.Finish.Year, param.Finish.Month, param.Finish.Day, 23, 59, 59, DateTimeKind.Utc);            

            return new NormalDateRange
            {
                Start = start,
                Finish = finish,
                
            };
        }

        public static DateTime normalize(DateTime param)
        {
            return new DateTime(param.Year, param.Month, param.Day, 0, 0, 0);
        }

        public static string EnumToJson(Type e)
        {

            var ret = "{";

            foreach (var val in Enum.GetValues(e))
            {

                var name = Enum.GetName(e, val);
                ret += ((int)val).ToString() + ":'" + name + "',";

            }
            ret += "}";
            return ret;

        }

        public static string EnumToJson2(Type e)
        {

            var ret = "{";

            foreach (var val in Enum.GetValues(e))
            {

                var name = Enum.GetName(e, val);
                ret += ((int)val).ToString() + ":'" + name + "',";

            }
            ret += "}";
            return ret;

        }
        public static int WeekOfYearISO8601(DateTime date)
        {
            var day = (int)CultureInfo.CurrentCulture.Calendar.GetDayOfWeek(date);
            return CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(date.AddDays(4 - (day == 0 ? 7 : day)), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }
        public static DateRange NormalizeDateRangeUtc(DateRange param)
        {
            try
            {
                if (param == null)
                {
                    return new DateRange();
                }

                param.Start = new DateTime(param.Start.Year, param.Start.Month, param.Start.Day, 0, 0, 0, DateTimeKind.Utc);
                param.Finish = new DateTime(param.Finish.Year, param.Finish.Month, param.Finish.Day, 0, 0, 0, DateTimeKind.Utc);

            }
            catch (ArgumentNullException)
            {
                return new DateRange();
            }

            return param;
        }
    }


    public class NormalDateRange
    {
        public DateTime Start { set; get; }
        public DateTime Finish { set; get; }
    }
}
