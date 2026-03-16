using TaxiDispatch.Domain;
using TaxiDispatch.DTOs.LocalPOI;


namespace TaxiDispatch.DTOs.Booking
{
    public class GetPriceResponseDto
    {
        public bool FromBase { get; set; }
        public string Tariff { get; set; }

        public double DeadMileage { get; set; }
        public int DeadMinutes { get; set; }

        public double JourneyMileage { get; set; }
        public int JourneyMinutes { get; set; }


        public double TotalMileage { get { return Math.Round(DeadMileage + JourneyMileage,1); } }
        public int TotalMinutes { get { return DeadMinutes + JourneyMinutes; } }
        public double PriceAccount { get; set; }
        public double PriceDriver { get; set; }
        public string PriceReference { get; set; }

        public string MileageText 
        {
            get 
            {
                if (FromBase)
                {
                    return $"{TotalMileage:N1} Miles - (Dead Miles: {Math.Round(DeadMileage, 1)}) + (Trip Miles: {Math.Round(JourneyMileage, 1)})";
                }
                else
                {
                    return $"{TotalMileage:N1} Miles";
                }
                
                
            }
        }
        public string DurationText 
        {
            get 
            {
                var duration = new TimeSpan(0, TotalMinutes, 0);
                return $"{duration.Hours} Hour(s) {duration.Minutes} Minutes";
            }
        }

        public int Legs { get; set; }
    }
}
