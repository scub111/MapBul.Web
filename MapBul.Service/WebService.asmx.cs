﻿using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Web;
using System.Web.Services;
using MapBul.DBContext;
using MapBul.SharedClasses;
using MapBul.SharedClasses.Constants;
using Newtonsoft.Json;

namespace MapBul.Service
{
    /// <summary>
    /// Summary description for WebService
    /// </summary>
    [WebService(Namespace = "http://MapBul.ru/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(true)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class WebService : System.Web.Services.WebService
    {
        #region private
            /// <summary>
            /// Метод возвращает список ИД категорий от данной до корневой
            /// </summary>
            /// <param name="category"></param>
            /// <returns></returns>
        private List<int> GetCategoriesBranch(category category)
        {
            var result = new List<int>();
            var currentCategory = category;
            while (currentCategory != null)
            {
                result.Add(currentCategory.Id);
                currentCategory = currentCategory.category2;
            }
            return result;
        }
        /// <summary>
        /// Метод возвращает полный URL до файла
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private string MapUrl(string filePath)
        {
            //return $@"{HttpContext.Current.Request.Url.Scheme}://{HttpContext.Current.Request.Url.Authority}/{filePath}".Replace("\\", "/");
            return !string.IsNullOrEmpty(filePath) ? $@"http://web.mapbul.scub111.com/{filePath}".Replace("\\", "/") : null;
        }
        /// <summary>
        /// Метод возвращает описатель пользователя в зависимости от его типа
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        private object GetUserDescriptor(user user)
        {
            switch (user.usertype.Tag)
            {
                case UserTypes.Editor:
                    return user.editor.First();
                case UserTypes.Journalist:
                    return user.journalist.First();
                case UserTypes.Tenant:
                    return user.tenant.First();
                case UserTypes.Guide:
                    return user.guide.First();
                case UserTypes.Admin:
                    return new { FirstName = "Администратор", MiddleName = "Администратор", LastName = "Администратор" };
                default:
                    throw new MyException(Errors.NotFound);
            }
        }

        #endregion

        #region webMethods

        /// <summary>
        /// Метод проверки адреса электронной почты и пароля пользователя
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        [WebMethod]
        public string Authorize(string email, string password)
        {
            try
            {
                var repo = new MySqlRepository();
                var user = repo.GetUserByEmailAndPassword(email, password); //вытащили пользователя

                dynamic userDescriptor = GetUserDescriptor(user);

                var userType = repo.GetMobileUserTypeById(user.UserTypeId); //вытащили его тип
                var result =
                    new JsonResult(new List<Dictionary<string, object>>
                    {
                        new Dictionary<string, object>
                        {
                            {"FirstName", userDescriptor.FirstName},
                            {"UserTypeTag", userType.Tag},
                            {"UserTypeDescription", userType.Description}
                        }
                    });
                result.AddObjectToResult(user, 0);
                return JsonConvert.SerializeObject(result);
            }
            catch (MyException e)
            {
                return JsonConvert.SerializeObject(new JsonResult(e.Error.Message));
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new JsonResult(e.Message));
            }
        }
        /// <summary>
        /// Метод возвращает тип пользователя в виде строки по его ID
        /// </summary>
        /// <param name="id"></param>
        /// <param name="userGuid"></param>
        /// <returns></returns>
        [WebMethod]
        public string GetUserTypeById(int id, string userGuid)
        {
            try
            {
                var repo = new MySqlRepository();
                var userType = repo.GetMobileUserTypeById(id);
                var result = new JsonResult(new List<Dictionary<string, object>>());
                result.AddObjectToResult(userType, 0);
                return JsonConvert.SerializeObject(result);
            }
            catch (MyException e)
            {
                return JsonConvert.SerializeObject(new JsonResult(e.Error.Message));
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new JsonResult(e.Message));
            }
        }

        /// <summary>
        /// Метод возвращает список маркеров в указанном прямоугольнике
        /// </summary>
        /// <param name="p1Lat"></param>
        /// <param name="p1Lng"></param>
        /// <param name="p2Lat"></param>
        /// <param name="p2Lng"></param>
        /// <param name="userGuid"></param>
        /// <param name="appLang"></param>
        /// <returns></returns>
        [WebMethod]
        public string GetMarkers(double p1Lat, double p1Lng, double p2Lat, double p2Lng, string userGuid, string appLang)
        {
            try
            {
                var repo = new MySqlRepository();
                var markers = repo.GetMarkersInSquare(p1Lat, p1Lng, p2Lat, p2Lng);

                var result = new JsonResult(new List<Dictionary<string, object>>());

                var i = 0;
                var userId = 0;
                if (userGuid != "")
                    userId = repo.GetUser(userGuid).Id;

                foreach (var marker in markers)
                {
                    if (!marker.Personal || marker.UserId == userId)
                    {
                        var Name = marker.Name;
                        if (appLang != "ru" && !string.IsNullOrEmpty(marker.NameEn))
                        {
                            Name = marker.NameEn;
                        }

                        result.AddObjectToResult(
                            new
                            {
                                marker.Id,
                                Name,
                                marker.Lat,
                                marker.Lng,
                                Icon = MapUrl(marker.category.Pin),
                                Logo = MapUrl(marker.Logo),
                                WorkTime = marker.worktime.Select(wt => new
                                {
                                    wt.OpenTime,
                                    wt.CloseTime,
                                    wt.weekday.Id
                                }).ToList(),
                                CategoriesBranch = GetCategoriesBranch(marker.category),
                                SubCategories =
                                    marker.subcategory.Select(
                                        s =>
                                            (appLang != "ru" && !string.IsNullOrEmpty(marker.NameEn))
                                                ? s.category.EnName
                                                : s.category.Name).ToList(),
                                marker.Wifi,
                                marker.Personal
                            }, i);
                        i++;
                    }
                }

                return JsonConvert.SerializeObject(result);
            }
            catch (MyException e)
            {
                return JsonConvert.SerializeObject(new JsonResult(e.Error.Message));
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new JsonResult(e.Message));
            }
        }

