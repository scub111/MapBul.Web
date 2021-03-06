﻿using System.Web.Mvc;
using MapBul.SharedClasses.Constants;
using MapBul.Web.Auth;
using MapBul.Web.Filters;
using MapBul.Web.Models;

namespace MapBul.Web.Controllers
{
    [Culture]
    public class HomeController : Controller
    {
        /// <summary>
        /// Главная страница сайта
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [MyAuth(Roles = UserTypes.Admin + ", " + UserTypes.Journalist + ", " + UserTypes.Editor)]
        public ActionResult Index()
        {
            var mapInfo = new MapInfoModel();
            return View(mapInfo);
        }
    }
}