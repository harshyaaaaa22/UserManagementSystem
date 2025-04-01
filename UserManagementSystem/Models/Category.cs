using System.ComponentModel.DataAnnotations;

namespace UserManagementSystem.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public string Description { get; set; }

        // Navigation property
        public virtual ICollection<Product> Products { get; set; }
    }
}
