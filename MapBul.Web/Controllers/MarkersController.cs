﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MapBul.DBContext;
using MapBul.SharedClasses;
using MapBul.SharedClasses.Constants;
using MapBul.Web.Auth;
using MapBul.Web.Filters;
using MapBul.Web.Models;
using MapBul.Web.Repository;
using Newtonsoft.Json;

namespace MapBul.Web.Controllers
{
    [Culture]
    public class MarkersController : Controller
    {
        /// <summary>
        /// Главная страница раздела маркеров
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [MyAuth(Roles = UserTypes.Admin + ", " + UserTypes.Editor)]
        public ActionResult Index()
        {
            return View();
        }


#region partials

        /// <summary>
        /// Частичное представление списка маркеров
        /// </summary>
        /// <returns></returns>
        [MyAuth(Roles = UserTypes.Admin + ", " + UserTypes.Editor)]
        public ActionResult _MarkersTablePartial()
        {
            var repo = DependencyResolver.Current.GetService<IRepository>();
            var auth = DependencyResolver.Current.GetService<IAuthProvider>();
            var userGuid = auth.UserGuid;
            var model = new MarkersListModel(userGuid);
            ViewBag.Statuses = repo.GetStatuses(userGuid);
            return PartialView("Partial/_MarkersTablePartial",model);
        }

        /// <summary>
        /// частичное представление модального окна формы добавления нового маркера
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [MyAuth(Roles = UserTypes.Admin)]
        public ActionResult _NewMarkerModalPartial()
        {
            var auth = DependencyResolver.Current.GetService<IAuthProvider>();
            var userGuid = auth.UserGuid;
            var repo = DependencyResolver.Current.GetService<IRepository>();
            var model = new NewMarkerModel();
            ViewBag.Cities = repo.GetCities().Where(c => c.Id != 0).ToList();
            ViewBag.Categories = repo.GetMarkerCategories();
            ViewBag.Discounts = repo.GetDiscounts();
            ViewBag.Statuses = repo.GetStatuses(userGuid);
            ViewBag.WeekDays = repo.GetWeekDays();
            return PartialView("Partial/_NewMarkerModalPartial", model);
        }

        /// <summary>
        /// частичное представление модального окна формы редактирования маркера
        /// </summary>
        /// <param name="markerId"></param>
        /// <returns></returns>
        [HttpPost]
        [MyAuth(Roles = UserTypes.Admin + ", " + UserTypes.Editor)]
        public ActionResult _EditMarkerModalPartial(int markerId)
        {
            var auth = DependencyResolver.Current.GetService<IAuthProvider>();
            var userGuid = auth.UserGuid;
            var repo = DependencyResolver.Current.GetService<IRepository>();
            var marker = repo.GetMarker(markerId);
            var model = new NewMarkerModel(marker);
            ViewBag.Cities = repo.GetCities().ToList();
            ViewBag.Categories = repo.GetMarkerCategories();
            ViewBag.Discounts = repo.GetDiscounts();
            ViewBag.Statuses = repo.GetStatuses(userGuid);
            ViewBag.WeekDays = repo.GetWeekDays();
            return PartialView("Partial/_EditMarkerModalPartial", model);
        }

#endregion

#region actions

        /// <summary>
        /// метод добавления нового маркера
        /// </summary>
        /// <param name="model"></param>
        /// <param name="openTimesString"></param>
        /// <param name="closeTimesString"></param>
        /// <param name="markerPhoto"></param>
        /// <param name="markerLogo"></param>
        /// <param name="markerPhotos"></param>
        /// <param name="lat"></param>
        /// <param name="lng"></param>
        /// <returns></returns>
        [HttpPost]
        [MyAuth(Roles = UserTypes.Admin)]
        public bool AddNewMarker(NewMarkerModel model, string openTimesString, string closeTimesString,
            HttpPostedFileBase markerPhoto, HttpPostedFileBase markerLogo, List<HttpPostedFileBase> markerPhotos,
            float lat, float lng)
        {
            if (Math.Abs(model.Lat) <= 0)
                model.Lat = lat;

            if (Math.Abs(model.Lng) <= 0)
                model.Lng = lng;

            var openTimes = JsonConvert.DeserializeObject<List<WorkTimeDay>>(openTimesString);
            var closeTimes = JsonConvert.DeserializeObject<List<WorkTimeDay>>(closeTimesString);
            var repo = DependencyResolver.Current.GetService<IRepository>();
            var auth = DependencyResolver.Current.GetService<IAuthProvider>();
            var userGuid = auth.UserGuid;

            if (markerPhoto != null)
            {
                FileProvider.DeleteFile(model.Photo);
                var filePath = FileProvider.SaveMarkerPhoto(markerPhoto);
                model.Photo = filePath;
            }

            if (markerLogo != null)
            {
                FileProvider.DeleteFile(model.Logo);
                var filePath = FileProvider.SaveMarkerLogo(markerLogo);
                model.Logo = filePath;
            }

            if (markerPhotos != null && markerPhotos.Any())
            {
                model.marker_photos = new List<marker_photos>();
                foreach (var photo in markerPhotos)
                {
                    var filePath = FileProvider.SaveMarkerPhoto(photo);
                    model.marker_photos.Add(new marker_photos { Photo = filePath });
                }
            }

            if (string.IsNullOrEmpty(model.Name) && !string.IsNullOrEmpty(model.NameEn))
                model.Name = model.NameEn;

            if (string.IsNullOrEmpty(model.Introduction) && !string.IsNullOrEmpty(model.IntroductionEn))
                model.Introduction = model.IntroductionEn;

            if (string.IsNullOrEmpty(model.Description) && !string.IsNullOrEmpty(model.DescriptionEn))
                model.Description = model.DescriptionEn;

            var tempNewMarkerId = repo.AddMarker(model, openTimes, closeTimes, userGuid);

            return true;
        }

