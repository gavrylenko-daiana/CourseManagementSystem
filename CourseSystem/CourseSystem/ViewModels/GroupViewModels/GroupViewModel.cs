using Core.Models;

namespace UI.ViewModels;

public class GroupViewModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public double Progress { get; set; }
}