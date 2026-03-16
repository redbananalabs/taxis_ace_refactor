using System.ComponentModel;

namespace TaxiDispatch.Domain
{
    public enum GroupByType
    {
        Month,
        Year
    }
    public enum ViewPeriodBy
    { 
        Hourly,
        Daily,
        Weekly,
        Monthly,
        Quaterly,
        Yearly
    }

    public enum PushNotificationNavId
    {
        None = 0,
        Allocate = 1,
        Unallocate = 2,
        Amended = 3,
        Cancelled = 4,
        DirectMessage = 5,
        GlobalMessage = 6,
    }

    public enum BookingsByStatus
    {
        Unallocated = 0,
        Allocated = 1,
        Cancelled = 2,
        Completed = 3,
    }

    public enum NotificationEvent
    { 
        Default = 0,
        System,
        Driver
    }

    public enum NotificationStatus
    { 
        /// <summary>
        /// default for admin notification
        /// </summary>
        Default = 0,
        /// <summary>
        /// push specific
        /// </summary>
        Sent,
        /// <summary>
        /// push specific
        /// </summary>
        Recieved, 
        Read, 
    }

    public enum DocumentType
    {
        Insurance,
        MOT,
        DBS,
        VehicleBadge,
        DriverLicence,
        SafeGuarding,
        FirstAidCert,
        DriverPhoto
    }

    public enum ExpenseCategory
    { 
        Fuel,
        Parts,
        Insurance,
        MOT,
        DBS,
        VehicleBadge,
        Maintanence,
        Certification,
        Other
    }

    public enum ConfirmationStatus
    {
        [Description("-")]
        Select = 0,
        [Description("Confirmed")]
        Confirmed = 1,
        [Description("Not Confirmed")]
        Pending = 2
    }

    public enum PaymentStatus
    {
        [Description("-")]
        Select = 0,
        [Description("Paid")]
        Paid = 2,
        [Description("Awaiting Payment")]
        Pending = 3
    }

    public enum BookingScope
    {
        [Description("Cash")]
        Cash = 0,
        [Description("Account")]
        Account = 1,
        [Description("Rank")]
        Rank = 2,
        [Description("All")]
        All = 3,
        [Description("Card")]
        Card = 4
    }



    public enum TariffType
    {
        Tariff_1,
        Tariff_2,
        Tariff_3
    }

    public enum LocalPOIType
    {
        Not_Set,
        Train_Station,
        Supermarket,
        House,
        Pub,
        Restaurant,
        Doctors,
        Airport,
        Ferry_Port,
        Hotel,
        School,
        Hospital,
        Wedding_Venue,
        Miscellaneous,
        Shopping_Centre
    }

    public enum PriceStatus
    {
        All,
        UnPriced,
        Priced
    }

    public enum VehicleType
    { 
        Unknown,
        Saloon,
        Estate,
        MPV,
        MPVPlus,
        SUV
    }

    public enum SentMessageType
    { 
        DriverOnAllocate,
        DriverOnUnAllocate,
        DriverOnAmend,
        DriverOnCancel,
        CustomerOnAllocate,
        CustomerOnUnAllocate,
        CustomerOnAmend,
        CustomerOnCancel,
        CustomerOnComplete,
        DriverDirectMessage,
        DriverGlobalMessage
    }

    public enum SendMessageOfType
    {
        None = 0,
        WhatsApp = 1,
        Sms = 2,
        Push = 3
    }

    public enum DriverAvailabilityType
    { 
        NotSet = 0,
        Available = 1,
        Unavailable = 2
    }

    public enum AppDriverShift
    {
        Start = 1000,
        Finish = 1001,
        OnBreak = 1002,
        FinishBreak = 1003
    }

    public enum BookingStatus
    {
        None,
        AcceptedJob,
        RejectedJob,
        Complete,
        RejectedJobTimeout

    }

    public enum AppJobStatus
    { 
        OnRoute = 3003,
        AtPickup = 3004,
        PassengerOnBoard = 3005,
        SoonToClear = 3006,
        Clear = 3007,
        NoJob = 3008,
    }

    public enum AppJobOffer
    {
        Accept = 2000,
        Reject = 2001,
        TimedOut = 2002
    }

    public enum WebBookingStatus
    { 
        Default,
        Accepted,
        Rejected
    }
}

