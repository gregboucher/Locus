using Locus.Models;
using Locus.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Locus.Controllers
{
    public class DashboardController : Controller
    {
        private readonly IRepository _repository;

        public DashboardController(IRepository repository)
        {
            _repository = repository;
        }

        [Route("")]
        [Route("Dashboard")]
        public ViewResult Index()
        {
            DashboardIndexViewModel model = new DashboardIndexViewModel
            {
                PageTitle = "Dashboard",
                Controller = ControllerContext.RouteData.Values["controller"].ToString(),
                Action = ControllerContext.RouteData.Values["action"].ToString(),
                AssignedUserCount = _repository.AssignedUserCount(),
                DueTodayCount = _repository.DueTodayCount(),
                OverdueCount = _repository.OverdueCount(),
                Groups = _repository.GetAssignmentsByGroup()
            };
            return View(model);
        }

        [Route("Dashboard/GetAsset/{SerialNumber}")]
        public ViewResult GetAsset(string serialNumber)
        {
            var model = _repository.GetAsset(serialNumber);
            return View(model);
        }
    }
}