using Locus.Data;
using Locus.Models;
using Locus.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Locus.Controllers
{
    [Route("[controller]")]
    public class UserController : Controller
    {
        private readonly IRepository _repository;
        private IComplexTransferObject<Report> _actionTransferObject;

        public UserController(IRepository repository, IComplexTransferObject<Report> actionTransferObject)
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
                if (postModel.AssignmentOperations != null || postModel.EditOperations != null)
                {
                    _actionTransferObject.Model = _repository.EditExistingUser(postModel);
                    return RedirectToAction("Report");
                }
                return RedirectToAction("Dashboard", "Home");
            }
            return RedirectToAction();
        }

        [HttpGet]
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