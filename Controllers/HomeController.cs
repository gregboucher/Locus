using Locus.Models;
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
                DistinctUsersByGroupCount = _repository.DistinctUsersByGroupCount(),
                DueTodayCount = _repository.DueTodayCount(),
                OverdueCount = _repository.OverdueCount(),
                CreatedTodayCount = _repository.CreatedTodayCount(),
                Groups = _repository.GetAssignmentsByGroup()
            };
            return View(model);
        }
    }
}