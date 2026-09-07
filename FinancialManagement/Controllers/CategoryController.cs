using Microsoft.AspNetCore.Mvc;

namespace FinancialManagement.Controllers
{
    public class CategoryController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
