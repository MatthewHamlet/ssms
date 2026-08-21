using System;
using System.Collections.Generic;

namespace SurveyFormApp.Models;

public partial class SurveyQuestionOption
{
    public long Id { get; set; }

    public long QuestionId { get; set; }

    public string OptionLabel { get; set; } = null!;

    public string OptionValue { get; set; } = null!;

    public int OrderNo { get; set; }

    public bool IsDefault { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? SurveyFormId { get; set; }

    public virtual SurveyQuestion Question { get; set; } = null!;
}
