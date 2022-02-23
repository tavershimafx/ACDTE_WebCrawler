using AngleSharp;
using AngleSharp.Dom;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACDTE_WebCrawler.Models;

namespace ACDTE_WebCrawler
{
    public class AutoPart100Crawler
    {
        private readonly string baseDir = "Autopart100";
        private readonly string baseUrl = "http://www.en.autopart100.cn";
        private readonly ILogger<AutoPart100Crawler> _logger;

        public AutoPart100Crawler(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<AutoPart100Crawler>();
        }

        public AutoPart100Crawler()
        {
            
        }

        public async Task<string> ExtractCategoryUrls()
        {
            Console.WriteLine();
            var config = Configuration.Default.WithDefaultLoader();
            var context = BrowsingContext.New(config);
            Console.ForegroundColor = ConsoleColor.White; Console.Write($"INFO: Fetching homepage from ");
            Console.ForegroundColor = ConsoleColor.DarkGreen;Console.WriteLine(baseUrl);
            
            var document = await context.OpenAsync(baseUrl);
            Console.ForegroundColor = ConsoleColor.White; Console.WriteLine($"Done fetching homepage");


            Console.ForegroundColor = ConsoleColor.White; Console.WriteLine($"INFO: Retrieving categories from result...");
            var ulSelector = "div.w-hd.bgw div.nav-categorys ul.cate-item";
            var lis = document.QuerySelectorAll(ulSelector);

            var ch = lis.Children("li");
            List<Category> categories = new();
            foreach (var li in ch)
            {
                Category category = new();
                var anchor = li.Children.First().Children.First(); // this is the a element
                category.Name = anchor.TextContent.Trim();
                category.Url = $"{baseUrl}{anchor.GetAttribute("href")}";

                string parentCatSelector = "dl dd div";
                var childCats = li.QuerySelectorAll(parentCatSelector);
                foreach (var fli in childCats.Children("dl")) // a list of dl
                {
                    Category firstChild = new();
                    var ac = fli.Children.First().Children.First(); // this is the a element
                    firstChild.Name = ac.TextContent.Trim();
                    firstChild.Url = $"{baseUrl}{ac.GetAttribute("href")}";

                    string ssSelector = "dd";
                    var secondChildren = fli.QuerySelectorAll(ssSelector);
                    foreach (var sli in secondChildren.Children("em")) // a list of em
                    {
                        Category secondChild = new();
                        secondChild.Name = sli.Children.First().TextContent.Trim();
                        secondChild.Url = $"{baseUrl}{sli.Children.First().GetAttribute("href")}";
                        firstChild.Children.Add(secondChild);
                    }

                    category.Children.Add(firstChild);
                }

                categories.Add(category);
            }

            byte[] buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(categories));
            await SaveFile(new MemoryStream(buffer), "categories.json");
            Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine("Done retrieving urls...");
            return string.Empty;
        }

        private async Task SaveFile(Stream stream, string filename)
        {
            Directory.CreateDirectory(baseDir);
            using (var output = new FileStream($"{baseDir}/{filename }", FileMode.Create))
            {
                await stream.CopyToAsync(output);
            }
        }

