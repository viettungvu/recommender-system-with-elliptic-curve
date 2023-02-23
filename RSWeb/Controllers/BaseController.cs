using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using System;
using Microsoft.AspNetCore.Mvc;

namespace RSWeb.Controllers
{
    public class BaseController : Controller
    {
       
        [HttpPost]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

            return LocalRedirect(returnUrl);
        }
    }
}
