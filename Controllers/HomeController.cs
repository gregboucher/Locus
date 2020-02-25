using Locus.Data;
using Locus.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Locus.Controllers
{
    [Route("[controller]")]
    public class HomeController : Controller
    {
        private readonly IRepository _repository;

        public HomeController(IRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        [Route("~/")]
        [Route("")]
        [Route("[action]")]
        public ViewResult Dashboard()
        {
            var viewModel = new HomeDashboardViewModel
            {
                Controller = "Home",
                Page = "Dashboard",
                Icon = "home",
                CountDueToday = _repository.CountDueToday(),
                CountOverdue = _repository.CountOverdue(),
                CountCreatedToday = _repository.CountUsersCreatedToday(),
                CollectionsOfUsers = _repository.GetUsersByCollection()
            };
            return View(viewModel);
        }
    }
}