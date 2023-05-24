using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles =SD.Role_Admin)]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CategoryController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            List<Category> categoryList = _unitOfWork._categoryRepository.GetAll().ToList();
            return View(categoryList);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Category obj)
        {
            if (obj.Name == obj.DisplayOrder.ToString())
            {
                ModelState.AddModelError("name", "Name and diplayOrder not be same");
            }
            if (ModelState.IsValid)
            {
                _unitOfWork._categoryRepository.Add(obj);
                _unitOfWork.Save();
                TempData["success"] = "Category created Succesfully";
                return RedirectToAction("Index");
            }
            return View();

        }

        //[HttpGet]
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            Category? categoryFromDb = _unitOfWork._categoryRepository.Get(u => u.CategoryId == id); // it works only primary key
            //Category? categoryFromDb1 = _db.Categories.FirstOrDefault(u=>u.Id==id); // it works with any property
            //Category? categoryFromDb2 = _db.Categories.Where(u=>u.Id==id).FirstOrDefault();

            if (categoryFromDb == null)
            {
                return NotFound();
            }
            return View(categoryFromDb);

        }

        [HttpPost]
        public IActionResult Edit(Category obj)
        {

            if (ModelState.IsValid)
            {
                _unitOfWork._categoryRepository.update(obj);
                _unitOfWork.Save();
                TempData["success"] = "Category Updated Succesfully";
                return RedirectToAction("Index");
            }
            return View();

        }

        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            Category? categoryFromDb = _unitOfWork._categoryRepository.Get(u => u.CategoryId == id);

            if (categoryFromDb == null)
            {
                return NotFound();
            }
            return View(categoryFromDb);

        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeletePost(int? id)
        {
            Category? obj = _unitOfWork._categoryRepository.Get(u => u.CategoryId == id);
            if (obj == null)
            {
                return NotFound();
            }

            _unitOfWork._categoryRepository.Remove(obj);
            _unitOfWork.Save();
            TempData["success"] = "Category Deleted Succesfully";
            return RedirectToAction("Index");



        }
    }
}
