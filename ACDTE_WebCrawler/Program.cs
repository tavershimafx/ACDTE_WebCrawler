using System;

namespace ACDTE_WebCrawler
{
    class Program
    {
        static void Main(string[] args)
        {
            AutoPart100Crawler crawler = new();

            Console.WriteLine("Job starting..."); 
            //crawler.ExtractCategoryUrls().GetAwaiter().GetResult();
            //crawler.GetNumberOfPagesForCategory().GetAwaiter().GetResult();
            //crawler.GetProducts().GetAwaiter().GetResult();
            crawler.GetProductDetails().GetAwaiter().GetResult();
        }
    }
}
