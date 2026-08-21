using System;
using System.Collections.Generic;

namespace SurveyFormApp.Models;

public partial class SurveyGeoValidation
{
    public long Id { get; set; }

    public long ResponseId { get; set; }

    public decimal SurveyLatitude { get; set; }

    public decimal SurveyLongitude { get; set; }

    public decimal DebtorLatitude { get; set; }

    public decimal DebtorLongitude { get; set; }

    public decimal DistanceMeters { get; set; }

    public bool IsValid { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual SurveyResponse Response { get; set; } = null!;
}
