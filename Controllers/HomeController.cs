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

        [Route("~/")]
        [Route("")]
        [Route("[action]")]
        public ViewResult Dashboard()
        {
            HomeDashboardViewModel model = new HomeDashboardViewModel
            {
                CountDueToday = _repository.CountDueToday(),
                CountOverdue = _repository.CountOverdue(),
                CountCreatedToday = _repository.CountUsersCreatedToday(),
                Groups = _repository.GetAssignmentsByGroup()
            };
            return View(model);
        }
    }
}