using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Common
{
    public class ServiceResult
    {
        public bool Success { get; }
        public string Message { get; }
        public object? Data { get; }

        public ServiceResult(bool success, string message, object? data = null)
        {
            Success = success;
            Message = message;
            Data = data;
        }

        public static ServiceResult SuccessResult(string message, object? data = null)
            => new ServiceResult(true, message, data);

        public static ServiceResult Failure(string message, object? data = null)
            => new ServiceResult(false, message, data);
    }
}
