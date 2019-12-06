using Locus.Models;
using Locus.ViewModels;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace Locus.Controllers
{
    public class DashboardController : Controller
    {
        private readonly IRepository _assetRepository;

        public DashboardController(IRepository assetRepository)
        {
            _assetRepository = assetRepository;
        }

        [Route("")]
        [Route("Dashboard")]
        [Route("Home")]
        [Route("Dashboard/Index")]
        public ViewResult Index()
        {
            IEnumerable<Group> model = _assetRepository.GetActiveAssignments();
            return View(model);
        }

        [Route("Dashboard/GetAsset/{SerialNumber}")]
        public ViewResult GetAsset(string SerialNumber)
        {
            var model = _assetRepository.GetAsset(SerialNumber);
            return View(model);
        }
    }
}