namespace Arelia.Domain.Entities;

public class Organization
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Settings
    public string DefaultCurrencyCode { get; set; } = "DKK";
    public bool DefaultPublicVisible { get; set; }
    public DayOfWeek? DefaultRehearsalDay { get; set; } = DayOfWeek.Thursday;
    public TimeOnly? DefaultRehearsalStartTime { get; set; } = new(19, 0);
    public int? DefaultRehearsalDurationMinutes { get; set; } = 150;
    public string? DefaultRehearsalLocation { get; set; }
    public string Timezone { get; set; } = "Europe/Copenhagen";
    public string? DefaultLanguage { get; set; } = "en";
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // Navigation
    public ICollection<OrganizationUser> OrganizationUsers { get; set; } = [];
}
