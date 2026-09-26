namespace Orquestra.Domain.Entities;

public sealed class Organization
{
    private Organization() { }
    public Organization(string name, string slug, DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        Name = name;
        Slug = slug;
        CreatedAt = now;
    }
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
}