        /// <summary>
        /// Метод возвращает список непереданных в данной сессии маркеров в указанном прямоугольнике
        /// </summary>
        /// <param name="p1Lat"></param>
        /// <param name="p1Lng"></param>
        /// <param name="p2Lat"></param>
        /// <param name="p2Lng"></param>
        /// <param name="sessionId"></param>
        /// <param name="userGuid"></param>
        /// <param name="appLang"></param>
        /// <returns></returns>
        [WebMethod]
        public string GetSessionMarkers(double p1Lat, double p1Lng, double p2Lat, double p2Lng, string sessionId, string userGuid, string appLang)
        {
            try
            {
                var repo = new MySqlRepository();
                var markers = repo.GetMarkersInSquare(p1Lat, p1Lng, p2Lat, p2Lng, sessionId);

                var result = new JsonResult(new List<Dictionary<string, object>>());
                var userId = 0;
                if (userGuid != "")
                    userId = repo.GetUser(userGuid).Id;

                var i = 0;

                foreach (var marker in markers)
                {
                    if (!marker.Personal || marker.UserId == userId)
                    {
                        var Name = marker.Name;
                        if (appLang != "ru" && !string.IsNullOrEmpty(marker.NameEn))
                        {
                            Name = marker.NameEn;
                        }
                        result.AddObjectToResult(
                            new
                            {
                                marker.Id,
                                Name,
                                marker.Lat,
                                marker.Lng,
                                Icon = MapUrl(marker.category.Pin),
                                Logo = MapUrl(marker.Logo),
                                WorkTime = marker.worktime.Select(wt => new
                                {
                                    wt.OpenTime,
                                    wt.CloseTime,
                                    wt.weekday.Id
                                }).ToList(),
                                CategoriesBranch = GetCategoriesBranch(marker.category),
                                SubCategories =
                                    marker.subcategory.Select(
                                        s =>
                                            (appLang != "ru" && !string.IsNullOrEmpty(marker.NameEn))
                                                ? s.category.EnName
                                                : s.category.Name).ToList(),
                                marker.Wifi,
                                marker.Personal
                            }, i);
                        i++;
                    }
                }

                return JsonConvert.SerializeObject(result);
            }
            catch (MyException e)
            {
                return JsonConvert.SerializeObject(new JsonResult(e.Error.Message));
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new JsonResult(e.Message));
            }
        }

        /// <summary>
        /// Метод удаляет данные сессии
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        [WebMethod]
        public string RemoveRequestSession(string sessionId)
        {
            try
            {
                var repo = new MySqlRepository();
                repo.RemoveRequestSession(sessionId);

                var result = new JsonResult(new List<Dictionary<string, object>>());
                return JsonConvert.SerializeObject(result);
            }
            catch (MyException e)
            {
                return JsonConvert.SerializeObject(new JsonResult(e.Error.Message));
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new JsonResult(e.Message));
            }
        }

        /// <summary>
        /// Метод возвращает подробное описание маркера
        /// </summary>
        /// <param name="markerId"></param>
        /// <param name="appLang"></param>
        /// <returns></returns>
        [WebMethod]
        public string GetMarkerDescription(int markerId, string appLang)
        {
            var repo = new MySqlRepository();
            var marker = repo.GetMarker(markerId);
            var result = new JsonResult(new List<Dictionary<string, object>>());

            var haveRelatedEvents = repo.GetEvents().Any(e => e.MarkerId == markerId);

            var categories = repo.GetMarkerCategories();
            //var tempPhotosPaths = repo.GetArrayOfPathsMarkerPhotos(markerId);
            //List<string> tempPhotosPathsUrl = tempPhotosPaths.Select(MapUrl).ToList();
            if (appLang != "ru")
            {
                if (!string.IsNullOrEmpty(marker.NameEn))
                {
                    marker.Name = marker.NameEn;
                }
                if (!string.IsNullOrEmpty(marker.DescriptionEn))
                {
                    marker.Description = marker.DescriptionEn;
                }
                if (!string.IsNullOrEmpty(marker.IntroductionEn))
                {
                    marker.Introduction = marker.IntroductionEn;
                }
            }
            marker.Photo = MapUrl(marker.Photo);
            marker.Logo = MapUrl(marker.Logo);
            result.AddObjectToResult(marker, 0);
            result.AddObjectToResult(new {HaveRelatedEvents = haveRelatedEvents}, 0);

            //Todo: Временная заглушка пока все маркеры не перейдут на новый формат хранения фотографий
            var photos = repo.GetArrayOfPathsMarkerPhotos(markerId).Select(MapUrl).ToList();
            if (photos.Count < 10 && !string.IsNullOrEmpty(marker.Photo))
                photos.Insert(0, marker.Photo);

            var photosMini = repo.GetArrayOfPathsMarkerPhotosMini(markerId).Select(MapUrl).ToList();
            if (photos.Count < 10 && !string.IsNullOrEmpty(marker.Photo))
                photosMini.Insert(0, marker.Photo);

            result.AddObjectToResult(new
            {
                Photos = photos,
                PhotosMini = photosMini,
                Phones = marker.phone.Select(p => new {p.Primary, p.Number}).ToList(),
                Discount = marker.discount.Value,
                Subcategories = marker.subcategory.Select(s => new {s.category.Id, s.category.Name}).ToList(),
                CityName = marker.city.Name,
                Pin = MapUrl(marker.category.Pin),
                Icon = MapUrl(marker.category.Icon),
                WorkTime = marker.worktime.Select(wt => new
                {
                    wt.OpenTime,
                    wt.CloseTime,
                    wt.weekday.Id
                }).ToList(),
                repo.GetMarkerCategories().First(c=>c.Id==GetCategoriesBranch(marker.category).Last()).Color,
                CategoriesBranch =
                    GetCategoriesBranch(marker.category)
                        .Select(
                            number =>
                                new
                                {
                                    categories.First(c => c.Id == number).Id,
                                    Name =
                                            (appLang != "ru") &&
                                            !string.IsNullOrEmpty(categories.First(c => c.Id == number).EnName)
                                                ? categories.First(c => c.Id == number).EnName
                                                : categories.First(c => c.Id == number).Name
                                })
                        .ToList()
            }, 0);
            return JsonConvert.SerializeObject(result);
        }

        /// <summary>
        /// Метод возвращает список корневых категорий маркеров
        /// </summary>
        /// <returns></returns>
        [WebMethod]
        public string GetRootCategories(string appLang)
        {
            var repo = new MySqlRepository();
            var rootCategories = repo.GetRootMarkerCategories();
            var index = 0;
            var result = new JsonResult(new List<Dictionary<string, object>>());
            foreach (var rootCategory in rootCategories)
            {
                if (appLang != "ru" && !string.IsNullOrEmpty(rootCategory.EnName))
                {
                    rootCategory.Name = rootCategory.EnName;
                }

                rootCategory.Icon = MapUrl(rootCategory.Icon);
                rootCategory.Pin = MapUrl(rootCategory.Pin);
                var childCategories = repo.GetChildCategories(rootCategory.Id);
                result.AddObjectToResult(rootCategory, index);
                result.AddObjectToResult(
                    new
                    {
                        ChildCategories =
                            childCategories.Select(
                                c =>
                                (appLang != "ru" && !string.IsNullOrEmpty(c.EnName))?
                                    new
                                    {
                                        c.Id,
                                        c.ParentId,
                                        c.AddedDate,
                                        Pin = MapUrl(c.Pin),
                                        Icon = MapUrl(c.Icon),
                                        Name = c.EnName,
                                        c.Color
                                    } :
                                    new
                                    {
                                        c.Id,
                                        c.ParentId,
                                        c.AddedDate,
                                        Pin = MapUrl(c.Pin),
                                        Icon = MapUrl(c.Icon),
                                        Name = c.Name,
                                        c.Color
                                    }
                                    ).ToList()
                    }, index);
                index++;
            }
            return JsonConvert.SerializeObject(result);
        }

        /// <summary>
        /// Метод возращает Набор статей. Если в метод не передаются параметры, то возвращаются первые 15 статей, далее, статьи подгружаются при прокрутке по 15 штук.
        /// </summary>
        /// <param name="refresh">Если true, то подгружаются статьи, добавленные с последнего обновления</param>
        /// <param name="existingDateTime"></param>
        /// <param name="appLang"></param>
        /// <returns></returns>
        [WebMethod]
        public string GetRecentArticles(string appLang, int page, int size, bool refresh = false, DateTime? existingDateTime = null)
        {
            var repo = new MySqlRepository();
            var articles = repo.GetArticles();
            var result = new JsonResult(new List<Dictionary<string, object>>());
            List<article> filteredArticles;

            /*
            if (existingDateTime != null && refresh)
                filteredArticles =
                    articles.Where(a => a.PublishedDate > existingDateTime)
                        .OrderByDescending(a => a.PublishedDate)
                        .ToList();
            else if (existingDateTime != null)
                filteredArticles =
                    articles.Where(a => a.PublishedDate < existingDateTime)
                        .OrderByDescending(a => a.PublishedDate)
                        .Skip((page - 1) * size)
                        .Take(size).ToList();
            else
                filteredArticles = articles
                    .OrderByDescending(a => a.PublishedDate)
                    .Skip((page - 1) * size)
                    .Take(size)
                    .ToList();
            */
            filteredArticles =
                articles
                    // .Where(a => existingDateTime < a.StartDate || a.StartDate <= existingDateTime && existingDateTime <= a.EndDate)
                    // .OrderBy(a => a.StartDate)
                    .Skip((page - 1) * size)
                    .Take(size)
                    .ToList();

            var i = 0;
            foreach (var article in filteredArticles)
            {
                article.Photo = MapUrl(article.Photo);
                article.TitlePhoto = MapUrl(article.TitlePhoto);

                object authorName;

                switch (article.user.usertype.Tag)
                {
                    case UserTypes.Editor:
                        authorName = new
                        {
                            article.user.editor.First().FirstName,
                            article.user.editor.First().MiddleName,
                            article.user.editor.First().LastName
                        };
                        break;
                    case UserTypes.Journalist:
                        authorName = new
                        {
                            article.user.journalist.First().FirstName,
                            article.user.journalist.First().MiddleName,
                            article.user.journalist.First().LastName
                        };
                        break;
                    default:
                        authorName = new
                        {
                            FirstName = "Администратор",
                            MiddleName = "Администратор",
                            LastName = "Администратор"
                        };
                        break;
                }

                if (appLang != "ru")
                {
                    if (!string.IsNullOrEmpty(article.TitleEn))
                    {
                        article.Title = article.TitleEn;
                    }
                    if (!string.IsNullOrEmpty(article.DescriptionEn))
                    {
                        article.Description = article.DescriptionEn;
                    }
                    if (!string.IsNullOrEmpty(article.TextEn))
                    {
                        article.Text= article.TextEn;
                    }
                    if (!string.IsNullOrEmpty(article.SourcePhotoEn))
                    {
                        article.SourcePhoto= article.SourcePhotoEn;
                    }
                    if (!string.IsNullOrEmpty(article.SourceUrlEn))
                    {
                        article.SourceUrl = article.SourceUrlEn;
                    }
                }

                result.AddObjectToResult(article, i);
                string markerAddress=null;
                string markerAddressName = null;
                if (article.marker != null)
                {
                    markerAddress = article.marker.city.country.Name + ", " + article.marker.city.Name + ", " +
                                    article.marker.Street + " " + article.marker.House + " " + article.marker.Buliding;
                    markerAddressName = article.marker.Name;
                }

                result.AddObjectToResult(new { AuthorName = authorName, MarkerAddress = markerAddress, AddressName = markerAddressName }, i);
                result.AddObjectToResult(
                    new
                    {
                        Subcategories =
                            article.articlesubcategory.Select(
                                a =>
                                    (appLang != "ru" && !string.IsNullOrEmpty(a.category.EnName))
                                        ? a.category.EnName
                                        : a.category.Name).ToList()
                    }, i);

                i++;
            }
            return JsonConvert.SerializeObject(result);
        }

        /// <summary>
        /// Метод возращает набор событий. Если в метод не передаются параметры, то возвращаются первые 15 событий, далее, события подгружаются при прокрутке по 15 штук.
        /// </summary>
        /// <param name="appLang"></param>
        /// <param name="page"></param>
        /// <param name="size"></param>
        /// <param name="refresh">Если true, то подгружаются события, добавленные с последнего обновления</param>
        /// <param name="existingDateTime"></param>
        /// <returns></returns>
        [WebMethod]
        public string GetRecentEvents(string appLang, int page, int size, bool refresh = false,
            DateTime? existingDateTime = null)
        {
            var repo = new MySqlRepository();
            var articles = repo.GetEvents();
            var result = new JsonResult(new List<Dictionary<string, object>>());
            List<article> filteredArticles;

            /*
            if (existingDateTime != null && refresh)
                filteredArticles =
                    articles.Where(a => a.PublishedDate > existingDateTime)
                        .OrderByDescending(a => a.PublishedDate)
                        .ToList();
            else if (existingDateTime != null)
                filteredArticles =
                    articles.Where(a => a.PublishedDate < existingDateTime)
                        .OrderByDescending(a => a.PublishedDate)
                        .Skip((page - 1) * size)
                        .Take(size)
                        .ToList();
            else
                filteredArticles = articles.OrderByDescending(a => a.PublishedDate)
                    .Skip((page - 1) * size)
                    .Take(size)
                    .ToList();
            */

            filteredArticles =
                articles.Where(a => existingDateTime < a.StartDate || a.StartDate <= existingDateTime && existingDateTime <= a.EndDate)
                .OrderBy(a => a.StartDate)
                .Skip((page - 1) * size)
                .Take(size)
                .ToList();

            var i = 0;
            foreach (var article in filteredArticles)
            {
                article.Photo = MapUrl(article.Photo);
                article.TitlePhoto = MapUrl(article.TitlePhoto);

                object authorName;

                switch (article.user.usertype.Tag)
                {
                    case UserTypes.Editor:
                        authorName = new
                        {
                            article.user.editor.First().FirstName,
                            article.user.editor.First().MiddleName,
                            article.user.editor.First().LastName
                        };
                        break;
                    case UserTypes.Journalist:
                        authorName = new
                        {
                            article.user.journalist.First().FirstName,
                            article.user.journalist.First().MiddleName,
                            article.user.journalist.First().LastName
                        };
                        break;
                    default:
                        authorName = new
                        {
                            FirstName = "Администратор",
                            MiddleName = "Администратор",
                            LastName = "Администратор"
                        };
                        break;
                }

                if (appLang != "ru")
                {
                    if (!string.IsNullOrEmpty(article.TitleEn))
                    {
                        article.Title = article.TitleEn;
                    }
                    if (!string.IsNullOrEmpty(article.DescriptionEn))
                    {
                        article.Description = article.DescriptionEn;
                    }
                    if (!string.IsNullOrEmpty(article.TextEn))
                    {
                        article.Text = article.TextEn;
                    }
                    if (!string.IsNullOrEmpty(article.SourcePhotoEn))
                    {
                        article.SourcePhoto = article.SourcePhotoEn;
                    }
                    if (!string.IsNullOrEmpty(article.SourceUrlEn))
                    {
                        article.SourceUrl = article.SourceUrlEn;
                    }
                }
                result.AddObjectToResult(article, i);
                string markerAddress = null;
                string markerAddressName = null;
                if (article.marker != null)
                {
                    markerAddress = article.marker.city.country.Name + ", " + article.marker.city.Name + ", " +
                                    article.marker.Street + " " + article.marker.House + " " + article.marker.Buliding;
                    markerAddressName = article.marker.Name;
                }

                result.AddObjectToResult(
                    new {AuthorName = authorName, MarkerAddress = markerAddress, AddressName = markerAddressName}, i);
                result.AddObjectToResult(new
                {
                    Subcategories =
                    article.articlesubcategory.Select(
                        a => (appLang != "ru" && !string.IsNullOrEmpty(a.category.EnName))
                            ? a.category.EnName
                            : a.category.Name).ToList(),
                    StopDate = article.EndDate
                }, i);

                i++;
            }
            return JsonConvert.SerializeObject(result);
        }

        //ToDo:устаревший 
        /// <summary>
        /// Метод добавления нового маркера
        /// </summary>
        /// <param name="userGuid"></param>
        /// <param name="name"></param>
        /// <param name="introduction"></param>
        /// <param name="description"></param>
        /// <param name="cityId"></param>
        /// <param name="baseCategoryId"></param>
        /// <param name="lat"></param>
        /// <param name="lng"></param>
        /// <param name="entryTicket"></param>
        /// <param name="discount"></param>
        /// <param name="street"></param>
        /// <param name="house"></param>
        /// <param name="building"></param>
        /// <param name="floor"></param>
        /// <param name="site"></param>
        /// <param name="email"></param>
        /// <param name="photoBase64"></param>
        /// <param name="subCategoryIds"></param>
        /// <param name="phones"></param>
        /// <param name="openTimes"></param>
        /// <param name="closeTimes"></param>
        /// <param name="isPersonal">Указывает, является ли метка персональной</param>
        /// <param name="appLang"></param>
        /// <returns></returns>
        [WebMethod]
        public string CreateMarker(string userGuid, string name, string introduction, string description, int cityId,
            int baseCategoryId, double lat, double lng, string entryTicket, int discount, string street, string house,
            string building, string floor, string site, string email, string[] photoBase64, int[] subCategoryIds,
            string[] phones, List<KeyValue<int, int>> openTimes, List<KeyValue<int, int>> closeTimes, bool isPersonal, string appLang)
        {
            try
            {
                if (string.IsNullOrEmpty(entryTicket))
                    entryTicket = "Нет";

                var repo = new MySqlRepository();
                repo.CheckUser(userGuid);
                var user = repo.GetUser(userGuid);
                var userType = repo.GetMobileUserTypeById(user.UserTypeId);
                var permittedUserTypes = new[] {UserTypes.Guide};

                if (!isPersonal)
                {
                    if (!permittedUserTypes.Contains(userType.Tag))
                        throw new MyException(Errors.NotPermitted);
                    if (!repo.HavePermissions(user.Guid, cityId))
                        throw new MyException(Errors.NotPermitted);
                    if (string.IsNullOrEmpty(house))
                    {
                        house = "Без дома";
                    }
                    if (string.IsNullOrEmpty(street))
                    {
                        street = "Без улицы";
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(street))
                    {
                        street = "Без улицы";
                    }
                    if (string.IsNullOrEmpty(house))
                    {
                        house = "Без дома";
                    }
                    //if (string.IsNullOrEmpty(building))
                    //{
                    //    building = "test";
                    //}
                    //if (string.IsNullOrEmpty(floor))
                    //{
                    //    floor = "test";
                    //}
                }


                //var bytes = Convert.FromBase64String(photoBase64);

                //var photoPath = FileProvider.SaveMarkerPhoto(bytes);
                var logoPath = string.Empty;
                if (photoBase64.Length > 0)
                {
                    logoPath = FileProvider.SaveMarkerLogo(Convert.FromBase64String(photoBase64.LastOrDefault()));
                }

                var newMarker = new marker
                {
                    Name = name,
                    Introduction = introduction,
                    Description = description,
                    CityId = cityId,
                    BaseCategoryId = baseCategoryId,
                    EntryTicket = entryTicket,
                    Buliding = building,
                    DiscountId = repo.GetDiscountId(discount),
                    Email = email,
                    Floor = floor,
                    House = house,
                    Lat = (float) lat,
                    Lng = (float) lng,
                    Site = site,
                    Street = street,
                    UserId = user.Id,
                    StatusId = repo.GetStatuses().First(s => s.Tag == MarkerStatuses.Checking).Id,
                    //Photo = photoPath,
                    Logo = logoPath,
                    Personal = isPersonal
                };

                if (appLang != "ru")
                {
                    newMarker.NameEn = name;
                    newMarker.IntroductionEn = introduction;
                    newMarker.DescriptionEn = description;
                }

                if (isPersonal)
                    newMarker.StatusId = 1;

                repo.AddMarker(newMarker,
                    openTimes.Select(o => new WorkTimeDay {WeekDayId = o.Key, Time = TimeSpan.FromMinutes(o.Value)})
                        .ToList(),
                    closeTimes.Select(o => new WorkTimeDay {WeekDayId = o.Key, Time = TimeSpan.FromMinutes(o.Value)})
                        .ToList(), user.Id, subCategoryIds, phones);
                //Сохранение фотографий на сервере
                var tempPhotoList = new List<string>();
                var tempPhotoMiniList = new List<string>();
                foreach (var base64 in photoBase64)
                {
                    var tempPhoto = Convert.FromBase64String(base64);
                    tempPhotoList.Add(FileProvider.SaveMarkerPhoto(tempPhoto, false));
                    tempPhotoMiniList.Add(FileProvider.SaveMarkerPhoto(tempPhoto, true));
                }
                repo.AddMarkerPhotos(newMarker.Id, tempPhotoList.ToArray(), tempPhotoMiniList.ToArray());
                //repo.AddMarkerPhotos(newMarker.Id,
                //    photoBase64.Select(Convert.FromBase64String)
                //        .Select(b64 => FileProvider.SaveMarkerPhoto(b64, false))
                //        .ToArray(),
                //    photoBase64.Select(Convert.FromBase64String)
                //        .Select(b64 => FileProvider.SaveMarkerPhoto(b64, true))
                //        .ToArray());

            }
            catch (MyException e)
            {
                return JsonConvert.SerializeObject(new JsonResult(e.Error.Message));
            }
            catch (DbEntityValidationException e)
            {
                return
                    JsonConvert.SerializeObject(
                        new JsonResult(e.EntityValidationErrors.First().ValidationErrors.First().PropertyName + " " +
                                       e.EntityValidationErrors.First().ValidationErrors.First().ErrorMessage));
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new JsonResult(e.ToString()));
            }

            return JsonConvert.SerializeObject(new JsonResult(new List<Dictionary<string, object>>()));
        }
        
        [WebMethod]
        public string EditMarker(string userGuid, string name, string introduction, string description, int cityId,
            int baseCategoryId, double lat, double lng, string entryTicket, int discount, string street, string house,
            string building, string floor, string site, string email/*, string photoPath*/, string[] photoBase64, int[] subCategoryIds,
            string[] phones, List<KeyValue<int, int>> openTimes, List<KeyValue<int, int>> closeTimes, bool isPersonal, int markerId, string appLang)
        {
            try
            {
                if (string.IsNullOrEmpty(entryTicket))
                    entryTicket = "Нет";

                var repo = new MySqlRepository();
                repo.CheckUser(userGuid);
                var user = repo.GetUser(userGuid);
                var userType = repo.GetMobileUserTypeById(user.UserTypeId);
                var permittedUserTypes = new[] { UserTypes.Guide };

                //if (!isPersonal)
                //{
                //    if (!permittedUserTypes.Contains(userType.Tag))
                //        throw new MyException(Errors.NotPermitted);
                //    if (!repo.HavePermissions(user.Guid, cityId))
                //        throw new MyException(Errors.NotPermitted);
                //}
                if (!isPersonal)
                {
                    if (!permittedUserTypes.Contains(userType.Tag))
                        throw new MyException(Errors.NotPermitted);
                    if (!repo.HavePermissions(user.Guid, cityId))
                        throw new MyException(Errors.NotPermitted);
                    if (string.IsNullOrEmpty(house))
                    {
                        house = "Без дома";
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(street))
                    {
                        street = "Без улицы";
                    }
                    if (string.IsNullOrEmpty(house))
                    {
                        house = "Без дома";
                    }
                    //if (string.IsNullOrEmpty(building))
                    //{
                    //    building = "test";
                    //}
                    //if (string.IsNullOrEmpty(floor))
                    //{
                    //    floor = "test";
                    //}
                }

                var newMarker = repo.GetMarker(markerId);

                //string photoPathServer = "";
                //string logoPath = "";
                //if (string.IsNullOrEmpty(photoBase64) && !string.IsNullOrEmpty(photoPath))
                //{
                //    photoPathServer = newMarker.Photo;
                //}
                //else
                //{
                //    var bytes = Convert.FromBase64String(photoBase64);
                //    photoPathServer = FileProvider.SaveMarkerPhoto(bytes);
                //    logoPath = FileProvider.SaveMarkerLogo(bytes);
                //}

                if (photoBase64?.Length > 0)//новые фото
                {
                    repo.RemoveMarkerPhoto(newMarker.Id, photoBase64.Where(p64 => p64.Contains("http://185.76.145.214/")).Select(photo => photo.Replace("http://185.76.145.214/", "")).ToArray());
                    var tempPhotoList = new List<string>();
                    var tempPhotoMiniList = new List<string>();
                    foreach (var base64 in photoBase64.Where(p64=>!p64.Contains("http://185.76.145.214/")))
                    {
                        var tempPhoto = Convert.FromBase64String(base64);
                        tempPhotoList.Add(FileProvider.SaveMarkerPhoto(tempPhoto, false));
                        tempPhotoMiniList.Add(FileProvider.SaveMarkerPhoto(tempPhoto, true));
                    }
                    repo.AddMarkerPhotos(newMarker.Id, tempPhotoList.ToArray(), tempPhotoMiniList.ToArray());
                    //repo.AddMarkerPhotos(newMarker.Id, photoBase64.Where(photo=>!photo.Contains("http://185.76.145.214/")).Select(Convert.FromBase64String).Select(FileProvider.SaveMarkerPhoto).ToArray());
                    //repo.AddMarkerPhotos(newMarker.Id,
                    //    photoBase64.Where(photo => photo.Contains("http://185.76.145.214/"))
                    //        .Select(photo => photo.Replace("http://185.76.145.214/", ""))
                    //        .ToArray());
                    if (photoBase64.Any(photo => !photo.Contains("http://185.76.145.214/")))
                    {
                        newMarker.Logo =
                            FileProvider.SaveMarkerLogo(
                                Convert.FromBase64String(photoBase64.LastOrDefault(i => !i.Contains("http://185.76.145.214/"))));
                    }
                }
                else//нет новых фото
                {
                    
                }

                if (appLang != "ru")
                {
                    if (newMarker.NameEn != name)
                        newMarker.NameEn = name;
                    if (newMarker.IntroductionEn != introduction)
                        newMarker.IntroductionEn = introduction;
                    if (newMarker.DescriptionEn != description)
                        newMarker.DescriptionEn = description;
                }

                newMarker.Name = name;
                newMarker.Introduction = introduction;
                newMarker.Description = description;
                newMarker.CityId = cityId;
                newMarker.BaseCategoryId = baseCategoryId;
                newMarker.EntryTicket = entryTicket;
                newMarker.Buliding = building;
                newMarker.DiscountId = repo.GetDiscountId(discount);
                newMarker.Email = email;
                newMarker.Floor = floor;
                newMarker.House = house;
                newMarker.Lat = (float) lat;
                newMarker.Lng = (float) lng;
                newMarker.Site = site;
                newMarker.Street = street;
                newMarker.UserId = user.Id;
                newMarker.StatusId = repo.GetStatuses().First(s => s.Tag == MarkerStatuses.Checking).Id;
                //newMarker.Photo = photoPathServer;
                //newMarker.Logo = logoPath;
                newMarker.Personal = isPersonal;


                if (isPersonal)
                    newMarker.StatusId = 1;

                repo.UpdateMarker(newMarker,
                    openTimes.Select(o => new WorkTimeDay { WeekDayId = o.Key, Time = TimeSpan.FromMinutes(o.Value) })
                        .ToList(),
                    closeTimes.Select(o => new WorkTimeDay { WeekDayId = o.Key, Time = TimeSpan.FromMinutes(o.Value) })
                        .ToList(), user.Id, subCategoryIds, phones);
            }
            catch (MyException e)
            {
                return JsonConvert.SerializeObject(new JsonResult(e.Error.Message));
            }
            catch (DbEntityValidationException e)
            {
                return
                    JsonConvert.SerializeObject(
                        new JsonResult(e.EntityValidationErrors.First().ValidationErrors.First().PropertyName + " " +
                                       e.EntityValidationErrors.First().ValidationErrors.First().ErrorMessage));
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new JsonResult(e.ToString()));
            }

            return JsonConvert.SerializeObject(new JsonResult(new List<Dictionary<string, object>>()));
        }

        /// <summary>
        /// Метод возвращает список городов, на которые у указанного пользователя есть права
        /// </summary>
        /// <param name="userGuid"></param>
        /// <param name="isPersonal">Если пользователь собирается поставить персональную метку - ему доступны все города</param>
        /// <returns></returns>
        [WebMethod]
        public string GetPermittedCities(string userGuid, bool isPersonal)
        {
            try
            {
                var repo = new MySqlRepository();
                var result = new JsonResult(new List<Dictionary<string, object>>());


                var permittedCities = repo.GetPermittedCities(userGuid, isPersonal);

                var i = 0;
                foreach (var permittedCity in permittedCities)
                {
                    result.AddObjectToResult(new {permittedCity.PlaceId, permittedCity.Id, permittedCity.Name}, i);
                    i++;
                }

                return JsonConvert.SerializeObject(result);
            }
            catch (MyException e)
            {
                return JsonConvert.SerializeObject(new JsonResult(e.Error.Message));
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new JsonResult(e.ToString()));
            }

        }


        /// <summary>
        /// Метод возвращает список стран, на которые у указанного пользователя есть права
        /// </summary>
        /// <param name="userGuid"></param>
        /// <returns></returns>
        [WebMethod]
        public string GetPermittedCountry(string userGuid)
        {
            try
            {
                var repo = new MySqlRepository();
                var result = new JsonResult(new List<Dictionary<string, object>>());


                var permittedCountries = repo.GetPermittedCountries(userGuid);

                var i = 0;
                foreach (var permittedCountry in permittedCountries)
                {
                    result.AddObjectToResult(new { permittedCountry.PlaceId, permittedCountry.Id, permittedCountry.Name, permittedCountry.Code }, i);
                    i++;
                }

                return JsonConvert.SerializeObject(result);
            }
            catch (MyException e)
            {
                return JsonConvert.SerializeObject(new JsonResult(e.Error.Message));
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new JsonResult(e.ToString()));
            }
        }


        /// <summary>
        /// Метод регистрации нового жителя
        /// </summary>
        /// <param name="email"></param>
        /// <param name="firstName"></param>
        /// <param name="middleName"></param>
        /// <param name="lastName"></param>
        /// <param name="birthDate"></param>
        /// <param name="gender"></param>
        /// <param name="phone"></param>
        /// <param name="address"></param>
        /// <param name="appLang"></param>
        /// <returns></returns>
        [WebMethod]
        public string RegisterTenant(string email, string firstName, string middleName, string lastName,
            DateTime birthDate, string gender, string phone, string address, string appLang)
        {
            try
            {
                var repo = new MySqlRepository();
                repo.AddNewTenant(email, firstName, middleName, lastName, birthDate, gender, phone, address, appLang);
                return JsonConvert.SerializeObject(new JsonResult(new List<Dictionary<string, object>>()));
            }
            catch (MyException e)
            {
                return JsonConvert.SerializeObject(new JsonResult(e.Error.Message));
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new JsonResult(e.ToString()));
            }
        }

        /// <summary>
        /// Метод восстановления пароля. Новый пароль высылается на почту.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="appLang"></param>
        /// <returns></returns>
        [WebMethod]
        public string RecoverPassword(string email, string appLang)
        {
            try
            {
                var repo=new MySqlRepository();
                repo.RecoverPassword(email, appLang);
                return JsonConvert.SerializeObject(new JsonResult(new List<Dictionary<string, object>>()));
            }
            catch (MyException e)
            {
                return JsonConvert.SerializeObject(new JsonResult(e.Error.Message));
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new JsonResult(e.ToString()));
            }
        }

        /// <summary>
        /// Метод сохранения избранных статей и событий на сервере.
        /// </summary>
        /// <param name="userGuid"></param>
        /// <param name="articleEventId"></param>
        /// <returns></returns>
        [WebMethod]
        public string SaveFavoriteArticleAndEvent(string userGuid, int articleEventId)
        {
            try
            {
                var repo = new MySqlRepository();
                repo.SaveFavoriteArticleEvent(userGuid,articleEventId);
                return JsonConvert.SerializeObject(new JsonResult(new List<Dictionary<string, object>>()));
            }
            catch (MyException e)
            {
                return JsonConvert.SerializeObject(new JsonResult(e.Error.Message));
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new JsonResult(e.ToString()));
            }
        }

        /// <summary>
        /// Метод сохранения избранной метки на сервере.
        /// </summary>
        /// <param name="userGuid"></param>
        /// <param name="markerId"></param>
        /// <returns></returns>
        [WebMethod]
        public string SaveFavoriteMarker(string userGuid, int markerId)
        {
            try
            {
                var repo = new MySqlRepository();
                repo.SaveFavoriteMarker(userGuid, markerId);
                return JsonConvert.SerializeObject(new JsonResult(new List<Dictionary<string, object>>()));
            }
            catch (MyException e)
            {
                return JsonConvert.SerializeObject(new JsonResult(e.Error.Message));
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new JsonResult(e.ToString()));
            }
        }

        /// <summary>
        /// Метод удаления избранных статей и событий на сервере.
        /// </summary>
        /// <param name="userGuid"></param>
        /// <param name="articleEventId"></param>
        /// <returns></returns>
        [WebMethod]
        public string RemoveFavoriteArticleAndEvent(string userGuid, int articleEventId)
        {
            try
            {
                var repo = new MySqlRepository();
                repo.RemoveFavoriteArticleEvent(userGuid, articleEventId);
                return JsonConvert.SerializeObject(new JsonResult(new List<Dictionary<string, object>>()));
            }
            catch (MyException e)
            {
                return JsonConvert.SerializeObject(new JsonResult(e.Error.Message));
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new JsonResult(e.ToString()));
            }
        }

        /// <summary>
        /// Метод удаления избранной метки на сервере.
        /// </summary>
        /// <param name="userGuid"></param>
        /// <param name="markerId"></param>
        /// <returns></returns>
        [WebMethod]
        public string RemoveFavoriteMarker(string userGuid, int markerId)
        {
            try
            {
                var repo = new MySqlRepository();
                repo.RemoveFavoriteMarker(userGuid, markerId);
                return JsonConvert.SerializeObject(new JsonResult(new List<Dictionary<string, object>>()));
            }
            catch (MyException e)
            {
                return JsonConvert.SerializeObject(new JsonResult(e.Error.Message));
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new JsonResult(e.ToString()));
            }
        }

        /// <summary>
        /// Метод получения избранных статей и событий
        /// </summary>
        /// <param name="userGuid"></param>
        /// <param name="appLang"></param>
        /// <returns></returns>
        [WebMethod]
        public string GetFavoritsArticlAndEvent(string userGuid, string appLang)
        {
            var repo = new MySqlRepository();
            var articles = repo.GetArticles();
            articles.AddRange(repo.GetEvents());
            var result = new JsonResult(new List<Dictionary<string, object>>());
            var favoritsIdArticleAndEvent = repo.GetIdFavoritsArticleAndEvent(userGuid);
            var i = 0;

            var filteredArticles = articles.Where(a => favoritsIdArticleAndEvent.Any(fa => fa == a.Id)).ToList();

            foreach (var article in filteredArticles)
            {
                article.Photo = MapUrl(article.Photo);
                article.TitlePhoto = MapUrl(article.TitlePhoto);

                object authorName;

                switch (article.user.usertype.Tag)
                {
                    case UserTypes.Editor:
                        authorName = new
                        {
                            article.user.editor.First().FirstName,
                            article.user.editor.First().MiddleName,
                            article.user.editor.First().LastName
                        };
                        break;
                    case UserTypes.Journalist:
                        authorName = new
                        {
                            article.user.journalist.First().FirstName,
                            article.user.journalist.First().MiddleName,
                            article.user.journalist.First().LastName
                        };
                        break;
                    default:
                        authorName = new
                        {
                            FirstName = "Администратор",
                            MiddleName = "Администратор",
                            LastName = "Администратор"
                        };
                        break;
                }

                if (appLang != "ru")
                {
                    if (!string.IsNullOrEmpty(article.TitleEn))
                    {
                        article.Title = article.TitleEn;
                    }
                    if (!string.IsNullOrEmpty(article.DescriptionEn))
                    {
                        article.Description = article.DescriptionEn;
                    }
                    if (!string.IsNullOrEmpty(article.TextEn))
                    {
                        article.Text = article.TextEn;
                    }
                    if (!string.IsNullOrEmpty(article.SourcePhotoEn))
                    {
                        article.SourcePhoto = article.SourcePhotoEn;
                    }
                    if (!string.IsNullOrEmpty(article.SourceUrlEn))
                    {
                        article.SourceUrl = article.SourceUrlEn;
                    }
                }
                result.AddObjectToResult(article, i);
                string markerAddress = null;
                string markerAddressName = null;
                if (article.marker != null)
                {
                    markerAddress = article.marker.city.country.Name + ", " + article.marker.city.Name + ", " +
                                    article.marker.Street + " " + article.marker.House + " " + article.marker.Buliding;
                    markerAddressName = article.marker.Name;
                }

                result.AddObjectToResult(new { AuthorName = authorName, MarkerAddress = markerAddress, AddressName = markerAddressName }, i);
                result.AddObjectToResult(
                    new
                    {
                        Subcategories =
                            article.articlesubcategory.Select(
                                a =>
                                    (appLang != "ru" && !string.IsNullOrEmpty(a.category.EnName))
                                        ? a.category.EnName
                                        : a.category.Name).ToList()
                    }, i);

                i++;
            }
            return JsonConvert.SerializeObject(result);
        }

        /// <summary>
        /// Метод получения избранных меток
        /// </summary>
        /// <param name="userGuid"></param>
        /// <param name="appLang"></param>
        /// <returns></returns>
        [WebMethod]
        public string GetFavoritsMarker(string userGuid, string appLang)
        {
            var repo = new MySqlRepository();
            var markers = repo.GetFavoriteMarkers(userGuid).ToList();
            var result = new JsonResult(new List<Dictionary<string, object>>());
            var i = 0;

            foreach (var marker in markers)
            {
                var categories = repo.GetMarkerCategories();
                
                if (appLang != "ru")
                {
                    if (!string.IsNullOrEmpty(marker.NameEn))
                        marker.Name = marker.NameEn;
                    if (!string.IsNullOrEmpty(marker.DescriptionEn))
                        marker.Description = marker.DescriptionEn;
                    if (!string.IsNullOrEmpty(marker.IntroductionEn))
                        marker.Introduction = marker.IntroductionEn;
                }

                marker.Photo = MapUrl(marker.Photo);
                marker.Logo = MapUrl(marker.Logo);
                result.AddObjectToResult(marker, i);
                result.AddObjectToResult(new
                {
                    Phones = marker.phone.Select(p => new {p.Primary, p.Number}).ToList(),
                    Discount = marker.discount.Value,
                    Subcategories = marker.subcategory.Select(s => new
                    {
                        s.category.Id,
                        Name =
                            (appLang != "ru" && !string.IsNullOrEmpty(s.category.EnName))
                                ? s.category.EnName
                                : s.category.Name
                    }).ToList(),
                    CityName = marker.city.Name,
                    Pin = MapUrl(marker.category.Pin),
                    Icon = MapUrl(marker.category.Icon),
                    WorkTime = marker.worktime.Select(wt => new
                    {
                        wt.OpenTime,
                        wt.CloseTime,
                        wt.weekday.Id
                    }).ToList(),
                    repo.GetMarkerCategories().First(c => c.Id == GetCategoriesBranch(marker.category).Last()).Color,
                    CategoriesBranch =
                        GetCategoriesBranch(marker.category)
                            .Select(
                                number =>
                                    new
                                    {
                                        categories.First(c => c.Id == number).Id,
                                        Name =
                                            (appLang != "ru") &&
                                            !string.IsNullOrEmpty(categories.First(c => c.Id == number).EnName)
                                                ? categories.First(c => c.Id == number).EnName
                                                : categories.First(c => c.Id == number).Name
                                    })
                            .ToList()
                }, i);
                i++;
            }
            return JsonConvert.SerializeObject(result);
        }

        /// <summary>
        /// Метод получения событий по маркеру
        /// </summary>
        /// <param name="markerId">id маркера</param>
        /// <param name="nearest">ближайшие</param>
        /// <param name="appLang"></param>
        /// <returns></returns>
        [WebMethod]
        public string GetRelatedEventsFromMarker(int markerId, bool nearest ,string appLang)
        {
            var repo = new MySqlRepository();
            var articles = repo.GetEvents();
            var selectedMarker = repo.GetMarker(markerId);
            if (selectedMarker == null)
            {
                throw new MyException(Errors.NotFound);
            }
            var result = new JsonResult(new List<Dictionary<string, object>>());
            var i = 0;

            var filteredArticles = articles.Where(a => a.MarkerId == selectedMarker.Id).OrderBy(a=>a.StartDate).ToList();
            if (nearest)
            {
                filteredArticles = filteredArticles.Where(a => a.StartDate >= DateTime.Now).OrderBy(a => a.StartDate).Take(3).ToList();
            }
            foreach (var article in filteredArticles)
            {
                article.Photo = MapUrl(article.Photo);
                article.TitlePhoto = MapUrl(article.TitlePhoto);

                object authorName;

                switch (article.user.usertype.Tag)
                {
                    case UserTypes.Editor:
                        authorName = new
                        {
                            article.user.editor.First().FirstName,
                            article.user.editor.First().MiddleName,
                            article.user.editor.First().LastName
                        };
                        break;
                    case UserTypes.Journalist:
                        authorName = new
                        {
                            article.user.journalist.First().FirstName,
                            article.user.journalist.First().MiddleName,
                            article.user.journalist.First().LastName
                        };
                        break;
                    default:
                        authorName = new
                        {
                            FirstName = "Администратор",
                            MiddleName = "Администратор",
                            LastName = "Администратор"
                        };
                        break;
                }

                if (appLang != "ru")
                {
                    if (!string.IsNullOrEmpty(article.TitleEn))
                    {
                        article.Title = article.TitleEn;
                    }
                    if (!string.IsNullOrEmpty(article.DescriptionEn))
                    {
                        article.Description = article.DescriptionEn;
                    }
                    if (!string.IsNullOrEmpty(article.TextEn))
                    {
                        article.Text = article.TextEn;
                    }
                    if (!string.IsNullOrEmpty(article.SourceUrlEn))
                    {
                        article.SourceUrl = article.SourceUrlEn;
                    }
                    if (!string.IsNullOrEmpty(article.SourcePhotoEn))
                    {
                        article.SourcePhoto = article.SourcePhotoEn;
                    }
                }
                result.AddObjectToResult(article, i);
                string markerAddress = null;
                string markerAddressName = null;
                if (article.marker != null)
                {
                    markerAddress = article.marker.city.country.Name + ", " + article.marker.city.Name + ", " +
                                    article.marker.Street + " " + article.marker.House + " " + article.marker.Buliding;
                    markerAddressName = article.marker.Name;
                }

                result.AddObjectToResult(new { AuthorName = authorName, MarkerAddress = markerAddress, AddressName = markerAddressName }, i);
                result.AddObjectToResult(new
                {
                    Subcategories =
                        article.articlesubcategory.Select(
                            a => (appLang != "ru" && !string.IsNullOrEmpty(a.category.EnName))
                                ? a.category.EnName
                                : a.category.Name).ToList()
                }, i);

                i++;
            }
            return JsonConvert.SerializeObject(result);
        }
        #endregion


    }
}
