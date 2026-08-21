using System;
using System.Collections.Generic;

namespace SurveyFormApp.Models;

public partial class SurveyQuestionRule
{
    public long Id { get; set; }

    public long QuestionId { get; set; }

    public long DependsOnQuestionId { get; set; }

    public string Operator { get; set; } = null!;

    public string Value { get; set; } = null!;

    public string Action { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public int? SurveyFormId { get; set; }

    public virtual SurveyQuestion DependsOnQuestion { get; set; } = null!;

    public virtual SurveyQuestion Question { get; set; } = null!;
}
