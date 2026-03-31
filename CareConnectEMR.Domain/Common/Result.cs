using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareConnectEMR.Domain.Common
{
    public class Result<T>
    {
        public bool IsSuccess { get; set; }
        public T? Data { get; set; }
        public string? ErrorMessage { get; set; }
        public int StatusCode { get; set; }

        public static Result<T> Ok(T data) => new() { IsSuccess = true, Data = data ,StatusCode=200};
        public static Result<T> Created(T data) => new() { IsSuccess = true, Data = data, StatusCode = 201 };
        public static Result<T> Fail(string errorMessage, int statusCode=400) => new() { IsSuccess = false, ErrorMessage = errorMessage, StatusCode = statusCode };
        public static Result<T> Unauthorized(string errorMessage="Invalid credentials") => new() { IsSuccess = false, ErrorMessage = errorMessage, StatusCode = 401 };
        public static Result<T> NotFound(string errorMessage="Not found") => new() { IsSuccess = false, ErrorMessage = errorMessage, StatusCode = 404 };
        public static Result<T> Forbidden(string errorMessage="Forbidden") => new() { IsSuccess = false, ErrorMessage = errorMessage, StatusCode = 403 };
    }
}
