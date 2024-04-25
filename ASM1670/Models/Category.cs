namespace ASM1670.Models
{
    public class Category
    {
        public int CategoryId { get; set; }
        public string? Name { get; set; }
        public ICollection<Job>? Jobs { get; set; }
    }
}
