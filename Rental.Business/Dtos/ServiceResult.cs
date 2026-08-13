namespace Rental.Business.Dtos
{
    public class ServiceResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public object? Data { get; set; }

        public static ServiceResult Success(object? data = null) => new ServiceResult { IsSuccess = true, Data = data };
        public static ServiceResult Failure(string errorMessage) => new ServiceResult { IsSuccess = false, ErrorMessage = errorMessage };
    }
}
