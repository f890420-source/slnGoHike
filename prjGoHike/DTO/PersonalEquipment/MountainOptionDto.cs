namespace prjGoHike.DTO.PersonalEquipment;

public class MountainOptionDto
{
    public long MountainId { get; set; }

    public string MountainName { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public int Altitude { get; set; }

    public int DifficultyLevel { get; set; }

    public bool MountainsPermitRequired { get; set; }

    public bool NationalParkPermitRequired { get; set; }
}
