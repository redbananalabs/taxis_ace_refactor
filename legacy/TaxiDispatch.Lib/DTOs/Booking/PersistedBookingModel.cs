#nullable disable
using TaxiDispatch.Data.Models;
using TaxiDispatch.Domain;
using Newtonsoft.Json;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using static TaxiDispatch.DTOs.Booking.AvailableHours;

namespace TaxiDispatch.DTOs.Booking
{
    public class PersistedBookingModel : BookingModel
    {
        // returned fields from user
        public string? RegNo { get; set; } = string.Empty;
        public string? BackgroundColorRGB { get; set; }
        public string? Fullname { get; set; }

        public int BookingId { get; set; }
        public int? UserId { get; set; }
        public int? SuggestedUserId { get; set; }

        public bool CancelledOnArrival { get; set; }

        public string CellText 
        { 
            get 
            {
                var mpv = Passengers > 4 ? "(MPV): " : "";
                var status = string.Empty;

                if (Status != null)
                {
                    if (Status == BookingStatus.RejectedJob)
                    {
                        status = "[R]";
                    }
                    else if (Status == BookingStatus.RejectedJobTimeout)
                    {
                        status = "[RT]";
                    }
                    else if (Status == BookingStatus.Complete)
                    {
                        if (Scope != BookingScope.Account)
                            status = "[C]";
                    }
                }

                if (CancelledOnArrival)
                {
                    if (Scope == BookingScope.Account)
                        return "COA - " + mpv + PassengerName;
                    else
                        return "COA - " + mpv + PickupAddress + " -- " + DestinationAddress;
                }
                else
                {
                    if (Scope == BookingScope.Account)
                        return status + " " + mpv + PassengerName;
                    else
                        return status + " " + mpv + PickupAddress + " -- " + DestinationAddress;
                }
            } 
        }

        [JsonIgnore]
        public string HasDetails
        {
            get 
            {
                return !string.IsNullOrEmpty(Details) ? "*" : "";
            }
        }

        [JsonIgnore]
        public BookingStatus? Status {  get; set; } 

        //[JsonIgnore]
        //public string? BookedOn
        //{
        //    get
        //    {
        //        return "todo";
        //    }
        //}

        //[JsonIgnore]
        //public string? ManuallyPriced
        //{
        //    get
        //    {
        //        return "todo";
        //    }
        //}

        //[JsonIgnore]

        //public string? ChargeFromBase
        //{
        //    get
        //    {
        //        return "todo";
        //    }
        //}

        public DateTime? EndTime
        {
            get
            {
                if (DurationMinutes < 20)
                {
                    return PickupDateTime.AddMinutes(20);
                }
                else
                {
                    return PickupDateTime.AddMinutes(DurationMinutes);
                }
            }
        }
    }
}