        public async Task GetNumberOfPagesForCategory()
        {
            string path = baseDir + Path.DirectorySeparatorChar + "categories.json";
            string content = "";
            using (var reader = new StreamReader(path))
            {
                content = reader.ReadToEnd();
            }

            List<Category> categories = JsonConvert.DeserializeObject<List<Category>>(content);
            foreach (var category in categories)
            {
                int num = GetNumberOfPages(category.Url);
                category.Pages = num;

                if (category.Children.Any())
                {
                    foreach (var first in category.Children)
                    {
                        int n = GetNumberOfPages(first.Url);
                        first.Pages = n;
                        if (first.Children.Any())
                        {
                            foreach (var second in first.Children)
                            {
                                int na = GetNumberOfPages(second.Url);
                                second.Pages = na;
                                if (second.Children.Any())
                                {
                                    foreach (var third in second.Children)
                                    {
                                        int nb = GetNumberOfPages(third.Url);
                                        third.Pages = nb;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            byte[] buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(categories));
            await SaveFile(new MemoryStream(buffer), "categories.json");
            Console.WriteLine("Done retrieving number of pages for all categories...");
        }

        public async Task GetProducts()
        {
            string path = baseDir + Path.DirectorySeparatorChar + "categories.json";
            string content = "";
            using (var reader = new StreamReader(path))
            {
                content = reader.ReadToEnd();
            }

            List<Product> products;
            List<Category> categories = JsonConvert.DeserializeObject<List<Category>>(content);
            
            foreach (var category in categories)
            {
                products = new();
                Console.ForegroundColor = ConsoleColor.White; Console.WriteLine($"Starting {category.Name}..");
                for (int a = 0; a < (category.Pages == 0 ? 1 : category.Pages); a++)
                {
                    string c1 = $"{category.Url.Replace(".html", string.Empty)}_p{a+1}.html";
                    var p1 = await GetProductsFromPage(c1, category.Name);
                    products.AddRange(p1);
                }

                byte[] b1 = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(products));
                await SaveFile(new MemoryStream(b1), $"{category.Name}.dat");
                Console.ForegroundColor = ConsoleColor.Yellow; Console.WriteLine($"Done retrieving products for {category.Name}");
                if (category.Children.Any())
                {
                    products = new();
                    foreach (var first in category.Children)
                    {
                        Console.ForegroundColor = ConsoleColor.White; Console.WriteLine($"Starting {first.Name}..");
                        for (int b = 0; b < (first.Pages == 0? 1: first.Pages); b++)
                        {
                            string c2 = $"{first.Url.Replace(".html", string.Empty)}_p{b + 1}.html";
                            var p2 = await GetProductsFromPage(c2, first.Name);
                            products.AddRange(p2);
                        }

                        byte[] b2 = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(products));
                        await SaveFile(new MemoryStream(b2), $"{first.Name}.dat");
                        Console.ForegroundColor = ConsoleColor.Yellow; Console.WriteLine($"Done retrieving products for {first.Name}");
                        if (first.Children.Any())
                        {
                            products = new();
                            foreach (var second in first.Children)
                            {
                                Console.ForegroundColor = ConsoleColor.White; Console.WriteLine($"Starting {second.Name}..");
                                for (int c = 0; c < (second.Pages == 0 ? 1 : second.Pages); c++)
                                {
                                    string c3 = $"{second.Url.Replace(".html", string.Empty)}_p{c + 1}.html";
                                    var p3 = await GetProductsFromPage(c3, second.Name);
                                    products.AddRange(p3);
                                }

                                byte[] b3 = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(products));
                                await SaveFile(new MemoryStream(b3), $"{first.Name}.dat");
                                Console.ForegroundColor = ConsoleColor.Yellow; Console.WriteLine($"Done retrieving products for {first.Name}");
                                if (second.Children.Any())
                                {
                                    products = new();
                                    foreach (var third in second.Children)
                                    {
                                        Console.ForegroundColor = ConsoleColor.White; Console.WriteLine($"Starting {third.Name}..");
                                        for (int d = 0; d < (third.Pages == 0 ? 1 : third.Pages); d++)
                                        {
                                            string c4 = $"{third.Url.Replace(".html", string.Empty)}_p{d + 1}.html";
                                            var p4 = await GetProductsFromPage(c4, third.Name);
                                            products.AddRange(p4);
                                        }

                                        byte[] b4 = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(products));
                                        await SaveFile(new MemoryStream(b4), $"{third.Name}.dat");
                                        Console.ForegroundColor = ConsoleColor.Yellow; Console.WriteLine($"Done retrieving products for {third.Name}");
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //byte[] buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(products));
            //await SaveFile(new MemoryStream(buffer), "products.json");
            //Console.ForegroundColor = ConsoleColor.Yellow;  Console.WriteLine("Done retrieving products for all categories...");
            //Console.ForegroundColor = ConsoleColor.White;
        }

        public async Task GetProductDetails()
        {
            var files = Directory.EnumerateFiles(baseDir, "*.dat");
            List<Product> products = new();
            foreach (var file in files)
            {
                string content = "";
                using (var reader = new StreamReader(file))
                {
                    content = reader.ReadToEnd();
                }

                List<Product> pre = JsonConvert.DeserializeObject<List<Product>>(content);
                foreach (var product in pre)
                {
                    products.Add(await ProductDetails(product.Url, product.Category));
                }
            }


            byte[] buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(products));
            await SaveFile(new MemoryStream(buffer), "crawl.result.json");
            Console.ForegroundColor = ConsoleColor.Yellow; Console.WriteLine("Done retrieving product details");
            Console.ForegroundColor = ConsoleColor.White;
        }

        private async Task<Product> ProductDetails(string url, string category)
        {
            var config = Configuration.Default.WithDefaultLoader();
            var context = BrowsingContext.New(config);

            Console.ForegroundColor = ConsoleColor.White; Console.Write($"Getting product details from ");
            Console.ForegroundColor = ConsoleColor.DarkGreen; Console.WriteLine(url);

            var document = await context.OpenAsync(url);
            Console.ForegroundColor = ConsoleColor.DarkYellow; Console.WriteLine($"Extracting contents...");

            // div containing the details
            var ulSelector = "div.area.clearfix div.detail";
            var div = document.QuerySelectorAll(ulSelector);

            //thumbnail
            var thumbSelector = "div.tab-picture";// 
            var thumb = div.Children(thumbSelector).First().QuerySelectorAll("img.img_autosize");
            var fi = thumb.First().GetAttribute("src");

            // product general info
            var gSelector = "div.tab-property.tab-property-yx";

            var title = div[0].QuerySelectorAll(gSelector).First().QuerySelector("h1").TextContent.Trim(); // the h1 tag

            var attribs = div[0].QuerySelectorAll($"{gSelector} dl");

            string k = "<table class=\"table table-striped\"><tbody>";
            var marjorAttrib = attribs.First();
            var atr1 = marjorAttrib.Children.First().TextContent.Trim();// first element has a different pattern
            var atr1Val = marjorAttrib.Children.Last().TextContent.Trim();

            k = $"{k}<tr><td> {atr1}:{atr1Val}</td></tr>";
            if (attribs.Length > 1)
            {
                for (int a = 1; a < attribs.Length; a++)
                {
                    var val = attribs.ElementAt(a).TextContent.Trim();
                    k = $"{k}<tr><td> {val}</td></tr>";
                }
            }
            k = $"{k}</tbody></table>";

            var dc = div[0].QuerySelectorAll("div.tabs-main-inx table");
            var description = string.Join(' ', dc.Select(x => x.OuterHtml));

            Product product = new()
            {
                ThumbnailUrl = fi,
                Name = title,
                Category = category,
                Description = $"{k}{description}"
            };

            return product;
        }

        private int GetNumberOfPages(string url)
        {
            var config = Configuration.Default.WithDefaultLoader();
            var context = BrowsingContext.New(config);

            Console.ForegroundColor = ConsoleColor.White; Console.Write($"Fetching category page from ");
            Console.ForegroundColor = ConsoleColor.DarkGreen; Console.WriteLine(url);
            
            var document = context.OpenAsync(url).GetAwaiter().GetResult();
            Console.ForegroundColor = ConsoleColor.DarkYellow; Console.WriteLine($"Extracting..");

            var ulSelector = "div.wrapper div.PageCtrlArea div.PageCtrl";
            var lis = document.QuerySelectorAll(ulSelector).Children("li");
            if (lis.Any())
            {
                string href = lis.Last().Children.First().GetAttribute("href");
                if (!string.IsNullOrEmpty(href)) // if the link is not disabled
                {
                    int pages = int.Parse(href.Split('/').Last().Split('.').First().Split('_').Last().Remove(0, 1));
                    return pages;
                }
            }

            Console.ForegroundColor = ConsoleColor.DarkBlue; Console.Write($"Done: ");
            Console.ForegroundColor = ConsoleColor.White; Console.Write($"at ");
            Console.ForegroundColor = ConsoleColor.DarkGreen; Console.WriteLine(url);
            Console.ForegroundColor = ConsoleColor.White;
            return 0;
        }

        private async Task<IEnumerable<Product>> GetProductsFromPage(string url, string category)
        {
            Console.WriteLine();
            var config = Configuration.Default.WithDefaultLoader();
            var context = BrowsingContext.New(config);
            Console.ForegroundColor = ConsoleColor.White; Console.Write($"INFO: Retrieving products from ");
            Console.ForegroundColor = ConsoleColor.DarkGreen; Console.WriteLine(url);

            var document = await context.OpenAsync(url);
            
            Console.ForegroundColor = ConsoleColor.White; Console.WriteLine($"INFO: Finding products from html result..");
            var ulSelector = "div.yx-list-show div.yx-list-main ul.clearfix";
            var lis = document.QuerySelectorAll(ulSelector).Children("li");
            List<Product> products = new ();
            foreach (var li in lis)
            {
                var id = li.Children.First().Children.First().GetAttribute("data-PartId");
                products.Add(new Product { Category = category, Url = $"http://www.en.autopart100.cn/parts/{id}.html" });
            }
            
            Console.WriteLine($"Done: retrieved {products.Count} products from {category}");
            return products;
        }
    }
}
