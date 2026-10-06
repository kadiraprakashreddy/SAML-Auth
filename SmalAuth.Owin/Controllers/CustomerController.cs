using System.Web.Mvc;

namespace SmalAuth.Owin.Controllers
{
    public class CustomerController : Controller
    {
        public ActionResult Edit(int id)
        {
            ViewBag.CustomerId = id;
            return View();
        }
    }
}
