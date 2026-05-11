using System.ComponentModel.DataAnnotations;

namespace PayderPay.Application.Dtos.Customers;

public class CreateCustomerRequest
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

    [Range(typeof(decimal), "0", "9999999999")]
    public decimal InitialMainAccountBalance { get; set; }
}
