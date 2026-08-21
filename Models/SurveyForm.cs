using System;
using System.Collections.Generic;

namespace SurveyFormApp.Models;

public partial class SurveyForm
{
    public long Id { get; set; }

    public string FormCode { get; set; } = null!;

    public string FormName { get; set; } = null!;

    public string? ProductType { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<SurveyFormVersion> SurveyFormVersions { get; set; } = new List<SurveyFormVersion>();
}
