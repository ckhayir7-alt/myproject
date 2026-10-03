namespace WRMS.Web.Models.Shared;

public class EmptyStateViewModel
{
    public string Icon { get; set; } = "bi-inbox";
    public string Title { get; set; } = "No records found";
    public string Message { get; set; } = "There is nothing to display yet.";
    public string? ActionText { get; set; }
    public string? ActionUrl { get; set; }
}
