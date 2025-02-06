using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TestPrep.DTOs;
using TestPrep.Entities;

namespace TestPrep.Controllers;
[Route("api/[controller]")]
[ApiController]
public class TripController : ControllerBase
{
    public readonly MydbContext Context;

    public TripController(MydbContext context)
    {
        Context = context;
    }

    [HttpGet]
    public async Task<ActionResult> GetTrips()
    {
        if (Context.Trips == null)
        {
            return new NotFoundResult();
        }
        
        var trips = await Context.Trips
            .OrderByDescending(t => t.DateFrom)
            .Select(t => new TripDTO()
            {
                Name = t.Name,
                Description = t.Description,
                DateFrom = t.DateFrom,
                DateTo = t.DateTo,
                MaxPeople = t.MaxPeople,

                Countries = t.IdCountries.Select(ct => new CountryDTO
                {
                    Name = ct.Name
                }).ToList(),

                Clients = t.ClientTrips.Select(ct => new ClientDTO
                {
                    FirstName = ct.IdClientNavigation.FirstName,
                    LastName = ct.IdClientNavigation.LastName
                }).ToList()
            }).ToListAsync();
        return Ok(trips);
    }

    [HttpPost("{id}")]
    public async Task<ActionResult> UpdateTrip([FromBody] TripDTO tripDto, int id)
    {
        var trip = await Context.Trips
            .Include(t => t.IdCountries)
            .Include(t => t.ClientTrips).
            FirstOrDefaultAsync(t => t.IdTrip == id);

        if (trip == null)
        {
            return BadRequest("There is no such trip");
        }
        trip.Name = tripDto.Name;
        trip.Description = tripDto.Description;
        trip.DateFrom = tripDto.DateFrom;
        trip.DateTo = tripDto.DateTo;
        trip.MaxPeople = tripDto.MaxPeople;

        trip.IdCountries.Clear(); // Remove existing country relationships
        if (tripDto.Countries.Any())
        {
            foreach (var countryDto in tripDto.Countries)
            {
                var country = await Context.Countries.FirstOrDefaultAsync(c => c.Name == countryDto.Name);
                if (country != null)
                {
                    trip.IdCountries.Add(country);
                }
                
            }
        }

        Context.ClientTrips.RemoveRange(trip.ClientTrips); 

        if (tripDto.Clients.Any())
        {
            foreach (var clientDto in tripDto.Clients)
            {
                var client = await Context.Clients.FirstOrDefaultAsync(c =>
                    c.FirstName == clientDto.FirstName && c.LastName == clientDto.LastName);

                if (client != null)
                {
                    trip.ClientTrips.Add(new ClientTrip
                    {
                        IdClient = client.IdClient,
                        IdTrip = trip.IdTrip,
                    });
                }
            }
        }
        
        
        await Context.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("add/{id}")]
    public async Task<ActionResult> AddTrip([FromBody] TripDTO t, int id)
    {
        var trip = await Context.Trips.FirstOrDefaultAsync(t => t.IdTrip == id);

        if (trip != null)
        {
            return BadRequest("Such trip is alreeady present");
        }

        var addtrip = new Trip()
        {
            Name = t.Name,
            Description = t.Description,
            DateFrom = t.DateFrom,
            DateTo = t.DateTo,
            MaxPeople = t.MaxPeople
        };


        if (t.Countries != null && t.Countries.Any())
        {
            foreach (var countryDto in t.Countries)
            {
                var country = await Context.Countries
                    .FirstOrDefaultAsync(c => c.Name == countryDto.Name);

                if (country != null)
                {
                    addtrip.IdCountries.Add(country);
                }
            }
        }

        // Handle Clients
        if (t.Clients != null && t.Clients.Any())
        {
            foreach (var clientDto in t.Clients)
            {
                var client = await Context.Clients
                    .FirstOrDefaultAsync(c => c.FirstName == clientDto.FirstName && c.LastName == clientDto.LastName);

                if (client != null)
                {
                    addtrip.ClientTrips.Add(new ClientTrip
                    {
                        IdClient = client.IdClient,
                        IdTrip = addtrip.IdTrip,
                        IdClientNavigation = client,
                        IdTripNavigation = addtrip
                    });
                }

            }
        }

        Context.Trips.Add(addtrip);
        await Context.SaveChangesAsync();
        return Ok();

    }

    [HttpDelete("delete/{id}")]
    public async Task<ActionResult> DeleteTrip(int id)
    {
        var trip = await Context.Trips
            .Include(t => t.ClientTrips)
            .Include(t => t.IdCountries)
            .FirstOrDefaultAsync(t => t.IdTrip == id);

        if (trip == null)
        {
            return BadRequest("There is no such trip");
        }
        
        Context.ClientTrips.RemoveRange(trip.ClientTrips);
        Context.Countries.RemoveRange(trip.IdCountries);

        Context.Trips.Remove(trip);
        await Context.SaveChangesAsync();
        return Ok();

    }


}