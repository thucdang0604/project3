using System;
using System.ComponentModel.DataAnnotations;

namespace JamesThewProject.Models
{
    public class PaymentViewModel
    {
        [Required(ErrorMessage = "Card Holder Name is required.")]
        public string CardHolderName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Card Number is required.")]
        [RegularExpression(@"\d{16}", ErrorMessage = "Card Number must be 16 digits.")]
        public string CardNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Expiry Date is required.")]
        public DateTime ExpiryDate { get; set; }

        [Required(ErrorMessage = "CVV is required.")]
        [RegularExpression(@"\d{3}", ErrorMessage = "CVV must be 3 digits.")]
        public string CVV { get; set; } = string.Empty;

        [Required(ErrorMessage = "Billing Address is required.")]
        public string BillingAddress { get; set; } = string.Empty;
    }
}
