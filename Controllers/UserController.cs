using Locus.Data;
using Locus.Models;
using Locus.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace Locus.Controllers
{
    [Route("[controller]")]
    public class UserController : Controller
    {
        private readonly IRepository _repository;
        private IComplexTransferObject<IList<IResult>> _actionTransferObject;

        public UserController(IRepository repository, IComplexTransferObject<IList<IResult>> actionTransferObject)
        {
            _repository = repository;
            _actionTransferObject = actionTransferObject;
        }

        [HttpGet]
        [Route("")]
        [Route("[action]")]
        public ViewResult Create()
        {
            var viewModel = new UserCreateViewModel
            {
                Controller = "User",
                Page = "Create",
                Icon = "user-plus",
                Roles = _repository.GetAllRoles(),
                CollectionsOfModels = _repository.GetModelsByCollection(null)
            };
            return View(viewModel);
        }

        [HttpPost]
        [Route("[action]")]
        public IActionResult Create(UserCreatePostModel postModel)
        {
            if (ModelState.IsValid)
            {
                var tuple = _repository.CreateNewUser(postModel);
                _actionTransferObject.Results = tuple.Item1;
                return RedirectToAction("Report", new { userId = tuple.Item2});
            }
            return RedirectToAction();
        }

        [HttpGet]
        [Route("[action]/{userId}")]
        public IActionResult Edit(int userId)
        {
            //ensure the user is active, else 404
            if (_repository.UserStatus(userId, null) == Status.Active)
            {
                var viewModel = new UserEditViewModel
                {
                    Controller = "User",
                    Page = "Edit",
                    Icon = "edit",
                    UserDetails = _repository.GetUserDetails(userId),
                    Roles = _repository.GetAllRoles(),
                    CollectionsOfModels = _repository.GetModelsByCollection(userId)
                };
                return View(viewModel);
            }
            return RedirectToAction("Warning", "Error", new { StatusCode = 404 });
        }

        [HttpPost]
        [Route("[action]/{userId}")]
        public IActionResult Edit(UserEditPostModel postModel)
        {
            if (ModelState.IsValid)
            {
                if (postModel.AssignmentOperations != null || postModel.EditOperations != null)
                {
                    _actionTransferObject.Results = _repository.EditExistingUser(postModel);
                    return RedirectToAction("Report", new { userId = postModel.UserId});
                }
                return RedirectToAction("Dashboard", "Home");
            }
            return RedirectToAction();
        }

        [HttpGet]
        [Route("[action]/{userId}")]
        public IActionResult Report(int userId)
        {
            if (_actionTransferObject.Results != null)
            {
                var viewModel = new UserReportViewModel
                {
                    Controller = "User",
                    Page = "Report",
                    Icon = "doc-text-inv",
                    Report = _repository.GenerateReport(_actionTransferObject.Results, userId)
                };
                return View(viewModel);
            }
            return RedirectToAction("Warning", "Error", new { StatusCode = 404 });
        }
    }
}