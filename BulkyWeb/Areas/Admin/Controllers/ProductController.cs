using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using System.Data;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }
        public IActionResult Index()
        {
            List<Product> ProductList = _unitOfWork._productRepository.GetAll(includeProperties:"Category").ToList();
          

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
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                if (file != null)
                {
                    string fileName=Guid.NewGuid().ToString()+Path.GetExtension(file.FileName);  
                    string productPath=Path.Combine(wwwRootPath,@"images\product");
                    if(!string.IsNullOrEmpty(productVM.Product.ImageUrl)) {
                        //delete old imaage
                        var OldImagePath = Path.Combine(wwwRootPath,productVM.Product.ImageUrl.TrimStart('\\'));
                  if(System.IO.File.Exists(OldImagePath))
                        {
                            System.IO.File.Delete(OldImagePath);
                        }
                    }
                    using (var fileStream = new FileStream(Path.Combine(productPath, fileName),FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }
                    productVM.Product.ImageUrl = @"\images\product\" + fileName;
                }
                if (productVM.Product.Id == 0)
                {
                    _unitOfWork._productRepository.Add(productVM.Product);
                }
                else
                {
                    _unitOfWork._productRepository.update(productVM.Product);
                    TempData["success"] = "Product Updated Succesfully";
                }
              
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
          

        

       

     


        

        #region
        [HttpGet]
        public IActionResult GetAll()
        {
            List<Product> objProductList = _unitOfWork._productRepository.GetAll(includeProperties: "Category").ToList();
            return Json(new { data = objProductList });
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var productToBeDeleted = _unitOfWork._productRepository.Get(u=>u.Id==id);
           if(productToBeDeleted == null)
            {
                return Json(new {success=false,Message="Error while deleting"});
            }
          
        var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, productToBeDeleted.ImageUrl.TrimStart('\\'));
                 
            if(System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
            _unitOfWork._productRepository.Remove(productToBeDeleted);
            _unitOfWork.Save();
                List<Product> objProductList = _unitOfWork._productRepository.GetAll(includeProperties: "Category").ToList();
            return Json(new { data = objProductList });
        }
        #endregion
    }
}
