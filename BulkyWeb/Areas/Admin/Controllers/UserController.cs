using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.Models;
using BulkyBook.Models.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;
		private readonly UserManager<IdentityUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        public UserController(IUnitOfWork unitOfWork, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            
			_userManager= userManager;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;


        }
        public IActionResult Index()
        {
            return View();
        }

		public IActionResult RoleManagement(string userId)
		{
            
			RoleManagementVM RoleVM = new RoleManagementVM()
            {
                ApplicationUser= _unitOfWork._applicationUserRepository.Get(u=>u.Id==userId,includeProperties:"Company"),
                RoleList=_roleManager.Roles.Select(i=>new SelectListItem
                {
                    Text=i.Name,
                    Value=i.Name
                }),
				CompanyList = _unitOfWork._companyRepository.GetAll().Select(i => new SelectListItem
				{
					Text = i.Name,
					Value = i.Id.ToString()
				})
			};
            RoleVM.ApplicationUser.Role = _userManager.GetRolesAsync(_unitOfWork._applicationUserRepository.Get(u => u.Id == userId))
                .GetAwaiter().GetResult().FirstOrDefault();
			return View(RoleVM);
		}

		public IActionResult RoleManagement(RoleManagementVM roleManagementVM)
		{
         
            string oldRole = _userManager.GetRolesAsync(_unitOfWork._applicationUserRepository.Get(u => u.Id == roleManagementVM.ApplicationUser.Id))
                .GetAwaiter().GetResult().FirstOrDefault();

            ApplicationUser applicationUser = _unitOfWork._applicationUserRepository.Get(u => u.Id == roleManagementVM.ApplicationUser.Id);
            if (!(roleManagementVM.ApplicationUser.Role == oldRole))
            {
                // a role was updated
               

				if (roleManagementVM.ApplicationUser.Role==SD.Role_Company) {
                    applicationUser.CompanyId = roleManagementVM.ApplicationUser.CompanyId;
                }
                if (oldRole == SD.Role_Company)
                {
                    applicationUser.CompanyId = null;

                }
                _unitOfWork._applicationUserRepository.Update(applicationUser);
                _unitOfWork.Save();
                _userManager.RemoveFromRoleAsync(applicationUser,oldRole).GetAwaiter().GetResult();
				_userManager.AddToRoleAsync(applicationUser,roleManagementVM.ApplicationUser.Role).GetAwaiter().GetResult();
			}
            else
            {
                if(oldRole == SD.Role_Company && applicationUser.CompanyId!=roleManagementVM.ApplicationUser.CompanyId) {
                  applicationUser.CompanyId=roleManagementVM.ApplicationUser.CompanyId;
                    _unitOfWork._applicationUserRepository.Update(applicationUser);
                    _unitOfWork.Save();
                }
            }
            return RedirectToAction("Index");
		}



		#region
		[HttpGet]
        public IActionResult GetAll()
        {
            List<ApplicationUser> objUserList = _unitOfWork._applicationUserRepository.GetAll(includeProperties:"Company").ToList();
           

          foreach (var user in objUserList) {

                user.Role = _userManager.GetRolesAsync(user).GetAwaiter().GetResult().FirstOrDefault();


			if (user.Company==null)
                {
                    user.Company = new Company() { Name=""};
                }
            
            }
            
            
            return Json(new { data = objUserList });
        }

        [HttpDelete]
        public IActionResult LockUnlock([FromBody] string id)
        {
           var objFromDb=_unitOfWork._applicationUserRepository.Get(u=>u.Id== id);
            if (objFromDb==null)
            {
                return Json(new { success = false, Message = "Error while locking unlocking" });
                
            }
            if(objFromDb.LockoutEnd!=null && objFromDb.LockoutEnd > DateTime.Now)
            {
				//user is currently locked and we need to unlock them
				objFromDb.LockoutEnd=DateTime.Now;
			}
            else
            {
                objFromDb.LockoutEnd = DateTime.Now.AddYears(1000);

			}
            _unitOfWork._applicationUserRepository.Update(objFromDb);
            _unitOfWork.Save();

            return Json(new { success = false, Message = "Deleted Successfully" });
        }
        #endregion







       
    }
}
