using log4net;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using static TransactionAPI.Models.TransactionModel;
using TransactionAPI.Models;
using Newtonsoft.Json;

namespace TransactionAPI.Services
{
    public class TransactionServices
    {
        #region Dependency Injection
        private static readonly ILog log = LogManager.GetLogger(typeof(TransactionServices));
        private readonly TransactionModel _transactionModel;
        #endregion

        public TransactionServices(TransactionModel transactionModel)
        {
            _transactionModel = transactionModel;
        }
        public TransactionResponse ValidateTransactionRequest(TransactionRequest request)
        {
            try
            {
                // Validate User
                if (!IsAllowedPartner(request.PartnerKey, request.PartnerPassword)) return CreateErrorResponse("Access Denied: Invalid PartnerKey or PartnerPassword");

                // Allow Bypass only for Admin Profile
                if (!(request.PartnerKey.ToLower().Contains("admin")))
                {
                    // Validate Timestamp
                    if (!IsValidTimestamp(request.Timestamp)) return new TransactionResponse { Result = 0, ResultMessage = "Expired." };

                    // Validate Signature
                    if (!IsValidSignature(request)) return CreateErrorResponse("Access Denied: Invalid Signature");
                }

                // Field Validations
                var validationResults = new List<ValidationResult>();
                var validationContext = new ValidationContext(request);
                if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
                {
                    LogErrors(validationResults);
                    return CreateErrorResponse(string.Join("; ", validationResults.Select(vr => vr.ErrorMessage)));
                }

                // Validate Items
                if (!ValidateItems(request, out string itemError)) return CreateErrorResponse(itemError);

                // Successful Return
                return new TransactionResponse
                {
                    Result = 1,
                    TotalAmount = request.TotalAmount,
                    TotalDiscount = 0,
                    FinalAmount = request.TotalAmount
                };

            }
            catch (Exception ex)
            {
                log.Error($"Error Thrown Location : {nameof(ValidateTransactionRequest)} \n\nStackTrace: {ex.StackTrace}\n\nStackMessage: {ex.Message}");
                return CreateErrorResponse("An error occurred while processing the request.");
            }
        }

        #region Error Logging
        private void LogErrors(IEnumerable<ValidationResult> validationResults)
        {
            foreach (var validationResult in validationResults)
            {
                log.Error(validationResult.ErrorMessage);
            }
        }
        private TransactionResponse CreateErrorResponse(string message)
        {
            log.Error(message);
            return new TransactionResponse
            {
                Result = 0,
                ResultMessage = "Access Denied!"
            };
        }
        #endregion

        #region Validation Helpers
        private bool IsAllowedPartner(string partnerKey, string partnerPassword)
        {
            return _transactionModel.allowedPartners.ContainsKey(partnerKey) && _transactionModel.allowedPartners[partnerKey] == partnerPassword;
        }
        private bool IsValidSignature(TransactionRequest request)
        {
            if (!DateTime.TryParseExact(request.Timestamp, "yyyy-MM-ddTHH:mm:ssZ", null, System.Globalization.DateTimeStyles.AdjustToUniversal, out DateTime parsedTimestamp))
            {
                return false;
            }

            var correctedTimeStamp = parsedTimestamp.ToString("yyyyMMddHHmmss");

            var _ConcatenatedString = string.Concat(correctedTimeStamp, request.PartnerKey, request.PartnerRefNo, request.TotalAmount, request.PartnerPassword);
            using (var sha256 = SHA256.Create())
            {
                var hash = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(_ConcatenatedString)));
                return hash == request.Sig;
            }
        }
        private bool IsValidTimestamp(string timestamp)
        {
            if (!DateTime.TryParseExact(timestamp, "yyyy-MM-ddTHH:mm:ssZ", null, System.Globalization.DateTimeStyles.AdjustToUniversal, out DateTime requestTime))
            {
                return false;
            }

            var serverTime = DateTime.UtcNow;
            var timeDifference = Math.Abs((serverTime - requestTime).TotalMinutes);
            return timeDifference <= 5; // +-5 minutes validity
        }
        private bool ValidateItems(TransactionRequest request, out string error)
        {
            error = string.Empty;
            if (request.Items != null)
            {
                foreach (var item in request.Items)
                {
                    if (item.Qty <= 1 || item.Qty > 5)
                    {
                        error = "Quantity must be between 1 and 5.";
                        return false;
                    }
                    if (item.UnitPrice <= 0)
                    {
                        error = "UnitPrice must be positive.";
                        return false;
                    }
                }

                var itemTotalAmount = request.Items.Sum(item => item.Qty * item.UnitPrice);
                if (itemTotalAmount != request.TotalAmount)
                {
                    error = "Invalid Total Amount in itemDetails.";
                    return false;
                }
            }
            return true;
        }
        #endregion

        #region Transaction Processing

        public TransactionResponse ProcessTransaction(TransactionRequest request)
        {
            // Example of applying business logic for discounts
            var totalDiscount = CalculateDiscount(request.TotalAmount);
            var finalAmount = request.TotalAmount - totalDiscount;

            return new TransactionResponse
            {
                Result = 1, // Success
                TotalAmount = request.TotalAmount,
                TotalDiscount = totalDiscount,
                FinalAmount = finalAmount
            };
        }
        private long CalculateDiscount(long totalAmount)
        {
            // Base discount rules
            double baseDiscount = totalAmount switch
            {
                var x when x < 200 => 0,
                var x when x <= 500 => 0.05,
                var x when x <= 800 => 0.07,
                var x when x <= 1200 => 0.1,
                _ => 0.15
            };

            double discount = totalAmount * baseDiscount;

            // Check conditional discounts
            if (IsPrime(totalAmount) && totalAmount > 500)
                discount += totalAmount * 0.08;

            if (totalAmount % 10 == 5 && totalAmount > 900)
                discount += totalAmount * 0.1;

            // Cap discount to 20% of total amount
            return (long)Math.Min(discount, totalAmount * 0.2);
        }
        private bool IsPrime(long number)
        {
            if (number <= 1) return false;
            for (long i = 2; i <= Math.Sqrt(number); i++)
            {
                if (number % i == 0) return false;
            }
            return true;
        }

        #endregion
    }
}
