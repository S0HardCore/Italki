using System.Web.Mvc;
using WebApplication1.Models;
using WebApplication1.Repository;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        MyRepository repository;
        public HomeController()
        {
            repository = new MyRepository();
        }

        [HttpPost]
        public ActionResult Main(int count)
        {
            repository.RefreshDB(count);
            return View();
        }

        [HttpGet]
        public JsonResult Get(TeachersPagingArguments tpa)
        {
            var teachers = repository.GetTeachers(tpa.pageIndex, tpa.pageSize, tpa.sortField, tpa.sortOrder);
            return Json(teachers, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Teachers(int id)
        {
            return View(repository.GetTeacher(id));
        }

        public ActionResult Main()
        {
            return View("Main");
        }
    }
}