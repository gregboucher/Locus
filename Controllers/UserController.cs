using Locus.Data;
using Locus.Models;
using Locus.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Locus.Controllers
{
    [Route("[controller]")]
    public class UserController : Controller
    {
        private readonly IRepository _repository;
        private IActionTransferObject<Report> _actionTransferObject;
        //private static Report _report;

        public UserController(IRepository repository, IActionTransferObject<Report> actionTransferObject)
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
                _actionTransferObject.Model = _repository.CreateNewUser(postModel);
                //_report = _repository.CreateNewUser(postModel);
                return RedirectToAction("Report");
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
                var viewModel = new UserReportViewModel
                {
                    Controller = "User",
                    Page = "Report",
                    Icon = "doc-text-inv",
                    Report = _repository.EditExistingUser(postModel)
                };
                if (viewModel.Report.CollectionsOfReportItems.Any())
                {
                    _actionTransferObject.Model = _repository.EditExistingUser(postModel);
                    //_report = _repository.EditExistingUser(postModel);
                    return RedirectToAction("Report");
                }
                return RedirectToAction("Dashboard", "Home");
            }
            return RedirectToAction();
        }

        [Route("[action]")]
        public IActionResult Report()
        {
            if (_actionTransferObject.Model != null)
            {
                var viewModel = new UserReportViewModel
                {
                    Controller = "User",
                    Page = "Report",
                    Icon = "doc-text-inv",
                    Report = _actionTransferObject.Model
                };
                return View(viewModel);
            }
            return RedirectToAction("Warning", "Error", new { StatusCode = 404 });
        }
    }
}