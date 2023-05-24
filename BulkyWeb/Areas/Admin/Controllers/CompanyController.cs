using BulkyBook.DataAccess.Data;
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
using System.Data;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
       
        public CompanyController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
           
        }
        public IActionResult Index()
        {
            List<Company> CompanyList = _unitOfWork._companyRepository.GetAll().ToList();
          

            return View(CompanyList);
        }

        public IActionResult Upsert(int? id)
        {
       
            if(id == null|| id==0) {
                return View(new Company());
            }
            else
            {
                Company companyObj = _unitOfWork._companyRepository.Get(u=>u.Id==id);
                return View(companyObj);
            }
            
        }

        [HttpPost]
        public IActionResult Upsert( Company company)
        {

            if (ModelState.IsValid)
            {
            
                if (company.Id == 0)
                {
                    _unitOfWork._companyRepository.Add(company);
                }
                else
                {
                    _unitOfWork._companyRepository.update(company);
                    TempData["success"] = "Company Updated Succesfully";
                }
              
                _unitOfWork.Save();
                TempData["success"] = "Company created Succesfully";
                return RedirectToAction("Index");
            }
            else
            {
              
                return View(company);
            }
            
        }
          

        

       

     


        

        #region
        [HttpGet]
        public IActionResult GetAll()
        {
            List<Company> objCompanyList = _unitOfWork._companyRepository.GetAll().ToList();
            return Json(new { data = objCompanyList });
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var companyToBeDeleted = _unitOfWork._companyRepository.Get(u=>u.Id==id);
           if(companyToBeDeleted == null)
            {
                return Json(new {success=false,Message="Error while deleting"});
            }
          
       
            _unitOfWork._companyRepository.Remove(companyToBeDeleted);
            _unitOfWork.Save();
           
            return Json(new { success = false, Message = "Deleted Successfully" });
        }
        #endregion
    }
}
