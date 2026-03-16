using TaxiDispatch.Data.Models;
using TaxiDispatch.Domain;
using TaxiDispatch.DTOs;
using TaxiDispatch.DTOs.Booking;
using TaxiDispatch.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace TaxiDispatch.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "v1")]
    [ApiAuthorize]
    public class AccountsController : ControllerBase
    {
        private AccountsService _service;
        private readonly UserManager<AppUser> _userManager;
        private readonly GeoZoneService _geoService;

        public AccountsController(AccountsService service, UserManager<AppUser> userManager, GeoZoneService geoService)
        {
            _service = service;
            _userManager = userManager;
            _geoService = geoService;
        }


        [HttpGet]
        [Route("GetAccountTariffs")]
        public async Task<IActionResult> GetAccountTariffs() 
        {
            var data = await _service.GetAccountTariffs();
            return Ok(data);
        }

        [HttpPost]
        [Route("CreateOrUpdateAccountTariff")]
        public async Task<IActionResult> CreateOrUpdateAccountTariff(AccountTariff tariff)
        {
            await _service.CreateOrUpdateAccountTariff(tariff);
            return Ok(tariff);
        }

        [HttpPost]
        [Route("AddOrUpdateZonePrice")]
        public async Task<IActionResult> AddOrUpdateZonePrice(ZoneToZonePrice dto)
        {
            if (dto.Id == 0)
            {
                var res = await _geoService.AddZonePrice(dto);
                return Ok(res);
            }
            else
            {
                await _geoService.UpdateZonePrice(dto);
                return Ok("updated");
            }
            
        }

        [HttpGet]
        [Route("GetZonePrices")]
        public async Task<IActionResult> GetZonePrices()
        {
            var data = await _geoService.GetZonePrices();
            return Ok(data);
        }

        [ApiAuthorize]
        [HttpGet]
        [Route("GetList")]
        public async Task<IActionResult> GetAccountsList()
        {
            var res = new List<AccountModel>();
            var data = await _service.GetAllAccounts();

            foreach (var account in data)
            {
                res.Add(new AccountModel { AccNo = account.AccNo, AccountName = account.BusinessName });
            }

            return Ok(res);
        }

        #region BILLING & PAYMENTS

        [HttpPost]
        [Route("PostOrUnPostJobsAccountDriver")]
        public async Task<IActionResult> PostOrUnPostJobsAccountDriver(int[] jobnos, bool postJob)
        {
            foreach (var jobno in jobnos)
            {
                await _service.PostJob(jobno, postJob);
                await _service.PostJobDriver(jobno, postJob);
            }

            return Ok();
        }

        #region DRIVER

        [ApiAuthorize]
        [HttpPost]
        [Route("DriverPriceJobByMileage")]
        public async Task<IActionResult> DriverPriceJobByMileage(UpdateBookingQuoteRequestDto obj)
        {
            if(!obj.IsValid)
            {
                return BadRequest("Invalid data or missing required fields");
            }

            var uname = User.Identity.Name;

            var user = await _userManager.FindByNameAsync(uname);

            obj.ActionByUserId = user.Id;
            obj.UpdatedByName = user.FullName;
            obj.PriceFromBase = true;

            await _service.PriceBookingByMileage(obj);

            return Ok();
        }

        [HttpPost]
        [Route("DriverPostOrUnPostJobs")]
        public async Task<IActionResult> DriverPostOrUnPostJobs(int[] jobnos, bool postJob)
        {
            foreach (var jobno in jobnos)
            {
                await _service.PostJobDriver(jobno,postJob);
            }

            return Ok();
        }

        [HttpPost]
        [Route("DriverGetChargableJobs")]
        public async Task<IActionResult> DriverGetChargableJobs(int userId, BookingScope scope, DateTime lastDate)
        {
            var data = await _service.GetChargableJobsForDriver(scope, lastDate, userId);

            // split data as priced or unpriced
            var chargablePriced = data.Where(o => o.PostedForStatement == true).ToList();
            var chargableNotPriced = data.Where(o => o.PostedForStatement == false).ToList();

            return Ok(new { Priced = chargablePriced, NotPriced = chargableNotPriced});
        }

        [HttpPost]
        [Route("DriverUpdateChargesData")]
        public async Task<IActionResult> DriverUpdateChargesData(UpdateChargesDataDto obj)
        {
            if (!obj.IsValid)
            {
                return BadRequest("Invalid data or missing required fields");
            }

            var res = await _service.UpdateChargesDataDriver(obj.BookingId, obj.WaitingMinutes, obj.ParkingCharge, obj.Price);
            return Ok(res);
        }


        [HttpPost]
        [Route("DriverCreateStatments")]
        public async Task<IActionResult> DriverCreateStatements(List<ChargeableJob> jobs)
        {
            await _service.ProcessDrivers(jobs);
            return Ok();
        }

        [HttpPost]
        [Route("DriverGetStatments")]
        public async Task<IActionResult> DriverGetStatments(DateTime from, DateTime to, int userId)
        {
            var data = await _service.GetStatements(from.Date, to.To2359(), userId);
            var statements = data.OrderByDescending(o => o.DateCreated).ToList();

            // get driver name and colors
            var ids = await _service.GetDriverNameColor();

            foreach (var item in statements)
            {
                var entry = ids.Where(o => o.UserId == item.UserId).FirstOrDefault();

                if (entry != null)
                {
                    item.Identifier = $"{entry.UserId} - {entry.FullName}";
                    item.ColorCode = entry.ColorCode;
                }
            }

            return Ok(statements);
        }

        [HttpGet]
        [Route("ResendDriverStatement")]
        public async Task<IActionResult> ResendDriverStatement(int statementNo)
        {
            await _service.ResendDriverStatement(statementNo);
            return Ok();
        }

        [HttpGet]
        [Route("MarkStatementAsPaid")]
        public async Task<IActionResult> MarkStatementAsPaid(int statementNo)
        {
            await _service.MarkStatementPaid(statementNo);

            return Ok();
        }

        #endregion

        #region ACCOUNT

        [HttpPost]
        [Route("AccountPriceManually")]
        public async Task<IActionResult> AccountPriceManually(ManualPriceUpdateRequestDto obj)
        {
            if(!obj.IsValid)
            {
                return BadRequest("Invalid data or missing required fields");
            }

            await _service.ManualPriceAccountUpdate(obj);

            return Ok();
        }

        [HttpPost]
        [Route("AccountPriceJobByMileage")]
        public async Task<IActionResult> AccountPriceJobByMileage(UpdateBookingQuoteRequestDto obj)
        {
            if (!obj.IsValid)
            {
                return BadRequest("Invalid data or missing required fields");
            }

            var res = await _service.PriceBookingByMileageAccount(obj);

            return Ok(res);
        }

        [HttpPost]
        [Route("AccountPriceJobHVS")]
        public async Task<IActionResult> AccountPriceJobHVS(UpdateBookingQuoteRequestDto obj)
        {
            if(!obj.IsValid)
            {
                return BadRequest("Invalid data or missing required fields");
            }

            var res = await _service.UpdateGetAccPriceHVS(obj);

            return Ok(res);
        }

        [HttpPost]
        [Route("PriceBulk")]
        public async Task<IActionResult> PriceBulk(UpdateBookingQuoteBulkRequestDto obj)
        {
           // var d = await _service.Test();
            
            //return Ok(data);

            if (!obj.IsValid)
            {
                return BadRequest("Invalid data or missing required fields");
            }

            if (obj.AccountNo == 9014 || obj.AccountNo == 10026 || obj.AccountNo == 10031) //10031 test account
            {
                var res = await _service.UpdatePricesHVSBulk(obj);
                return Ok(res);
            }
            else
            {
                var res = await _service.UpdatePricesBulk(obj);
                return Ok(res);
            }
        }


        [HttpPost]
        [Route("AccountPriceJobHVSBulk")]
        public async Task<IActionResult> AccountPriceJobHVSBulk(UpdateBookingQuoteBulkRequestDto obj)
        {
            if (!obj.IsValid)
            {
                return BadRequest("Invalid data or missing required fields");
            }

            var res = await _service.UpdatePricesHVSBulk(obj);

            return Ok(res);
        }


        [HttpPost]
        [Route("AccountPostOrUnPostJobs")]
        public async Task<IActionResult> AccountPostOrUnPostJobs(int[] jobnos, bool postJob)
        {
            foreach (var jobno in jobnos)
            { 
                await _service.PostJob(jobno, postJob); 
            }

            return Ok();
        }

        [HttpPost]
        [Route("AccountGetChargableJobs")]
        public async Task<IActionResult> AccountGetChargableJobs(int accno, DateTime from, DateTime to)
        {
            //var uid = SelectedUserId;
            var data = await _service.GetChargableJobsForAccount(accno, from, to);

            // split data as priced or unpriced
            var chargablePriced = data.Where(o => o.PostedForInvoicing == true).ToList();
            var chargableNotPriced = data.Where(o => o.PostedForInvoicing == false).ToList();

            return Ok(new { Priced = chargablePriced, NotPriced = chargableNotPriced });
        }

        [HttpPost]
        [Route("AccountGetChargableJobsGroupedSplit")]
        public async Task<IActionResult> AccountGetChargableJobsGroupedSplit(int accno, DateTime from, DateTime to)
        {
            var data = await _service.GetChargableJobsForAccount(accno, from, to);

            // split to single and multi passenger
            var singles = data.Where(o => !o.Passenger.Contains(",")).ToList();
            var shares = data.Where(o => o.Passenger.Contains(",")).ToList();

            // split into priced / not priced
            var pricedSingles = singles.Where(o => o.PostedForInvoicing == true).ToList();
            var notPricedSingles = singles.Where(o => o.PostedForInvoicing == false).ToList();

            var pricedShares = shares.Where(o => o.PostedForInvoicing == true).ToList();
            var notPricedShares = shares.Where(o => o.PostedForInvoicing == false).ToList();

            // group 
            var pricedSinglesGrp = _service.GroupBidirectionalByPassenger(pricedSingles);
            var notPricedSinglesGrp = _service.GroupBidirectionalByPassenger(notPricedSingles);

            var pricedSharesGrp = _service.GroupByBirectionalJourney(pricedShares);
            var notPricedSharesGrp = _service.GroupByBirectionalJourney(notPricedShares);

            var model = new { 
                Singles = new 
                { 
                    Priced = pricedSinglesGrp,
                    NotPriced = notPricedSinglesGrp
                }, 
                Shared = new 
                {
                    Priced = pricedSharesGrp,
                    NotPriced = notPricedSharesGrp
                } 
            };

            return Ok(model);
        }

        [HttpPost]
        [Route("AccountGetChargableJobsGrouped")]
        public async Task<IActionResult> AccountGetChargableJobsGrouped(int accno, DateTime from, DateTime to)
        {
            var data = await _service.GetChargableJobsForAccount(accno, from, to);

            // split data as priced or unpriced
            var chargablePriced = data.Where(o => o.PostedForInvoicing == true).ToList();
            var chargableNotPriced = data.Where(o => o.PostedForInvoicing == false).ToList();



            var pricedGrouped = _service.GroupBidirectionalByPassenger(chargablePriced);
            var notPricedGrouped = _service.GroupBidirectionalByPassenger(chargableNotPriced);

            return Ok(new { Priced = pricedGrouped, NotPriced = notPricedGrouped });
        }

        [HttpPost]
        [Route("AccountUpdateChargesData")]
        public async Task<IActionResult> AccountUpdateChargesData(UpdateChargesDataDto obj)
        {
            if(!obj.IsValid)
            {
                return BadRequest("Invalid data or missing required fields");
            }

            var res = await _service.UpdateChargesDataAcc(obj.BookingId, obj.WaitingMinutes, obj.ParkingCharge, obj.PriceAccount, obj.Price);
            return Ok(res);
        }

        [HttpPost]
        [Route("AccountCreateInvoice")]
        public async Task<IActionResult> AccountCreateInvoice(bool emailInvoices, List<ChargeableJob> jobs)
        {
            var lst = await _service.ProcessAccounts(jobs, emailInvoices);
            return Ok(lst);
        }

        [HttpGet]
        [Route("MarkInvoiceAsPaid")]
        public async Task<IActionResult> MarkInvoiceAsPaid(int invoiceNo)
        {
            await _service.MarkInvoicePaid(invoiceNo);
            return Ok();
        }

        [ApiAuthorize]
        [HttpGet]
        [Route("DeleteInvoice")]
        [Route("ClearInvoice")]
        [Route("CreditInvoice")]
        public async Task<IActionResult> CreditInvoice(int invoiceNo, string reason)
        {
            if (invoiceNo == 0)
            {
                return BadRequest("Invalid invoice number");
            }

            await _service.CreditInvoice(invoiceNo,reason);

            return Ok();
        }

        [ApiAuthorize]
        [HttpPost]
        [Route("CreditJourneys")]
        public async Task<IActionResult> CreditJourneys(
            CreditJourneysRequestDto req)
        {
            if (req.InvoiceNumber == 0)
            {
                return BadRequest("Invalid invoice number");
            }

            if (req.BookingIds.Count() == 0)
            {
                return BadRequest("Invalid invoice number");
            }

            await _service.GenerateSendCreditNotePDF(req.InvoiceNumber, req.Reason, req.BookingIds);

            return Ok();
        }


        [HttpGet]
        [Route("GetCreditNotes")]
        public async Task<IActionResult> GetCreditNotes(int accno)
        {
            var credits = await _service.GetCreditNotes(accno);

            return Ok(credits);
        }

        [HttpGet]
        [Route("DownloadCreditNote")]
        public async Task<IActionResult> GetCreditNote(int creditnoteId)
        {
            var fname = $"credit-note-{creditnoteId}.pdf";
            var filePath = Path.Combine("Data\\CreditNotes", fname);

            // Check if the file exists
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { Message = "File not found." });
            }

            try
            {
                // Read the file into a stream
                var memory = new MemoryStream();
                await using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    await stream.CopyToAsync(memory);
                }

                memory.Position = 0; // Reset stream position

                // Determine content type (you might want to use a library like MimeTypesMap)
                string contentType = "application/octet-stream";

                return File(memory, contentType, Path.GetFileName(filePath));
            }
            catch (Exception ex)
            {
                // Log the error (implement logging as per your project standards)
                return StatusCode(500, new { Message = "An error occurred while retrieving the file.", Details = ex.Message });
            }
        }


        [HttpGet]
        [Route("GetInvoices")]
        public async Task<IActionResult> GetInvoices(DateTime from, DateTime to, int accno)
        {
            var data = await _service.GetInvoices(from, to.To2359(), accno);
            var invoices = data.OrderByDescending(o => o.InvoiceDate).ToList();

            return Ok(invoices);
        }

        [HttpGet]
        [Route("DownloadInvoice")]
        public async Task<IActionResult> GetInvoice(int invoiceNo)
        {
            var fname = $"invoice-{invoiceNo}.pdf";
            var filePath = Path.Combine("Data\\Invoices", fname);

            // Check if the file exists
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { Message = "File not found." });
            }

            try
            {
                // Read the file into a stream
                var memory = new MemoryStream();
                await using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    await stream.CopyToAsync(memory);
                }

                memory.Position = 0; // Reset stream position

                // Determine content type (you might want to use a library like MimeTypesMap)
                string contentType = "application/octet-stream";

                return File(memory, contentType, Path.GetFileName(filePath));
            }
            catch (Exception ex)
            {
                // Log the error (implement logging as per your project standards)
                return StatusCode(500, new { Message = "An error occurred while retrieving the file.", Details = ex.Message });
            }
        }

        [HttpGet]
        [Route("DownloadStatement")]
        public async Task<IActionResult> GetStatement(int statementId)
        {
            var fname = $"statement-{statementId}.pdf";
            var filePath = Path.Combine("Data\\Statements", fname);

            // Check if the file exists
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { Message = "File not found." });
            }

            try
            {
                // Read the file into a stream
                var memory = new MemoryStream();
                await using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    await stream.CopyToAsync(memory);
                }

                memory.Position = 0; // Reset stream position

                // Determine content type (you might want to use a library like MimeTypesMap)
                string contentType = "application/octet-stream";

                return File(memory, contentType, Path.GetFileName(filePath));
            }
            catch (Exception ex)
            {
                // Log the error (implement logging as per your project standards)
                return StatusCode(500, new { Message = "An error occurred while retrieving the file.", Details = ex.Message });
            }
        }


        [HttpGet]
        [Route("DownloadInvoiceCSV")]
        public async Task<IActionResult> GetInvoiceCSV(int invoiceNo)
        {
            var res = await _service.GenerateInvoiceCSV(invoiceNo);

            try
            {
                // Read the file into a stream
                var memory = new MemoryStream();
                await using (var stream = new FileStream(res.Filename, FileMode.Open, FileAccess.Read))
                {
                    await stream.CopyToAsync(memory);
                }

                memory.Position = 0; // Reset stream position

                // Determine content type (you might want to use a library like MimeTypesMap)
                string contentType = "text/csv";

                return File(memory, contentType, Path.GetFileName(res.Filename));
            }
            catch (Exception ex)
            {
                // Log the error (implement logging as per your project standards)
                return StatusCode(500, new { Message = "An error occurred while retrieving the file.", Details = ex.Message });
            }
        }

        [HttpGet]
        [Route("ResendInvoice")]
        public async Task<IActionResult> ReSendInvoice(int invoiceNo)
        {
            var fname = $"invoice-{invoiceNo}.pdf";
            var filePath = Path.Combine("Data\\Invoices", fname);

            // Check if the file exists
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { Message = "Invoice file not found." });
            }
           
            await _service.ResendAccountInvoice(invoiceNo);

            return Ok();
        }


        #endregion

        [HttpPost]
        [Route("VATOutputs")]
        public async Task<IActionResult> VATOutputs(RequestVATOutputDto req)
        {
            if (!req.IsValid)
            {
                return BadRequest("Invalid data or missing required fields");
            }

            var data = await _service.CalculateVatOutputs(req.Start, req.End);

            var fname = $"vatoutput.csv";
            var path = $"Data\\{fname}";

            System.IO.File.WriteAllText(path, data);

            var bytes = System.IO.File.ReadAllBytes(path);
            var b64s = Convert.ToBase64String(bytes);

            try
            {
                // Read the file into a stream
                var memory = new MemoryStream();
                await using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    await stream.CopyToAsync(memory);
                }

                memory.Position = 0; // Reset stream position

                // Determine content type (you might want to use a library like MimeTypesMap)
                string contentType = "text/csv";

                return File(memory, contentType, System.IO.Path.GetFileName(path));
            }
            catch (Exception ex)
            {
                // Log the error (implement logging as per your project standards)
                return StatusCode(500, new { Message = "An error occurred while retrieving the file.", Details = ex.Message });
            }
        }
        #endregion
    }
}



