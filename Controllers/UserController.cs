using Locus.Data;
using Locus.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Locus.Controllers
{
    [Route("[controller]")]
    public class UserController : Controller
    {
        private readonly IRepository _repository;

        public UserController(IRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        [Route("")]
        [Route("[action]")]
        public ViewResult Create()
        {
            UserCreateViewModel viewModel = new UserCreateViewModel
            {
                Controller = "User",
                Page = "Create",
                Icon = "user-plus",
                Roles = _repository.GetAllRoles(),
                GroupedModels = _repository.GetModelsByGroup(null)
            };
            return View(viewModel);
        }

        [HttpPost]
        [Route("[action]")]
        public IActionResult Create(UserCreatePostModel postModel)
        {
            if (ModelState.IsValid)
            {
                UserSummaryViewModel viewModel = new UserSummaryViewModel
                {
                    Controller = "User",
                    Page = "Summary",
                    Icon = "doc-text-inv",
                    User = _repository.CreateNewUser(postModel)
                };
                return View("Summary", viewModel);
            }
            return RedirectToAction();
        }

        [HttpGet]
        [Route("[action]/{userId}")]
        public ViewResult Edit(int userId)
        {
            UserEditViewModel viewModel = new UserEditViewModel
            {
                Controller = "User",
                Page = "Edit",
                Icon = "edit",
                UserDetails = _repository.GetUserDetails(userId),
                Roles = _repository.GetAllRoles(),
                GroupedModels = _repository.GetModelsByGroup(userId)
            };
            return View(viewModel);
        }

        [HttpPost]
        [Route("[action]/{userId}")]
        public IActionResult Edit(UserEditPostModel postModel)
        {
            if (ModelState.IsValid)
            {
                UserSummaryViewModel viewModel = new UserSummaryViewModel
                {
                    Controller = "User",
                    Page = "Summary",
                    Icon = "doc-text-inv",
                    User = _repository.EditExistingUser(postModel)
                };
                if (viewModel.User.GroupedAssignments.Any())
                {
                    return View("Summary", viewModel);
                }
                return RedirectToAction("Dashboard", "Home");
            }
            return RedirectToAction();
        }
    }
}