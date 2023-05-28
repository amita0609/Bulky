
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models.Models.ViewModels;
using BulkyBook.Models.Models;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using System.Drawing.Text;
using System.Security.Claims;

namespace BulkyWeb.Areas.Customer.Controllers
{
	[Area("Customer")]
	[Authorize]
	public class CartController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
		[BindProperty]
		public ShoppingCartVM ShoppingCartVM { get; set; }
		public CartController(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}
		public IActionResult Index()
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
			ShoppingCartVM = new()
			{
				ShoppingCartList = _unitOfWork._shoppingCartRepository.GetAll(u => u.ApplicationUserId == userId,
				includeProperties: "Product"),
				OrderHeader = new()
			};

			IEnumerable<ProductImage> productImages = _unitOfWork._productImageRepository.GetAll();

			foreach (var cart in ShoppingCartVM.ShoppingCartList)
			{
				cart.Product.ProductImages=productImages.Where(u=>u.ProductId==cart.Product.Id).ToList();	
				cart.Price = GetPriceBasedOnQuantity(cart);
				ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
			}
			return View(ShoppingCartVM);
		}
		public IActionResult Summary()
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
			ShoppingCartVM = new()
			{
				ShoppingCartList = _unitOfWork._shoppingCartRepository.GetAll(u => u.ApplicationUserId == userId,
				includeProperties: "Product"),
				OrderHeader = new()
			};
			foreach (var cart in ShoppingCartVM.ShoppingCartList)
			{
				cart.Price = GetPriceBasedOnQuantity(cart);
				ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
			}
			ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork._applicationUserRepository.Get(u => u.Id == userId);

			ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
			ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
			ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
			ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
			ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;
			ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;

			return View(ShoppingCartVM);
		}
		[HttpPost]
		[ActionName("Summary")]
		public IActionResult SummaryPOST()
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
			ShoppingCartVM.ShoppingCartList = _unitOfWork._shoppingCartRepository.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product");
			ShoppingCartVM.OrderHeader.OrderDate = System.DateTime.Now;
			ShoppingCartVM.OrderHeader.ApplicationUserId = userId;

			ApplicationUser applicationUser = _unitOfWork._applicationUserRepository.Get(u => u.Id == userId);
			foreach (var cart in ShoppingCartVM.ShoppingCartList)
			{
				cart.Price = GetPriceBasedOnQuantity(cart);
				ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
			}
			if (applicationUser.CompanyId.GetValueOrDefault() == 0)
			{
				//it is a regular customer account
				ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
				ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
			}
			else
			{
				//it is a comapany user
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
				//it is a regular customer account and we need to capture payment
				//stripe logic
				var domain = "https://localhost:7240/";
				var options = new SessionCreateOptions
				{
					SuccessUrl = domain + $"Customer/Cart/OrderConfirmation?id={ShoppingCartVM.OrderHeader.Id}",
					CancelUrl = domain + "Customer/Cart/Index",
					LineItems = new List<SessionLineItemOptions>(),

					Mode = "payment",
				};
				foreach (var items in ShoppingCartVM.ShoppingCartList)
				{
					var sessionLineItem = new SessionLineItemOptions
					{
						PriceData = new SessionLineItemPriceDataOptions
						{
							UnitAmount = (long)(items.Price * 100), // $20.50= 2050
							Currency = "usd",
							ProductData = new SessionLineItemPriceDataProductDataOptions
							{
								Name = items.Product.Title
							}

						},
						Quantity = items.Count
					};
					options.LineItems.Add(sessionLineItem);
				}
				var service = new SessionService();
				Session session = service.Create(options);
				_unitOfWork._orderHeaderRepository.UpdateStripePaymentID(ShoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
				_unitOfWork.Save();
				Response.Headers.Add("Location", session.Url);
				return new StatusCodeResult(303);

			}
			return RedirectToAction(nameof(OrderConfirmation), new { id = ShoppingCartVM.OrderHeader.Id });
		}

		public IActionResult OrderConfirmation(int id)
		{

			OrderHeader orderHeader = _unitOfWork._orderHeaderRepository.Get(u => u.Id == id, includeProperties: "ApplicationUser");
			if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
			{
				//this is order by customer
				var service = new SessionService();
				Session session = service.Get(orderHeader.SessionId);

				if (session.PaymentStatus.ToLower() == "paid")
				{
					_unitOfWork._orderHeaderRepository.UpdateStripePaymentID(id, session.Id, session.PaymentIntentId);
					_unitOfWork._orderHeaderRepository.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
					_unitOfWork.Save();
				}
				HttpContext.Session.Clear();
			}
			List<ShoppingCart> shoppingCarts = _unitOfWork._shoppingCartRepository.GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();
			_unitOfWork._shoppingCartRepository.RemoveRange(shoppingCarts);
			_unitOfWork.Save();
			return View(id);
		}

		public IActionResult Plus(int cartId)
		{
			var cartFromDb = _unitOfWork._shoppingCartRepository.Get(u => u.Id == cartId);
			cartFromDb.Count += 1;
			_unitOfWork._shoppingCartRepository.update(cartFromDb);
			_unitOfWork.Save();
			return RedirectToAction(nameof(Index));

		}
		public IActionResult Minus(int cartId)
		{
			var cartFromDb = _unitOfWork._shoppingCartRepository.Get(u => u.Id == cartId, tracked: true);
			if (cartFromDb.Count <= 1)
			{
				//remove that from cart
				HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork._shoppingCartRepository
					.GetAll(u => u.ApplicationUserId == cartFromDb.ApplicationUserId).Count() - 1);
				_unitOfWork._shoppingCartRepository.Remove(cartFromDb);
			}
			else
			{
				cartFromDb.Count -= 1;
				_unitOfWork._shoppingCartRepository.update(cartFromDb);

			}
			_unitOfWork.Save();
			return RedirectToAction(nameof(Index));

		}

		public IActionResult Remove(int cartId)
		{
			var cartFromDb = _unitOfWork._shoppingCartRepository.Get(u => u.Id == cartId, tracked: true);
			HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork._shoppingCartRepository
				.GetAll(u => u.ApplicationUserId == cartFromDb.ApplicationUserId).Count() - 1);


			_unitOfWork._shoppingCartRepository.Remove(cartFromDb);




			_unitOfWork.Save();
			return RedirectToAction(nameof(Index));

		}
		private double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
		{
			if (shoppingCart.Count <= 50)
			{
				return shoppingCart.Product.Price;
			}
			else
			{
				if (shoppingCart.Count <= 100)
				{
					return shoppingCart.Product.Price50;
				}
				else
				{
					return shoppingCart.Product.Price100;
				}
			}
		}
	}
}