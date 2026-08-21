using System;
using System.Collections.Generic;

namespace SurveyFormApp.Models;

public partial class SurveyFraudFlag
{
    public long Id { get; set; }

    public long ResponseId { get; set; }

    public string FlagCode { get; set; } = null!;

    public string? Description { get; set; }

    public int Severity { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual SurveyResponse Response { get; set; } = null!;
}
