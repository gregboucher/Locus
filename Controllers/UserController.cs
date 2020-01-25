﻿using Locus.Models;
using Locus.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

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

        //[Route("~/")]
        [Route("")]
        [Route("[action]")]
        [HttpGet]
        public ViewResult Create()
        {
            UserCreateViewModel model = new UserCreateViewModel
            {
                
            };
            return View(model);
        }

        [Route("[action]")]
        [HttpPost]
        public ViewResult Create(AssetList assetList)
        {
            return View();
        }

        [Route("[action]/{id}")]
        public ViewResult Edit(int id)
        {
            UserEditViewModel model = new UserEditViewModel
            {
                
            };
            return View(model);
        }
    }
}
