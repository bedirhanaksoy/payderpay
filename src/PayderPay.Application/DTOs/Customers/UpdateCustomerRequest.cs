using System.ComponentModel.DataAnnotations;

namespace PayderPay.Application.DTOs.Customers;

public class UpdateCustomerRequest
{
    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
