using TaxiDispatch.DTOs.LocalPOI;
using TaxiDispatch.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Logging;
using System.Net;
using System.Net.Mime;

namespace TaxiDispatch.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "v1")]
    public class LocalPOIController : ControllerBase
    {
        private LocalPOIService _service;

        public LocalPOIController(LocalPOIService service)
        {
            _service = service;
        }

     //   [Authorize()]
        [HttpPost]
        [Route("GetPOI")]
        public async Task<IActionResult> GetLocalPOI(GetLocalPOIRequest request)
        {
            var res = await _service.GetLocalPOI(request.SearchTerm);
            return Ok(res);
        }

    //    [Authorize()]
        [HttpPost]
        [Route("GetPOI2")]
        public async Task<IActionResult> GetAddressWithPOI(GetLocalPOIRequest request)
        {
            var res = await _service.GetLocalPOI(request.SearchTerm);
            return Ok(res);
        }

        [ApiAuthorize]
        [HttpPost]
        [Route("Create")]
        public async Task<IActionResult> CreateLocalPOI(CreatePOIRequest request) 
        {
            var res = await _service.CreatePOI(request);
            return Ok(res);
        }

        [ApiAuthorize]
        [HttpPost]
        [Route("Update")]
        public async Task<IActionResult> UpdateLocalPOI(UpdatePOIRequest request)
        {
            var res = await _service.UpdatePOI(request);
            return Ok(res);
        }

        [ApiAuthorize]
        [HttpPost]
        [Route("Delete")]
        public async Task<IActionResult> DeleteLocalPOI(DeletePOIRequest request)
        {
            var res = await _service.DeletePOI(request);
            return Ok(res);
        }

        [HttpPost]
        [Route("Upload")]
        [Consumes("multipart/form-data")]
        [ApiAuthorize]
        public async Task<IActionResult> Post(IFormFile file)
        {
            // full path to file in temp location
            var filePath = Path.GetTempFileName();

            if (file.Length > 0)
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
            }

            // process uploaded files
            // Don't rely on or trust the FileName property without validation.
            
            var count = await _service.ImportCsv(filePath);
            
            return Ok(count);
        }
       
        public class FileUpload 
        {
            public IFormFile File { get; set; }
        }
    }
}

