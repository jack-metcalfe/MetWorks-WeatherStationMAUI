namespace InterfaceDefinition;
public interface IValidationIssue
{
    public string? Message { get; set; }
    public string? InstanceLocation { get; set; }
    public string? SchemaLocation { get; set; }
    public string? Keyword { get; set; }
    // Optional parent reference for upward traversal (if needed)
    public IValidationIssue? Parent { get; set; }
    // Child issues for nested schema evaluations
    public List<IValidationIssue> Children { get; set; }
    public bool HasChildren => Children.Count > 0;
    public bool HasError { get; set; }
    public string ToString();
}
