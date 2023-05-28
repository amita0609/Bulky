using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Migrations;
using BulkyBook.DataAccess.Repository;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.Models;
using BulkyBook.Models.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using Stripe;
using System.Data;
using Product = BulkyBook.Models.Product;

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
                productVM.Product=_unitOfWork._productRepository.Get(u=>u.Id==id,includeProperties:"ProductImages");
                return View(productVM);
            }
            
        }

        [HttpPost]
        public IActionResult Upsert( ProductVM productVM,List<IFormFile> files)
        {

            if (ModelState.IsValid)
            {
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
				string wwwRootPath = _webHostEnvironment.WebRootPath;
                if (files != null)
                {
                    foreach (var file in files)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string productPath = @"images\products\product-" + productVM.Product.Id;
                        string finalPath = Path.Combine(wwwRootPath, productPath);
                        if (!Directory.Exists(finalPath))
                        {
                            Directory.CreateDirectory(finalPath);
                        }
                        using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                        {
                            file.CopyTo(fileStream);
                        }

                        ProductImage productImage = new()
                        {
                            ImageUrl = @"\" + productPath + @"\" + fileName,
                            ProductId = productVM.Product.Id,
                        };

                       if (productVM.Product.ProductImages == null)
                        {
							productVM.Product.ProductImages=new List<ProductImage>();

						}
						productVM.Product.ProductImages.Add(productImage);

					}
                    _unitOfWork._productRepository.update(productVM.Product);
                    _unitOfWork.Save();
                }
              
              
               
                TempData["success"] = "Product created/updated Succesfully";
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
          

        public IActionResult DeleteImage(int ImageId)
        {
            var imageToBeDeleted = _unitOfWork._productImageRepository.Get(u => u.Id == ImageId);
          int productId= imageToBeDeleted.ProductId;
            if(imageToBeDeleted != null)
            {
                if (!string.IsNullOrEmpty(imageToBeDeleted.ImageUrl))
                {
                    var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, imageToBeDeleted.ImageUrl.TrimStart('\\'));

                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }
                _unitOfWork._productImageRepository.Remove(imageToBeDeleted);
                _unitOfWork.Save();

                TempData["success"] = "Deleted succesfully";

            }
            return RedirectToAction(nameof(Upsert), new {id=productId});
        }

       

     


        

        #region
        [HttpGet]
        public IActionResult GetAll()
        {
            List<BulkyBook.Models.Product> objProductList = _unitOfWork._productRepository.GetAll(includeProperties: "Category").ToList();
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

           

        
            string productPath = @"images\products\product-" + id;
            string finalPath = Path.Combine(_webHostEnvironment.WebRootPath, productPath);
            if (!Directory.Exists(finalPath))
            {
                string[] filePaths=Directory.GetFiles(finalPath);
                foreach (string filePath in filePaths)
                {
                    System.IO.File.Delete(filePath);
                }
                Directory.Delete(finalPath);
            }

            _unitOfWork._productRepository.Remove(productToBeDeleted);
            _unitOfWork.Save();
            
            return Json(new { success=true,Message="Delete Successful" });
        }
        #endregion
    }
}
