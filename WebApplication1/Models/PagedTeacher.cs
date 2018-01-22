using System.Collections.Generic;

namespace WebApplication1.Models
{
    public class PagedTeacher
    {
        public List<Teacher> data { get; set; }
        public int itemsCount { get; set; }

        public PagedTeacher()
        {
            data = new List<Teacher>();
        }
    }
}