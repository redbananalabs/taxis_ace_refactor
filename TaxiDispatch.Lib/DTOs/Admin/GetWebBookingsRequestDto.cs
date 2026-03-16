namespace TaxiDispatch.DTOs.Admin
{
    public class GetWebBookingsRequestDto
    {
        public int? AccNo { get; set; }
        public bool Processed { get; set; }
        public bool Accepted { get; set; }
        public bool Rejected { get; set; }
    }
}

