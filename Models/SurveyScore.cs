using System;
using System.Collections.Generic;

namespace SurveyFormApp.Models;

public partial class SurveyScore
{
    public long Id { get; set; }

    public long ResponseId { get; set; }

    public decimal? ScoreTotal { get; set; }

    public decimal? ScoreHousing { get; set; }

    public decimal? ScoreEnvironment { get; set; }

    public decimal? ScoreIncome { get; set; }

    public DateTime CalculatedAt { get; set; }

    public virtual SurveyResponse Response { get; set; } = null!;
}
