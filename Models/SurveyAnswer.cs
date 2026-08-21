using System;
using System.Collections.Generic;

namespace SurveyFormApp.Models;

public partial class SurveyAnswer
{
    public long Id { get; set; }

    public long ResponseId { get; set; }

    public long QuestionId { get; set; }

    public long? AnswerGroupId { get; set; }

    public string? ValueText { get; set; }

    public decimal? ValueNumber { get; set; }

    public DateTime? ValueDate { get; set; }

    public bool? ValueBoolean { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual SurveyAnswerGroup? AnswerGroup { get; set; }

    public virtual SurveyQuestion Question { get; set; } = null!;

    public virtual SurveyResponse Response { get; set; } = null!;
}
