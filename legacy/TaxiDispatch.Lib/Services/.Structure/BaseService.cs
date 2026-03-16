using TaxiDispatch.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace TaxiDispatch.Services
{
    public abstract class BaseService<T>
    {
        internal readonly TaxiDispatchContext _dB;
        public readonly ILogger<T> _logger;

        public BaseService(
            TaxiDispatchContext dB, ILogger<T> logger)
        {
            _logger = logger;
            _dB = dB;
        }

        public BaseService(
            IDbContextFactory<TaxiDispatchContext> factory, ILogger<T> logger)
        {
            _dB = factory.CreateDbContext();
            _logger = logger;
        }

        public IDbContextTransaction Transaction()
        {
            return _dB.Database.BeginTransaction();
        }
    }
}


