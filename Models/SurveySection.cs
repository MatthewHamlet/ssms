using System;
using System.Collections.Generic;

namespace SurveyFormApp.Models;

public partial class SurveySection
{
    public long Id { get; set; }

    public long FormVersionId { get; set; }

    public string SectionCode { get; set; } = null!;

    public string SectionTitle { get; set; } = null!;

    public int OrderNo { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? SurveyFormId { get; set; }

    public virtual SurveyFormVersion FormVersion { get; set; } = null!;

    public virtual ICollection<SurveyQuestionGroup> SurveyQuestionGroups { get; set; } = new List<SurveyQuestionGroup>();

    public virtual ICollection<SurveyQuestion> SurveyQuestions { get; set; } = new List<SurveyQuestion>();
}
