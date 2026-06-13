namespace TravelPlanner.Common.DTOs.Shared
{
    public class ResultDto<T>
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }

        public static ResultDto<T> Success(T data, string message = "Success")
        {
            return new ResultDto<T>
            {
                IsSuccess = true,
                Message = message,
                Data = data
            };
        }

        public static ResultDto<T> Failure(string message)
        {
            return new ResultDto<T>
            {
                IsSuccess = false,
                Message = message,
                Data = default
            };
        }
    }
}