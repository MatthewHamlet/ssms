using System;
using System.Collections.Generic;

namespace SurveyFormApp.Models;

public partial class SurveyQuestion
{
    public long Id { get; set; }

    public long? GroupId { get; set; }

    public long SectionId { get; set; }

    public string QuestionCode { get; set; } = null!;

    public string QuestionText { get; set; } = null!;

    public string QuestionType { get; set; } = null!;

    public bool IsRequired { get; set; }

    public int OrderNo { get; set; }

    public string? Placeholder { get; set; }

    public string? HelpText { get; set; }

    public string? ValidationRegex { get; set; }

    public decimal? MinValue { get; set; }

    public decimal? MaxValue { get; set; }

    public string? InputMask { get; set; }

    public string? UnitLabel { get; set; }

    public string? DefaultValue { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? MaxLength { get; set; }

    public int? SurveyFormId { get; set; }

    public virtual SurveyQuestionGroup? Group { get; set; }

    public virtual SurveySection Section { get; set; } = null!;

    public virtual ICollection<SurveyAnswer> SurveyAnswers { get; set; } = new List<SurveyAnswer>();

    public virtual ICollection<SurveyAttachment> SurveyAttachments { get; set; } = new List<SurveyAttachment>();

    public virtual ICollection<SurveyQuestionOption> SurveyQuestionOptions { get; set; } = new List<SurveyQuestionOption>();

    public virtual ICollection<SurveyQuestionRule> SurveyQuestionRuleDependsOnQuestions { get; set; } = new List<SurveyQuestionRule>();

    public virtual ICollection<SurveyQuestionRule> SurveyQuestionRuleQuestions { get; set; } = new List<SurveyQuestionRule>();
}
