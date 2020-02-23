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
            HomeDashboardViewModel viewModel = new HomeDashboardViewModel
            {
                Controller = "Home",
                Page = "Dashboard",
                Icon = "home",
                CountDueToday = _repository.CountDueToday(),
                CountOverdue = _repository.CountOverdue(),
                CountCreatedToday = _repository.CountUsersCreatedToday(),
                GroupedUsers = _repository.GetAssignmentsByGroup()
            };
            return View(viewModel);
        }
    }
}