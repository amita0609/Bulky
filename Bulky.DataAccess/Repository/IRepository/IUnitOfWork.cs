using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.Repository.IRepository
{
    public interface IUnitOfWork
    {
        ICategoryRepository _categoryRepository { get; }
        IProductRepository _productRepository { get; }
        ICompanyRepository _companyRepository { get; }
		IShoppingCartRepository _shoppingCartRepository { get; }
		IApplicationUserRepository _applicationUserRepository { get; }

		IOrderHeaderRepository _orderHeaderRepository { get; }
        IProductImageRepository _productImageRepository { get; }
		IOrderDetailRepository _orderDetailRepository { get; }
		void Save();
    }
}
