using System;
using System.Collections.Generic;

namespace ACDTE_WebCrawler.Models
{
    public class Category
    {
        public long Id { get; set; }

        public string Url { get; set; }

        public string Name { get; set; }

        public int Pages { get; set; }

        public long ParentId { get; set; }

        public List<Category> Children { get; set; } = new List<Category>();
    }
}
