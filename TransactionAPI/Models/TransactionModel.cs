using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TransactionAPI.Models
{
    public class TransactionModel
    {

        public readonly Dictionary<string, string> allowedPartners = new Dictionary<string, string>
        {
            { "Admin", "Admin"},
            { "FG-00001", "FAKEPASSWORD1234" },
            { "FG-00002", "FAKEPASSWORD4578" }
        };

        public class TransactionRequest
        {
            [Required(ErrorMessage = "PartnerKey is required.")]
            [MaxLength(50, ErrorMessage = "PartnerKey cannot exceed 50 characters.")]
            public string PartnerKey { get; set; }

            [Required(ErrorMessage = "PartnerRefNo is required.")]
            [MaxLength(50, ErrorMessage = "PartnerRefNo cannot exceed 50 characters.")]
            public string PartnerRefNo { get; set; }

            [Required(ErrorMessage = "PartnerPassword is required.")]
            [MaxLength(50, ErrorMessage = "PartnerPassword cannot exceed 50 characters.")]
            public string PartnerPassword { get; set; }

            [Required(ErrorMessage = "TotalAmount is required.")]
            [Range(1, long.MaxValue, ErrorMessage = "TotalAmount must be positive.")]
            public long TotalAmount { get; set; }

            public List<ItemDetail>? Items { get; set; }

            [Required(ErrorMessage = "Timestamp is required.")]
            public string Timestamp { get; set; }

            [Required(ErrorMessage = "Signature is required.")]
            public string Sig { get; set; }
        }

        public class ItemDetail
        {
            [Required(ErrorMessage = "PartnerItemRef is required.")]
            [MaxLength(50, ErrorMessage = "PartnerItemRef cannot exceed 50 characters.")]
            public string PartnerItemRef { get; set; }

            [Required(ErrorMessage = "Name is required.")]
            [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
            public string Name { get; set; }

            [Range(1, 5, ErrorMessage = "Quantity must be between 1 and 5.")]
            public int Qty { get; set; }

            [Range(1, long.MaxValue, ErrorMessage = "UnitPrice must be positive.")]
            public long UnitPrice { get; set; }
        }

        public class TransactionResponse
        {
            public int? Result { get; set; } // 1 = Success, 0 = Failure

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public long? TotalAmount { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public long? TotalDiscount { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public long? FinalAmount { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? ResultMessage { get; set; }
        }
    }
}
