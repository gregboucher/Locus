using Locus.Models;
using Locus.ViewModels;
using System.Collections.Generic;
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
        [Route("Home")]
        [Route("Dashboard/Index")]
        public ViewResult Index()
        {
            DashboardIndexViewModel model = new DashboardIndexViewModel
            {
                PageTitle = "Dashboard",
                AssignedAssetsCount = _repository.AssignedAssetCount();
                DueTodayCount = _repository.DueTodayCount();
                OverdueCount = _repository.OverdueCount();
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