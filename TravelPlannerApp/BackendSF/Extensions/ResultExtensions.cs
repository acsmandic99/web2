using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Common.DTOs.Shared;

namespace BackendSF.Extensions
{
    public static class ResultExtensions
    {
        public static IActionResult ToActionResult<T>(this ResultDto<T> result)
        {
            if (result.IsSuccess)
            {
                return new OkObjectResult(result);
            }
            return new BadRequestObjectResult(result);
        }
    }
}