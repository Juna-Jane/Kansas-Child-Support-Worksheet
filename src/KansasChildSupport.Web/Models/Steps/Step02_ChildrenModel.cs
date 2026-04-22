using System.ComponentModel.DataAnnotations;

namespace KansasChildSupport.Web.Models.Steps;

public class Step02_ChildrenModel
{
    public List<ChildEntry> Children { get; set; } = new() { new ChildEntry() };
}

public class ChildEntry
{
    [Required(ErrorMessage = "Please enter the child's first name.")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Please enter the child's date of birth.")]
    public DateTime? DateOfBirth { get; set; }

    public int GetAge()
    {
        if (DateOfBirth == null) return 0;
        var today = DateTime.Today;
        var age = today.Year - DateOfBirth.Value.Year;
        if (DateOfBirth.Value.Date > today.AddYears(-age)) age--;
        return age;
    }

    /// <summary>
    /// Nearest birthday rule: round to nearest birthday
    /// </summary>
    public int GetAgeNearestBirthday()
    {
        if (DateOfBirth == null) return 0;
        var today = DateTime.Today;
        var age = today.Year - DateOfBirth.Value.Year;
        // Check if next birthday is closer than last birthday
        var lastBirthday = DateOfBirth.Value.AddYears(age);
        if (lastBirthday > today) lastBirthday = DateOfBirth.Value.AddYears(age - 1);
        var nextBirthday = lastBirthday.AddYears(1);
        if ((today - lastBirthday).TotalDays > (nextBirthday - today).TotalDays)
            age = today.Year - DateOfBirth.Value.Year + (DateOfBirth.Value.Month < today.Month || (DateOfBirth.Value.Month == today.Month && DateOfBirth.Value.Day <= today.Day) ? 0 : -1) + 1;
        else
            age = today.Year - DateOfBirth.Value.Year - (DateOfBirth.Value.Month > today.Month || (DateOfBirth.Value.Month == today.Month && DateOfBirth.Value.Day > today.Day) ? 1 : 0);
        return age;
    }

    public string GetAgeGroup()
    {
        var age = GetAgeNearestBirthday();
        return age switch
        {
            <= 5 => "0-5",
            <= 11 => "6-11",
            _ => "12-18"
        };
    }

    public string GetAgeGroupDisplay()
    {
        var age = GetAge();
        var group = GetAgeGroup();
        return $"Age {age} (group: {group})";
    }
}
