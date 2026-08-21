using System;
using System.Collections.Generic;

namespace SurveyFormApp.Models;

public partial class SurveyAssignment
{
    public long Id { get; set; }

    public string ApplicationId { get; set; } = null!;

    public string? SurveyorId { get; set; }

    public long FormVersionId { get; set; }

    public string Status { get; set; } = null!;

    public string? BranchId { get; set; }

    public int? Priority { get; set; }

    public DateTime AssignedAt { get; set; }

    public DateTime? DueDate { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? CanceledAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? TakenBySurveyorId { get; set; }

    public DateTime? TakenAt { get; set; }

    public virtual SurveyFormVersion FormVersion { get; set; } = null!;

    public virtual ICollection<SurveyResponse> SurveyResponses { get; set; } = new List<SurveyResponse>();
}
