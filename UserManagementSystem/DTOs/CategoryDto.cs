﻿namespace UserManagementSystem.DTOs
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class CategoryCreateDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class CategoryUpdateDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class CategoryWithProductsDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<ProductDto> Products { get; set; }
    }
}
