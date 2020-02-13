using Locus.Data;
using Locus.ViewModels;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Locus.Controllers
{
    [Route("[controller]")]
    public class ErrorController : Controller
    {
        private readonly ILogger _logger;

        public ErrorController(ILogger logger)
        {
            _logger = logger;
        }

        [Route("[action]/{statusCode}")]
        public IActionResult Warning(int statusCode)
        {
            ErrorWarningViewModel model = new ErrorWarningViewModel
            {
                StatusCode = statusCode,
                Icon = "attention"
            };
            switch (statusCode)
            {
                case 404:
                    model.Message = "The page you were trying to view is unavailable. Please confirm the URL and try again.";
                    return View(model);
                default:
                    model.Message = "An error has occurred.";
                    return View(model);
            }
        }

        [Route("[action]")]
        public IActionResult Exception()
        {
            var exception = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            ErrorWarningViewModel model = new ErrorWarningViewModel
            {
                Message = exception.Error.Message,
                Icon = "attention"
            };
            if (!(exception.Error is LocusException))
            {
                _logger.WriteLog(exception.Error);
                model.Message = "";
            }
            return View(model);
        }
    }
}
