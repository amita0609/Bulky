using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProductController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            List<Product> ProductList = _unitOfWork._productRepository.GetAll().ToList();
          

            return View(ProductList);
        }

        public IActionResult Upsert(int? id)
        {
           
           // ViewData["CategoryList"] = CategoryList;
            ProductVM productVM = new ProductVM()
            {
                CategoryList = _unitOfWork._categoryRepository.GetAll().
                 Select(u => new SelectListItem
                 {
                     Text = u.Name,
                     Value = u.CategoryId.ToString(),
                 }),
            Product = new Product()
            };
            if(id == null|| id==0) {
                return View(productVM);
            }
            else
            {
                productVM.Product=_unitOfWork._productRepository.Get(u=>u.Id==id);
                return View(productVM);
            }
            
        }

        [HttpPost]
        public IActionResult Upsert( ProductVM productVM,IFormFile? file)
        {

            if (ModelState.IsValid)
            {
                _unitOfWork._productRepository.Add(productVM.Product);
                _unitOfWork.Save();
                TempData["success"] = "Product created Succesfully";
                return RedirectToAction("Index");
            }
            else
            {
                productVM.CategoryList = _unitOfWork._categoryRepository.GetAll().
                 Select(u => new SelectListItem
                 {
                     Text = u.Name,
                     Value = u.CategoryId.ToString(),
                 });
                return View(productVM);
            }
            
        }
          

        

       

        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            Product? ProductFromDb = _unitOfWork._productRepository.Get(u => u.Id == id);

            if (ProductFromDb == null)
            {
                return NotFound();
            }
            return View(ProductFromDb);

        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeletePost(int? id)
        {
            Product? obj = _unitOfWork._productRepository.Get(u => u.Id == id);
            if (obj == null)
            {
                return NotFound();
            }

            _unitOfWork._productRepository.Remove(obj);
            _unitOfWork.Save();
            TempData["success"] = "Product Deleted Succesfully";
            return RedirectToAction("Index");



        }
    }
}
