using Locus.Models;
using Locus.ViewModels;
using Microsoft.AspNetCore.Mvc;

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

        [Route("")]
        [Route("[action]")]
        public ViewResult Create()
        {
            UserCreateViewModel model = new UserCreateViewModel
            {
                Controller = ControllerContext.RouteData.Values["controller"].ToString(),
                Action = ControllerContext.RouteData.Values["action"].ToString(),
            };
            return View(model);
        }

        [Route("[action]/{id}")]
        public ViewResult Edit(int id)
        {
            UserEditViewModel model = new UserEditViewModel
            {
                Controller = ControllerContext.RouteData.Values["controller"].ToString(),
                Action = ControllerContext.RouteData.Values["action"].ToString(),
            };
            return View(model);
        }
    }
}
