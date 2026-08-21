using System;
using System.Collections.Generic;

namespace SurveyFormApp.Models;

public partial class SurveyAttachment
{
    public long Id { get; set; }

    public long ResponseId { get; set; }

    public long? QuestionId { get; set; }

    public string FileName { get; set; } = null!;

    public string FileUrl { get; set; } = null!;

    public string? FileType { get; set; }

    public long? FileSize { get; set; }

    public string? Checksum { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public DateTime? TakenAt { get; set; }

    public DateTime UploadedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual SurveyQuestion? Question { get; set; }

    public virtual SurveyResponse Response { get; set; } = null!;
}
