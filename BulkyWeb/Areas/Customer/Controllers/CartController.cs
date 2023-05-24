using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models.Models;
using BulkyBook.Models;
using BulkyBook.Models.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Stripe.BillingPortal;
using Stripe.Checkout;

namespace BulkyBookWeb.Areas.Customer.Controllers
{
   
    [Area("Customer")]
	[Authorize]
	public class CartController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IEmailSender _emailSender;
		[BindProperty]
		public ShoppingCartVM ShoppingCartVM { get; set; }
		public CartController(IUnitOfWork unitOfWork, IEmailSender emailSender)
		{
			_unitOfWork = unitOfWork;
			_emailSender = emailSender;
		}
		public IActionResult Index()
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

			ShoppingCartVM = new ShoppingCartVM()
			{
		ShoppingCartList = _unitOfWork._shoppingCartRepository.GetAll(u => u.ApplicationUserId == claim.Value,
				includeProperties: "Product"),
				OrderHeader = new()
			};
			foreach (var cart in ShoppingCartVM.ShoppingCartList)
			{
				cart.Price = GetPriceBasedOnQuantity(cart.Count, cart.Product.Price,
					cart.Product.Price50, cart.Product.Price100);
				ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
			}
			return View(ShoppingCartVM);
		}

		public IActionResult Summary()
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

			ShoppingCartVM = new ShoppingCartVM()
			{
				ShoppingCartList = _unitOfWork._shoppingCartRepository.GetAll(u => u.ApplicationUserId == claim.Value,
				includeProperties: "Product"),
				OrderHeader = new()
			};
			ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork._applicationUserRepository.Get(
				u => u.Id == claim.Value);

			ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
			ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
			ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
			ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
			ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;
			ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;



			foreach (var cart in ShoppingCartVM.ShoppingCartList)
			{
				cart.Price = GetPriceBasedOnQuantity(cart.Count, cart.Product.Price,
					cart.Product.Price50, cart.Product.Price100);
				ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
			}
			return View(ShoppingCartVM);
		}

		[HttpPost]
		[ActionName("Summary")]
		[ValidateAntiForgeryToken]
		public IActionResult SummaryPOST()
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

			ShoppingCartVM.ShoppingCartList = _unitOfWork._shoppingCartRepository.GetAll(u => u.ApplicationUserId == claim.Value,
				includeProperties: "Product");


			ShoppingCartVM.OrderHeader.OrderDate = System.DateTime.Now;
			ShoppingCartVM.OrderHeader.ApplicationUserId = claim.Value;


			foreach (var cart in ShoppingCartVM.ShoppingCartList)
			{
				cart.Price = GetPriceBasedOnQuantity(cart.Count, cart.Product.Price,
					cart.Product.Price50, cart.Product.Price100);
				ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
			}
			ApplicationUser applicationUser = _unitOfWork._applicationUserRepository.Get(u => u.Id == claim.Value);

			if (applicationUser.CompanyId.GetValueOrDefault() == 0)
			{
				ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
				ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
			}
			else
			{
				ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
				ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
			}

			_unitOfWork._orderHeaderRepository.Add(ShoppingCartVM.OrderHeader);
			_unitOfWork.Save();
			foreach (var cart in ShoppingCartVM.ShoppingCartList)
			{
				OrderDetail orderDetail = new()
				{
					ProductId = cart.ProductId,
					OrderId = ShoppingCartVM.OrderHeader.Id,
					Price = cart.Price,
					Count = cart.Count
				};
				_unitOfWork._orderDetailRepository.Add(orderDetail);
				_unitOfWork.Save();
			}


			if (applicationUser.CompanyId.GetValueOrDefault() == 0)
			{
				//stripe settings 
				var domain = "https://localhost:44300/";
				//var options = new SessionCreateOptions
				//{
				//	PaymentMethodTypes = new List<string>
				//{
				//  "card",
				//},
				//	LineItems = new List<SessionLineItemOptions>(),
				//	Mode = "payment",
				//	SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={ShoppingCartVM.OrderHeader.Id}",
				//	CancelUrl = domain + $"customer/cart/index",
				//};

				foreach (var item in ShoppingCartVM.ShoppingCartList)
				{

					var sessionLineItem = new SessionLineItemOptions
					{
						PriceData = new SessionLineItemPriceDataOptions
						{
							UnitAmount = (long)(item.Price * 100),//20.00 -> 2000
							Currency = "usd",
							ProductData = new SessionLineItemPriceDataProductDataOptions
							{
								Name = item.Product.Title
							},

						},
						Quantity = item.Count,
					};
					//options.LineItems.Add(sessionLineItem);

				}

				//var service = new SessionService();
				//Session session = service.Create(options);
			//	_unitOfWork._orderHeaderRepository.UpdateStripePaymentID(ShoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
				_unitOfWork.Save();
			//	Response.Headers.Add("Location", session.Url);
				return new StatusCodeResult(303);
			}

			else
			{
				return RedirectToAction("OrderConfirmation", "Cart", new { id = ShoppingCartVM.OrderHeader.Id });
			}
		}

		public IActionResult OrderConfirmation(int id)
		{
			OrderHeader orderHeader = _unitOfWork._orderHeaderRepository.Get(u => u.Id == id, includeProperties: "ApplicationUser");
			if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
			{
				//var service = new SessionService();
				//Session session = service.Get(orderHeader.SessionId);
				//check the stripe status
				//if (session.PaymentStatus.ToLower() == "paid")
				//{
				//	_unitOfWork._orderHeaderRepository.UpdateStripePaymentID(id, orderHeader.SessionId, session.PaymentIntentId);
				//	_unitOfWork._orderHeaderRepository.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
				//	_unitOfWork.Save();
				//}
			}
			_emailSender.SendEmailAsync(orderHeader.ApplicationUser.Email, "New Order - Bulky Book", "<p>New Order Created</p>");
			List<ShoppingCart> shoppingCarts = _unitOfWork._shoppingCartRepository.GetAll(u => u.ApplicationUserId ==
			orderHeader.ApplicationUserId).ToList();
			HttpContext.Session.Clear();
			_unitOfWork._shoppingCartRepository.RemoveRange(shoppingCarts);
			_unitOfWork.Save();
			return View(id);
		}



		public IActionResult Plus(int cartId)
		{
			var cart = _unitOfWork._shoppingCartRepository.Get(u => u.Id == cartId);
			//_unitOfWork._shoppingCartRepository.IncrementCount(cart, 1);
			_unitOfWork.Save();
			return RedirectToAction(nameof(Index));
		}

		public IActionResult Minus(int cartId)
		{
			var cart = _unitOfWork._shoppingCartRepository.Get(u => u.Id == cartId,tracked:true);
			if (cart.Count <= 1)
			{
				_unitOfWork._shoppingCartRepository.Remove(cart);
				var count = _unitOfWork._shoppingCartRepository.GetAll(u => u.ApplicationUserId == cart.ApplicationUserId).Count() - 1;
				HttpContext.Session.SetInt32(SD.SessionCart, count);
			}
			else
			{
				cart.Count -= 1;
				_unitOfWork._shoppingCartRepository.update(cart);
			}
			_unitOfWork.Save();
			return RedirectToAction(nameof(Index));
		}

		public IActionResult Remove(int cartId)
		{
			var cart = _unitOfWork._shoppingCartRepository.Get(u => u.Id == cartId,tracked:true);
			_unitOfWork._shoppingCartRepository.Remove(cart);
			_unitOfWork.Save();
			var count = _unitOfWork._shoppingCartRepository.GetAll(u => u.ApplicationUserId == cart.ApplicationUserId).Count()-1;
			HttpContext.Session.SetInt32(SD.SessionCart, count);
			return RedirectToAction(nameof(Index));
		}





		private double GetPriceBasedOnQuantity(double quantity, double price, double price50, double price100)
		{
			if (quantity <= 50)
			{
				return price;
			}
			else
			{
				if (quantity <= 100)
				{
					return price50;
				}
				return price100;
			}
		}
	}
}
