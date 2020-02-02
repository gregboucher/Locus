using Locus.Models;
using Locus.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

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
                Roles = _repository.GetAllRoles(),
                Groups = _repository.GetModelsByGroup(null)
            };
            return View(model);
        }


        [HttpPost]
        [Route("[action]")]
        public IActionResult Create(UserCreatePostModel model)
        {
            if (ModelState.IsValid)
            {
                _repository.TestTransaction(model);
                return RedirectToAction("Create"); //temp
            }
            return RedirectToAction("Create");
        }

        [HttpGet]
        [Route("[action]/{userId}")]
        public ViewResult Edit(int userId)
        {
            UserEditViewModel model = new UserEditViewModel
            {
                UserDetails = _repository.GetUserDetails(userId),
                Roles = _repository.GetAllRoles(),
                Groups = _repository.GetModelsByGroup(userId)
            };
            return View(model);
        }

        [HttpPost]
        [Route("[action]/{userId}")]
        public IActionResult Edit(UserEditPostModel model)
        {
            if (ModelState.IsValid)
            {

                return RedirectToAction("Edit", new { userId = model.UserId }); //temp
            }
            return RedirectToAction("Edit", new { userId = model.UserId });
        }
    }
}
