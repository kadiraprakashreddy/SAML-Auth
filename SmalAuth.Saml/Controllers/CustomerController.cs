using System.Web.Mvc;

namespace SmalAuth.Saml.Controllers
{
    [SamlAuthorize]
    public class CustomerController : Controller
    {
        public ActionResult Edit(int id)
        {
            ViewBag.CustomerId = id;
            return View();
        }
    }
}
