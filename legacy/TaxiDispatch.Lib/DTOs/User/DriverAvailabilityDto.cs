using TaxiDispatch.Domain;

namespace TaxiDispatch.DTOs.User
{
    public class DriverAvailabilityDto
    {
        public int? Id { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public string ColorCode { get; set; }
        public DriverAvailabilityType AvailabilityType { get; set; }
        public TimeSpan? From { get; set; } = new TimeSpan(0, 0, 0);
        public TimeSpan? To { get; set; } = new TimeSpan(0, 0, 0);
        public bool GiveOrTake { get; set; }

        public string AvailableHours
        {
            get
            {
                var got = GiveOrTake ? " (+/-)" : string.Empty;
                if (!string.IsNullOrEmpty(Description))
                {
                    if (Description == "AM School Run Only" || Description == "PM School Run Only")
                    {
                        return $"({Description}) {got}";
                    }

                    return $"{From.Value.ToString(@"hh\\:mm")} - {To.Value.ToString(@"hh\\:mm")} {got} ({Description})";
                }

                return $"{From.Value.ToString(@"hh\\:mm")} - {To.Value.ToString(@"hh\\:mm")} {got}";
            }
        }
    }
}
