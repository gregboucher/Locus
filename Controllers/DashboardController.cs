using Locus.Models;
//using Locus.ViewModels;
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

        public ViewResult Index()
        {
            var model = _assetRepository.GetAsset(1);
            return View(model);
        }
    }
}