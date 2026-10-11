using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return Content("Admin Portal - Restricted Area");
        }

        public IActionResult Staff()
        {
            return Content("Staff Accounts Management - Only Admin can access this page");
        }
    }
}
