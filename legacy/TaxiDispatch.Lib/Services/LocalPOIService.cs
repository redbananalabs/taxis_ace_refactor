using TaxiDispatch.Data;
using TaxiDispatch.Data.Models;
using TaxiDispatch.Domain;
using TaxiDispatch.DTOs.LocalPOI;
using Amazon.Runtime.Internal.Util;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace TaxiDispatch.Services
{
    public class LocalPOIService : BaseService<LocalPOIService>
    {
        private readonly IMapper _mapper;
        public LocalPOIService(
            TaxiDispatchContext dB,
            IMapper mapper,ILogger<LocalPOIService> logger)
            : base(dB, logger)
        {
            _mapper = mapper;
        }

        public async Task<List<LocalPOIModel>> GetLocalPOI(string searchTerm)
        {
            var lst = new List<LocalPOIModel>();

            List<LocalPOI> pois = new List<LocalPOI>();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                pois = await _dB.LocalPOIs.Where(o => o.Address.ToLower().StartsWith(searchTerm.ToLower()) || 
                    o.Name.ToLower().StartsWith(searchTerm.ToLower())).ToListAsync();
            }
            else
            {
                pois = await _dB.LocalPOIs.ToListAsync(); 
            }

            foreach (var item in pois)
            {
                lst.Add(new LocalPOIModel
                {
                    Id = item.Id,
                    Name = item.Name,
                    Address = item.Address,
                    Postcode = item.Postcode,
                    Type = item.Type,
                    Latitude = item.Latitude,
                    Longitude = item.Longitude
                });
            }

            return lst;
        }

        public async Task<LocalPOIModel?> GetLocalPOIById(int id) 
        {
            if (_dB.LocalPOIs != null)
            {
                var item = await _dB.LocalPOIs.FirstOrDefaultAsync(o => o.Id == id);
                if (item != null)
                {
                    return new LocalPOIModel()
                    {
                        Id = item.Id,
                        Name = item.Name,
                        Address = item.Address,
                        Postcode = item.Postcode,
                        Type = item.Type,
                        Latitude = item.Latitude,
                        Longitude = item.Longitude
                    };
                }
                else 
                {
                    return null;
                }
            }
            else
                return null;
        }

        public async Task<Result> CreatePOI(CreatePOIRequest request)
        {
            await _dB.LocalPOIs.AddAsync(new LocalPOI 
            { 
                Name= request.Name,
                Address = request.Address, 
                Postcode = request.Postcode,
                Type = request.Type
            });

            await _dB.SaveChangesAsync();
            return Result.Ok();
        }

        public async Task<Result> UpdatePOI(UpdatePOIRequest request)
        {
            var poi = await _dB.LocalPOIs.Where(o=>o.Id == request.Id).FirstOrDefaultAsync();

            if (poi != null)
            {
                poi.Name = request.Name;
                poi.Address = request.Address;
                poi.Postcode = request.Postcode;
                poi.Type = request.Type;

                await _dB.SaveChangesAsync();
                return Result.Ok();
            }
            else
            {
                throw new Exception($"The POI with Id {request.Id} was not found.");
            }
        }

        public async Task<Result> DeletePOI(DeletePOIRequest request)
        {
            var poi = await _dB.LocalPOIs.Where(o => o.Id == request.Id).FirstOrDefaultAsync();

            if (poi != null)
            {
                _dB.LocalPOIs.Remove(poi);

                await _dB.SaveChangesAsync();
                return Result.Ok();
            }
            else
            {
                throw new Exception($"The POI with Id {request.Id} was not found.");
            }
        }

        public async Task DeleteAll()
        { 
            await _dB.LocalPOIs.ExecuteDeleteAsync();
        }

        public async Task<int> ImportCsv(string filepath)
        { 
            await DeleteAll();

            var lines = await File.ReadAllLinesAsync(filepath);

            var pois = new List<LocalPOI>();

            for (int i = 1; i < lines.Length; i++) 
            {
                var fields = lines[i].Split(",");

                var address = fields[0].Trim();
                var postcode = fields[1].Trim();
                var area = fields[2].Trim();

                pois.Add(new LocalPOI
                {
                    Address = address,
                    Area = area,
                    Postcode = postcode
                });
            }

            _dB.LocalPOIs.AddRange(pois);
            await _dB.SaveChangesAsync();
            return pois.Count;
        }
    }
}


