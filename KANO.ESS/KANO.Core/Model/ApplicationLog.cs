using KANO.Core.Lib;
using KANO.Core.Lib.Extension;
using KANO.Core.Service;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    [Collection("ApplicationLog")]
    public class ApplicationLog<T> : IMongoPreSave<ApplicationLog<T>>
    {
        [BsonId]
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Type { get; set; }
        public string Module { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }

        public static ApplicationLog<ExceptionLog> FromException(string module, Exception exception, string message = null)
        {
            if (exception == null) throw new ArgumentNullException("exception");
            return ApplicationLog<ExceptionLog>.Create(module, "Error", message ?? exception.Message, ExceptionLog.FromException(exception));
        }

        public static ApplicationLog<T> Create(string module, string logType, string message, T data = default(T))
        {
            var res = new ApplicationLog<T>();
            res.Type = logType;
            res.Module = module;
            res.Message = message;
            res.Timestamp = DateTime.Now;
            res.Id = DateTime.Now.ToString("yyyyMMddHHmmssffffff") + ":" + Guid.NewGuid().ToString("N").Substring(0,16);
            res.Data = data;
            return res;
        }

        public void PreSave(IMongoDatabase db)
        {
            var log = GenericConfig<bool>.GetConfig(db, "AppLog", "Active", true);
            if (!log)
                Id = "";
        }
    }

    public class ApplicationLog
    {
        public static ApplicationLog<ExceptionLog> FromException(string module, Exception exception, string message = null)
        {
            return ApplicationLog<ExceptionLog>.FromException(module, exception, message);
        }
        public static ApiResult<object> ReturnError(string module, Exception exception, IMongoDatabase DB, string message = null)
        {
            return ReturnError(module, exception, DB, HttpStatusCode.InternalServerError, message);
        }
        public static ApiResult<object> ReturnError(string module, Exception exception, IMongoDatabase DB, HttpStatusCode httpStatusCode, string message = null)
        {
            var obj = FromException(module, exception, message);
            DB.Save(obj);
            var errMess = "Error encountered with log id [" + obj.Id + "]";
            if (!string.IsNullOrWhiteSpace(message))
                errMess += " with message " + message;
            return ApiResult<object>.Error(httpStatusCode, errMess);
        }
        public static ApplicationLog<T> Create<T>(string module, string logType, string message, T data = default(T))
        {
            return ApplicationLog<T>.Create(module, logType, message, data);
        }
    }

    public class ExceptionLog
    {
        public string Type { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public ExceptionLog InnerException { get; set; }

        public static ExceptionLog FromException(Exception exception)
        {
            var res = new ExceptionLog();
            res.Message = exception.Message;
            res.StackTrace = exception.StackTrace;
            res.Type = exception.GetType().Name;

            if (exception.InnerException != null)
                res.InnerException = ExceptionLog.FromException(exception.InnerException);
            return res;
        }
    }
}
