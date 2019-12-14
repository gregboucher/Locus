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

        [Route("")]
        [Route("[action]")]
        public ViewResult Dashboard()
        {
            HomeDashboardViewModel model = new HomeDashboardViewModel
            {
                Controller = ControllerContext.RouteData.Values["controller"].ToString(),
                Action = ControllerContext.RouteData.Values["action"].ToString(),
                AssignedUserCount = _repository.AssignedUserCount(),
                DueTodayCount = _repository.DueTodayCount(),
                OverdueCount = _repository.OverdueCount(),
                Groups = _repository.GetAssignmentsByGroup()
            };
            return View(model);
        }
    }
}