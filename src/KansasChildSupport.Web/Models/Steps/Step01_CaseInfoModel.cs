using System.ComponentModel.DataAnnotations;

namespace KansasChildSupport.Web.Models.Steps;

public class Step01_CaseInfoModel
{
    public string? CaseNumber { get; set; }

    [Required(ErrorMessage = "Please enter your name.")]
    public string Party1Name { get; set; } = "";

    [Required(ErrorMessage = "Please enter the other parent's name.")]
    public string Party2Name { get; set; } = "";

    [Required(ErrorMessage = "Please select your county.")]
    public string County { get; set; } = "";

    [Required(ErrorMessage = "Please select who has primary custody.")]
    public string PrimaryCustody { get; set; } = ""; // "Party1" or "Party2"

    public static List<string> KansasCounties => new()
    {
        "Allen", "Anderson", "Atchison", "Barber", "Barton", "Bourbon", "Brown", "Butler",
        "Chase", "Chautauqua", "Cherokee", "Cheyenne", "Clark", "Clay", "Cloud", "Coffey",
        "Comanche", "Cowley", "Crawford", "Decatur", "Dickinson", "Doniphan", "Douglas",
        "Edwards", "Elk", "Ellis", "Ellsworth", "Finney", "Ford", "Franklin", "Geary",
        "Gove", "Graham", "Grant", "Gray", "Greeley", "Greenwood", "Hamilton", "Harper",
        "Harvey", "Haskell", "Hodgeman", "Jackson", "Jefferson", "Jewell", "Johnson",
        "Kearny", "Kingman", "Kiowa", "Labette", "Lane", "Leavenworth", "Lincoln", "Linn",
        "Logan", "Lyon", "Marion", "Marshall", "McPherson", "Meade", "Miami", "Mitchell",
        "Montgomery", "Morris", "Morton", "Nemaha", "Neosho", "Ness", "Norton", "Osage",
        "Osborne", "Ottawa", "Pawnee", "Phillips", "Pottawatomie", "Pratt", "Rawlins",
        "Reno", "Republic", "Rice", "Riley", "Rooks", "Rush", "Russell", "Saline",
        "Scott", "Sedgwick", "Seward", "Shawnee", "Sheridan", "Sherman", "Smith",
        "Stafford", "Stanton", "Stevens", "Sumner", "Thomas", "Trego", "Wabaunsee",
        "Wallace", "Washington", "Wichita", "Wilson", "Woodson", "Wyandotte"
    };
}
