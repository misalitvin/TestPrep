namespace TestPrep.DTOs;

public class TripDTO
{
    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public DateTime DateFrom { get; set; }

    public DateTime DateTo { get; set; }

    public int MaxPeople { get; set; }

    public virtual IEnumerable<ClientDTO> Clients { get; set; }
    public virtual IEnumerable<CountryDTO> Countries { get; set; } 
}