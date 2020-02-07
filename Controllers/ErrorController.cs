using Locus.ViewModels;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Locus.Data;

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
        [Route("[action]")]
        public IActionResult Warning(int? statusCode)
        {
            ErrorWarningViewModel model = new ErrorWarningViewModel
            {
                Message = null
            };
            
            if (statusCode != null)
            {
                switch (statusCode)
                {
                    case 404:
                        model.Message = statusCode.ToString() + ": The page you were trying to view is unavailable.";
                        return View(model);
                    default:
                        model.Message = "An error has occurred. Status code: " + statusCode.ToString();
                        return View(model);
                }
            }

            var exception = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            LogEntry entry = new LogEntry
            {
                Path = exception.Path,
                Message = (exception.Error.InnerException != null ? exception.Error.InnerException.Message : exception.Error.Message)
            };

            _logger.WriteLog(entry);
            model.Message = exception.Error.Message;
            return View(model);
        }
    }
}
