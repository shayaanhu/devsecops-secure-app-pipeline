// DTO/CreateVehicleDto.cs
using System.ComponentModel.DataAnnotations;

namespace CarpoolApp.Server.DTO
{
    public class CreateVehicleDto
    {
        [Required]
        [StringLength(10, ErrorMessage = "Make cannot exceed 10 characters.")]
        [RegularExpression("^[A-Za-z]+$", ErrorMessage = "Make can only contain letters.")]
        public string Make { get; set; }

        [Required]
        [StringLength(10, ErrorMessage = "Model cannot exceed 10 characters.")]
        public string Model { get; set; }

        [Required]
        [StringLength(7, ErrorMessage = "Number Plate cannot exceed 7 characters.")]
        [RegularExpression("^[A-Za-z0-9-]+$", ErrorMessage = "Number Plate can only contain letters, numbers, and hyphens.")]
        public string NumberPlate { get; set; }
    }
}