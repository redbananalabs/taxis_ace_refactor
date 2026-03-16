using TaxiDispatch.Data.Models;
using TaxiDispatch.DTOs;
using TaxiDispatch.DTOs.Admin;
using TaxiDispatch.DTOs.Booking;
using TaxiDispatch.DTOs.MessageTemplates;
using AutoMapper;

namespace TaxiDispatch.Configuration
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<CreateBookingRequestDto, Booking>();
            CreateMap<UpdateBookingRequestDto, Booking>();

            CreateMap<Booking, PersistedBookingModel>();

            CreateMap<AccountPassengerDto, AccountPassenger>();
            CreateMap<AccountPassenger, AccountPassengerDto>();

            CreateMap<WebBookingDto, WebBooking>();

            CreateMap<DriverOnShift, DriverOnShiftDto>();

            CreateMap<DriverInvoiceStatement, DriverInvoiceStatementDto>();
            CreateMap<DriverStatementDto, DriverInvoiceStatementDto>();
            CreateMap<DriverExpense, DriverExpenseDto>();
            CreateMap<DriverExpenseDto, DriverExpense>();
            CreateMap<AccountDto, Account>();

            AllowNullCollections = true;
        }
    }
}

