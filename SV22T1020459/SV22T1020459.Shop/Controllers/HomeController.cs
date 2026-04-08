using Microsoft.AspNetCore.Mvc;
using SV22T1020459.Shop.Models;
using System.Diagnostics;
using SV22T1020459.BusinessLayers;
using SV22T1020459.Models.Common;
using SV22T1020459.Models.Catalog;

namespace SV22T1020459.Shop.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var categorySearch = new PaginationSearchInput { Page = 1, PageSize = 5, SearchValue = "" };
            var categoriesResult = await CatalogDataService.ListCategoriesAsync(categorySearch);

            var categoryProducts = new Dictionary<Category, List<Product>>();

            if (categoriesResult != null && categoriesResult.DataItems != null)
            {
                foreach (var category in categoriesResult.DataItems)
                {
                    var productSearch = new ProductSearchInput
                    {
                        Page = 1,
                        PageSize = 4,
                        SearchValue = "",
                        CategoryID = category.CategoryID,
                        SupplierID = 0,
                        MinPrice = 0,
                        MaxPrice = 0
                    };

                    var productsResult = await CatalogDataService.ListProductsAsync(productSearch);

                    if (productsResult != null && productsResult.DataItems != null && productsResult.DataItems.Any())
                    {
                        categoryProducts.Add(category, productsResult.DataItems.ToList());
                    }
                }
            }

            ViewBag.CategoryProducts = categoryProducts;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}