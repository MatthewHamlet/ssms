using System;
using System.Collections.Generic;

namespace SurveyFormApp.Models;

public partial class SurveyQuestionGroup
{
    public long Id { get; set; }

    public long SectionId { get; set; }

    public string GroupCode { get; set; } = null!;

    public string GroupLabel { get; set; } = null!;

    public bool IsRepeatable { get; set; }

    public int OrderNo { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? SurveyFormId { get; set; }

    public virtual SurveySection Section { get; set; } = null!;

    public virtual ICollection<SurveyAnswerGroup> SurveyAnswerGroups { get; set; } = new List<SurveyAnswerGroup>();

    public virtual ICollection<SurveyQuestion> SurveyQuestions { get; set; } = new List<SurveyQuestion>();
}
