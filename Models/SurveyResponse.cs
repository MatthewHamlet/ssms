using System;
using System.Collections.Generic;

namespace SurveyFormApp.Models;

public partial class SurveyResponse
{
    public long Id { get; set; }

    public long AssignmentId { get; set; }

    public string SurveyorId { get; set; } = null!;

    public DateTime StartedAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public string Status { get; set; } = null!;

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public string? DeviceId { get; set; }

    public string? AppVersion { get; set; }

    public string? SyncId { get; set; }

    public int? DurationSeconds { get; set; }

    public DateTime CreatedAt { get; set; }

    public long? FormVersionId { get; set; }

    public virtual SurveyAssignment Assignment { get; set; } = null!;

    public virtual ICollection<SurveyAnswerGroup> SurveyAnswerGroups { get; set; } = new List<SurveyAnswerGroup>();

    public virtual ICollection<SurveyAnswer> SurveyAnswers { get; set; } = new List<SurveyAnswer>();

    public virtual ICollection<SurveyAttachment> SurveyAttachments { get; set; } = new List<SurveyAttachment>();

    public virtual ICollection<SurveyFraudFlag> SurveyFraudFlags { get; set; } = new List<SurveyFraudFlag>();

    public virtual SurveyGeoValidation? SurveyGeoValidation { get; set; }

    public virtual ICollection<SurveyLocationLog> SurveyLocationLogs { get; set; } = new List<SurveyLocationLog>();

    public virtual SurveyScore? SurveyScore { get; set; }
}
