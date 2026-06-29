using System.ComponentModel.DataAnnotations;
using ToDoApi.Dtos;
using ToDoApi.Models;

namespace ToDoApi.Tests;

/// <summary>
/// Unit tests over the task request-DTO validation rules and the DTO &lt;-&gt; entity
/// mapping (the deliverables of Phase 3 issue #15). These assert the rules and the
/// trimming directly; the HTTP 400 + ProblemDetails wrapping that [ApiController] adds
/// once the endpoints exist is exercised separately by the integration tests.
/// </summary>
public class TaskDtoValidationTests
{
    private static IList<ValidationResult> Validate(object dto)
    {
        List<ValidationResult> results = [];
        Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);
        return results;
    }

    private static bool FailedFor(IList<ValidationResult> results, string member) =>
        results.Any(r => r.MemberNames.Contains(member));

    private static CreateTaskRequest ValidCreate() => new()
    {
        Title = "Buy milk",
        Description = "2% organic",
        DueDate = new DateOnly(2026, 7, 1),
    };

    [Fact]
    public void Valid_create_request_passes()
    {
        Assert.Empty(Validate(ValidCreate()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_or_whitespace_title_is_rejected(string? title)
    {
        CreateTaskRequest dto = ValidCreate();
        dto.Title = title;

        Assert.True(FailedFor(Validate(dto), nameof(CreateTaskRequest.Title)));
    }

    [Fact]
    public void Title_longer_than_200_is_rejected()
    {
        CreateTaskRequest dto = ValidCreate();
        dto.Title = new string('a', 201);

        Assert.True(FailedFor(Validate(dto), nameof(CreateTaskRequest.Title)));
    }

    [Fact]
    public void Title_of_exactly_200_is_allowed()
    {
        CreateTaskRequest dto = ValidCreate();
        dto.Title = new string('a', 200);

        Assert.False(FailedFor(Validate(dto), nameof(CreateTaskRequest.Title)));
    }

    [Fact]
    public void Missing_due_date_is_rejected()
    {
        CreateTaskRequest dto = ValidCreate();
        dto.DueDate = null;

        Assert.True(FailedFor(Validate(dto), nameof(CreateTaskRequest.DueDate)));
    }

    [Fact]
    public void Past_due_date_is_allowed()
    {
        CreateTaskRequest dto = ValidCreate();
        dto.DueDate = new DateOnly(2000, 1, 1);

        Assert.Empty(Validate(dto));
    }

    [Fact]
    public void Null_description_is_allowed()
    {
        CreateTaskRequest dto = ValidCreate();
        dto.Description = null;

        Assert.Empty(Validate(dto));
    }

    [Fact]
    public void Description_longer_than_2000_is_rejected()
    {
        CreateTaskRequest dto = ValidCreate();
        dto.Description = new string('a', 2001);

        Assert.True(FailedFor(Validate(dto), nameof(CreateTaskRequest.Description)));
    }

    [Fact]
    public void Update_request_enforces_the_same_required_rules()
    {
        UpdateTaskRequest dto = new() { Title = "   ", DueDate = null, IsCompleted = true };

        IList<ValidationResult> results = Validate(dto);

        Assert.True(FailedFor(results, nameof(UpdateTaskRequest.Title)));
        Assert.True(FailedFor(results, nameof(UpdateTaskRequest.DueDate)));
    }

    [Fact]
    public void ToEntity_trims_title_and_starts_incomplete()
    {
        CreateTaskRequest dto = ValidCreate();
        dto.Title = "  Buy milk  ";

        TaskItem entity = dto.ToEntity();

        Assert.Equal("Buy milk", entity.Title);
        Assert.Equal(dto.Description, entity.Description);
        Assert.Equal(dto.DueDate!.Value, entity.DueDate);
        Assert.False(entity.IsCompleted);
    }

    [Fact]
    public void ApplyTo_overwrites_editable_fields_and_trims_title()
    {
        TaskItem entity = new()
        {
            Title = "old title",
            Description = "old",
            DueDate = new DateOnly(2026, 1, 1),
            IsCompleted = false,
        };
        UpdateTaskRequest dto = new()
        {
            Title = "  new title  ",
            Description = "new",
            DueDate = new DateOnly(2026, 2, 2),
            IsCompleted = true,
        };

        dto.ApplyTo(entity);

        Assert.Equal("new title", entity.Title);
        Assert.Equal("new", entity.Description);
        Assert.Equal(new DateOnly(2026, 2, 2), entity.DueDate);
        Assert.True(entity.IsCompleted);
    }

    [Fact]
    public void ToResponse_copies_all_fields()
    {
        TaskItem entity = new()
        {
            Id = 42,
            Title = "Buy milk",
            Description = "2%",
            DueDate = new DateOnly(2026, 7, 1),
            IsCompleted = true,
            CreatedAtUtc = new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero),
            UpdatedAtUtc = new DateTimeOffset(2026, 6, 2, 9, 0, 0, TimeSpan.Zero),
            UserId = 7,
        };

        TaskResponse response = entity.ToResponse();

        Assert.Equal(entity.Id, response.Id);
        Assert.Equal(entity.Title, response.Title);
        Assert.Equal(entity.Description, response.Description);
        Assert.Equal(entity.DueDate, response.DueDate);
        Assert.Equal(entity.IsCompleted, response.IsCompleted);
        Assert.Equal(entity.CreatedAtUtc, response.CreatedAtUtc);
        Assert.Equal(entity.UpdatedAtUtc, response.UpdatedAtUtc);
    }
}
