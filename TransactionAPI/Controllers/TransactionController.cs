using log4net;
using Microsoft.AspNetCore.Mvc;
using TransactionAPI.Services;
using static TransactionAPI.Models.TransactionModel;

namespace TransactionAPI.Controllers
{
    public class TransactionController : ControllerBase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(TransactionController));
        private readonly TransactionServices _transactionServices;

        public TransactionController(TransactionServices transactionServices)
        {
            _transactionServices = transactionServices;
        }

        [HttpPost("submittrxmessage")]
        public IActionResult SubmitTransaction([FromBody] TransactionRequest request)
        {

            if (request != null) log.Info($"Request: {request}");
            else throw new Exception();

            try
            {
                var validationResult = _transactionServices.ValidateTransactionRequest(request);

                if (validationResult != null)
                {
                    log.Info($"Response[Validation Failed]: {validationResult}");
                    return BadRequest(validationResult);
                }

                var response = _transactionServices.ProcessTransaction(request);
                log.Info($"Response[Success]: {response}");

                return Ok(response);
            }
            catch (Exception ex)
            {
                log.Error($"Error Thrown Location : {nameof(SubmitTransaction)} \n\nStackTrace: {ex.StackTrace}\n\nStackMessage: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
