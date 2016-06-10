﻿using System.Web.Mvc;
using MapBul.SharedClasses.Constants;
using MapBul.Web.Auth;
using MapBul.Web.Models;

namespace MapBul.Web.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        [MyAuth(Roles = UserTypes.Admin + ", " + UserTypes.Journalist + ", " + UserTypes.Editor)]
        public ActionResult Index()
        {
            var mapInfo = new MapInfoModel();
            return View(mapInfo);
        }

        
    }
}