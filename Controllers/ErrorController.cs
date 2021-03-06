﻿using Locus.Data;
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
            var viewModel = new ErrorWarningViewModel
            {
                Controller = "Error",
                Page = "Warning",
                Icon = "attention",
                StatusCode = statusCode
            };
            switch (statusCode)
            {
                case 404:
                    viewModel.Message = "The page you were trying to view is unavailable.";
                    return View(viewModel);
                default:
                    viewModel.Message = "An error has occurred.";
                    return View(viewModel);
            }
        }
        
        [Route("[action]")]
        public IActionResult Exception()
        {
            var exception = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            if (exception != null)
            {
                var viewModel = new ErrorWarningViewModel
                {
                    Controller = "Error",
                    Page = "Exception",
                    Icon = "attention",
                    Message = exception.Error.Message
                };
                if (!(exception.Error is LocusException))
                {
                    _logger.WriteLog(exception.Error);
                    viewModel.Message = "";
                }
                return View(viewModel);
            }
            return RedirectToAction("Warning", "Error", new { StatusCode = 404 });
        }
    }
}
