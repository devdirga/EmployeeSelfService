using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace KANO.Core.Lib
{
    public class ApiResult<T>: IActionResult
    {
        public struct Result
        {
            public HttpStatusCode StatusCode { get; set; }
            public string Message { get; set; }
            public T Data { get; set; }
            public long Total { get; set; }
        }

        private Result Res;

        public ApiResult(HttpStatusCode code, string message, T data)
        {
            Res.StatusCode = code;
            Res.Message = message;
            Res.Data = data;
            Res.Total = 0;
        }

        public ApiResult(HttpStatusCode code, string message, T data, long Total)
        {
            Res.StatusCode = code;
            Res.Message = message;
            Res.Data = data;
            Res.Total = Total;
        }

        public ApiResult(Result result)
        {
            Res.StatusCode = result.StatusCode;
            Res.Message = result.Message;
            Res.Data = result.Data;
            Res.Total = result.Total;
        }

        public static ApiResult<object> Ok(string message = null)
        {
            return new ApiResult<object>(HttpStatusCode.OK, message, null);
        }

        public static ApiResult<T> Ok(T data, string message = null)
        {
            return new ApiResult<T>(HttpStatusCode.OK, message, data);
        }

        public static ApiResult<T> Ok(T data, long Total, string message = null)
        {
            return new ApiResult<T>(HttpStatusCode.OK, message, data, Total);
        }

        public static ApiResult<object> Error(HttpStatusCode code, string message = null)
        {
            return new ApiResult<object>(code, message, null);
        }

        public static ApiResult<Exception> Error(Exception error)
        {
            return new ApiResult<Exception>(HttpStatusCode.InternalServerError, error.Message, error);
        }

        public Task ExecuteResultAsync(ActionContext context)
        {
            var js = new JsonResult(Res);
            js.StatusCode = (int) Res.StatusCode;
            return js.ExecuteResultAsync(context);
        }
    }

    public static class ApiResult
    {
        public static ApiResult<T> Ok<T>(T data, string message = null)
        {
            return ApiResult<T>.Ok(data, message);
        }
        public static ApiResult<T> Ok<T>(T data, long total, string message = null)
        {
            return ApiResult<T>.Ok(data, total, message);
        }
        public static ApiResult<object> Ok(string message = null)
        {
            return ApiResult<object>.Ok(message);
        }
        public static ApiResult<object> Error(HttpStatusCode code, string message = null)
        {
            return ApiResult<object>.Error(code, message);
        }
        public static ApiResult<Exception> Error(Exception ex)
        {
            return ApiResult<Exception>.Error(ex);
        }
    }
}
