using Microsoft.AspNetCore.Http;

namespace TaxiDispatch.DTOs
{
    public class SubmitTicketRequest
    {
        public string Subject { get; set; }
        public string Message { get; set; }
        // Use IFormFile for a single file, or List<IFormFile> for multiple files
        public IFormFile? Attachment { get; set; }
    }
}

