using System.Collections.Generic;

namespace WebApplication1.Models
{
    public class Teacher
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public float Rating { get; set; }
        public string Description { get; set; }
        public int Students { get; set; }
        public int Lessons { get; set; }
        public float Price { get; set; }
        public string Country { get; set; }
        public List<Language> Languages { get; set; }
        public List<Tag> Tags { get; set; }
    }
}