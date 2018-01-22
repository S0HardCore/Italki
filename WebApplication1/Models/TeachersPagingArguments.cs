namespace WebApplication1.Models
{
    public class TeachersPagingArguments
    {
        public int pageIndex { get; set; }
        public int pageSize { get; set; }
        public string sortField { get; set; }
        public string sortOrder { get; set; }
    }
}