using System;
using System.Collections.Generic;

namespace SurveyFormApp.Models;

public partial class SurveyLocationLog
{
    public long Id { get; set; }

    public long ResponseId { get; set; }

    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }

    public DateTime RecordedAt { get; set; }

    public virtual SurveyResponse Response { get; set; } = null!;
}
