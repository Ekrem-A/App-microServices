using Order.Domain.Common;

namespace Order.Domain.ValueObjects;

public class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string District { get; }
    public string PostalCode { get; }
    public string Country { get; }
    public string? BuildingNumber { get; }
    public string? ApartmentNumber { get; }

    private Address(
        string street,
        string city,
        string district,
        string postalCode,
        string country,
        string? buildingNumber,
        string? apartmentNumber)
    {
        Street = street;
        City = city;
        District = district;
        PostalCode = postalCode;
        Country = country;
        BuildingNumber = buildingNumber;
        ApartmentNumber = apartmentNumber;
    }

    public static Address Create(
        string street,
        string city,
        string district,
        string postalCode,
        string country = "Türkiye",
        string? buildingNumber = null,
        string? apartmentNumber = null)
    {
        if (string.IsNullOrWhiteSpace(street))
            throw new ArgumentException("Street cannot be empty", nameof(street));

        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City cannot be empty", nameof(city));

        if (string.IsNullOrWhiteSpace(district))
            throw new ArgumentException("District cannot be empty", nameof(district));

        if (string.IsNullOrWhiteSpace(postalCode))
            throw new ArgumentException("Postal code cannot be empty", nameof(postalCode));

        return new Address(
            street.Trim(),
            city.Trim(),
            district.Trim(),
            postalCode.Trim(),
            country.Trim(),
            buildingNumber?.Trim(),
            apartmentNumber?.Trim());
    }

    public string GetFullAddress()
    {
        var parts = new List<string> { Street };

        if (!string.IsNullOrWhiteSpace(BuildingNumber))
            parts.Add($"No: {BuildingNumber}");

        if (!string.IsNullOrWhiteSpace(ApartmentNumber))
            parts.Add($"Daire: {ApartmentNumber}");

        parts.Add(District);
        parts.Add($"{PostalCode} {City}");
        parts.Add(Country);

        return string.Join(", ", parts);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return District;
        yield return PostalCode;
        yield return Country;
        yield return BuildingNumber;
        yield return ApartmentNumber;
    }
}

