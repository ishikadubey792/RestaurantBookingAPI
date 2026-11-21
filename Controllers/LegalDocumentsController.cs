using Microsoft.AspNetCore.Mvc;
using RestaurantBookingAPI.Common;
using RestaurantBookingAPI.IServices;

namespace RestaurantBookingAPI.Controllers
{
    [ApiController]
    [Route("api/legal-documents")]
    public class LegalDocumentsController : ControllerBase
    {
        private readonly ILegalDocumentService _legalDocumentService;

        public LegalDocumentsController(
            ILegalDocumentService legalDocumentService)
        {
            _legalDocumentService = legalDocumentService;
        }

        // Public endpoint to fetch the currently active Terms and Conditions.
        [HttpGet("terms")]
        public async Task<IActionResult> GetTerms()
        {
            var terms = await _legalDocumentService.GetActiveTermsAsync();

            if (terms == null)
            {
                return NotFound(new
                {
                    message = "Terms and Conditions are currently unavailable."
                });
            }

            return ApiResponseHelper.Ok(
                this,
                "Terms and Conditions retrieved successfully.",
                (object)terms
            );
        }
    }
}