        [HttpPost]
        [MyAuth(Roles = UserTypes.Admin + ", " + UserTypes.Editor)]
        public bool RemovePhotosMarker(HttpPostedFileBase markerPhoto, HttpPostedFileBase markerLogo, IEnumerable<HttpPostedFileBase> markerPhotosExt)
        {
            var repo = DependencyResolver.Current.GetService<IRepository>();
            return true;
        }

        public class RemovePhoto
        {
            public int id;
            public string photo;
        }

        /// <summary>
        /// метод сохранения изменений маркера
        /// </summary>
        /// <param name="model"></param>
        /// <param name="openTimesString"></param>
        /// <param name="closeTimesString"></param>
        /// <param name="markerPhoto"></param>
        /// <param name="markerLogo"></param>
        /// <param name="markerPhotos"></param>
        /// <param name="removePhotos"></param>
        /// <returns></returns>
        [HttpPost]
        [MyAuth(Roles = UserTypes.Admin + ", " + UserTypes.Editor)]
        public bool EditMarker(NewMarkerModel model, string openTimesString, string closeTimesString,
            HttpPostedFileBase markerPhoto,  HttpPostedFileBase markerLogo, IEnumerable<HttpPostedFileBase> markerPhotos, string removePhotosString)
        {
            var repo = DependencyResolver.Current.GetService<IRepository>();
            var auth = DependencyResolver.Current.GetService<IAuthProvider>();

            if (repo.GetCities().All(tc => tc.Id != model.CityId))
                model.CityId = 0;

            if (string.IsNullOrEmpty(model.Street) || model.Street== "Unnamed Road")
                model.Street = "Улица не определена";

            if (string.IsNullOrEmpty(model.House))
                model.House = "Нет";

            var openTimes = JsonConvert.DeserializeObject<List<WorkTimeDay>>(openTimesString);
            var closeTimes = JsonConvert.DeserializeObject<List<WorkTimeDay>>(closeTimesString);
            var removePhotos = JsonConvert.DeserializeObject<List<RemovePhoto>>(removePhotosString);
            var removePhotoIds = removePhotos.Select(p => p.id);

            var userGuid = auth.UserGuid;
            if (markerPhoto != null)
            {
                FileProvider.DeleteFile(model.Photo);
                var filePath = FileProvider.SaveMarkerPhoto(markerPhoto);
                model.Photo = filePath;
            }

            if (markerLogo != null)
            {
                FileProvider.DeleteFile(model.Logo);
                var filePath = FileProvider.SaveMarkerLogo(markerLogo);
                model.Logo = filePath;
            }

            if (markerPhotos != null && markerPhotos.Any())
            {
                model.marker_photos = new List<marker_photos>();
                foreach (var photo in markerPhotos)
                {
                    var filePath = FileProvider.SaveMarkerPhoto(photo);
                    model.marker_photos.Add(new marker_photos{Photo = filePath});
                }
            }

            if (removePhotos != null && removePhotos.Any())
                foreach (var photo in removePhotos)
                    FileProvider.DeleteFile(photo.photo);

            if (string.IsNullOrEmpty(model.Name) && !string.IsNullOrEmpty(model.NameEn))
                model.Name = model.NameEn;

            if (string.IsNullOrEmpty(model.Introduction) && !string.IsNullOrEmpty(model.IntroductionEn))
                model.Introduction = model.IntroductionEn;

            if (string.IsNullOrEmpty(model.Description) && !string.IsNullOrEmpty(model.DescriptionEn))
                model.Description = model.DescriptionEn;

            repo.EditMarker(model, openTimes, closeTimes, removePhotoIds?.ToList(), userGuid);

            return true;
        }

        /// <summary>
        /// Метод изменения статуса маркера
        /// </summary>
        /// <param name="markerId"></param>
        /// <param name="statusId"></param>
        /// <returns></returns>
        [HttpPost]
        [MyAuth(Roles = UserTypes.Admin + ", " + UserTypes.Editor)]
        public bool ChangeMarkerStatus(int markerId, int statusId)
        {
            var repo = DependencyResolver.Current.GetService<IRepository>();
            repo.ChangeMarkerStatus(markerId, statusId);
            return true;
        }

        /// <summary>
        /// метод удаления маркера
        /// </summary>
        /// <param name="markerId"></param>
        /// <returns></returns>
        [HttpPost]
        [MyAuth(Roles = UserTypes.Admin + ", " + UserTypes.Editor)]
        public bool DeleteMarker(int markerId)
        {
            var repo = DependencyResolver.Current.GetService<IRepository>();
            repo.DeleteMarker(markerId);
            return true;
        }

#endregion
  
    }

}