namespace Interfaces;
public interface IValidationResult
{
    public bool IsValid { get; set; }
    public List<IValidationIssue> Issues { get; set; }
}
