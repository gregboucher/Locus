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
        //[Route("~/")]
        [Route("")]
        [Route("[action]")]
        public ViewResult Create()
        {
            UserCreateViewModel model = new UserCreateViewModel
            {
                Controller = "User",
                Page = "Create",
                Icon = "user-plus",
                Roles = _repository.GetAllRoles(),
                Groups = _repository.GetModelsByGroup(null)
            };
            return View(model);
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
                    Groups = _repository.CreateNewUser(postModel)
                };
                return View("Summary", viewModel);
            }
            return RedirectToAction();
        }

        [HttpGet]
        [Route("[action]/{userId}")]
        public ViewResult Edit(int userId)
        {
            UserEditViewModel model = new UserEditViewModel
            {
                Controller = "User",
                Page = "Edit",
                Icon = "edit",
                UserDetails = _repository.GetUserDetails(userId),
                Roles = _repository.GetAllRoles(),
                Groups = _repository.GetModelsByGroup(userId)
            };
            return View(model);
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
                    Groups = _repository.EditExistingUser(postModel)
                };
                if (viewModel.Groups.Any())
                {
                    return View("Summary", viewModel);
                }
                return RedirectToAction("Dashboard", "Home");
            }
            return RedirectToAction();
        }
    }
}