using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using prjGoHike.Models;

namespace prjGoHike.APIControllers

{
    [ApiController]
    [Route("api/v1/[controller]")]
    public abstract class BaseController : ControllerBase
    {
        /// 成功回應 (HTTP 200 OK)
        protected IActionResult SuccessResponse<T>(T data, string message = "操作成功")
        {
            var response = new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
            return Ok(response);
        }

        /// 建立成功回應 (HTTP 201 Created)
        protected IActionResult CreatedResponse<T>(T data, string message = "新增成功")
        {
            var response = new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
            return StatusCode(StatusCodes.Status201Created, response);
        }


        /// 失敗/錯誤回應 (預設 HTTP 400 Bad Request)
        protected IActionResult ErrorResponse(string message, List<string>? errors = null, int statusCode = StatusCodes.Status400BadRequest)
        {
            var response = new ApiResponse<object>
            {
                Success = false,
                Message = message,
                Errors = errors
            };
            return StatusCode(statusCode, response);
        }

        /// 找不到資源回應 (HTTP 404 Not Found)
        protected IActionResult NotFoundResponse(string message = "找不到指定的資源")
        {
            return ErrorResponse(message, statusCode: StatusCodes.Status404NotFound);
        }
    }
}
