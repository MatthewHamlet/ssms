using System;
using System.Collections.Generic;

namespace SurveyFormApp.Models;

public partial class SurveyFormVersion
{
    public long Id { get; set; }

    public long FormId { get; set; }

    public int VersionNo { get; set; }

    public bool IsPublished { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual SurveyForm Form { get; set; } = null!;

    public virtual ICollection<SurveyAssignment> SurveyAssignments { get; set; } = new List<SurveyAssignment>();

    public virtual ICollection<SurveySection> SurveySections { get; set; } = new List<SurveySection>();
}
