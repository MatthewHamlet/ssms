using System;
using System.Collections.Generic;

namespace SurveyFormApp.Models;

public partial class SurveyAnswerGroup
{
    public long Id { get; set; }

    public long ResponseId { get; set; }

    public long GroupId { get; set; }

    public int SequenceNo { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual SurveyQuestionGroup Group { get; set; } = null!;

    public virtual SurveyResponse Response { get; set; } = null!;

    public virtual ICollection<SurveyAnswer> SurveyAnswers { get; set; } = new List<SurveyAnswer>();
}
