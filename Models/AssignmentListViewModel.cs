using System;
using System.Collections.Generic;

namespace SurveyFormApp.Models;

public class AssignmentListViewModel
{
    public List<SurveyAssignment> Items { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    // Filter yang lagi aktif
    public string? FilterStatus { get; set; }
    public string? FilterSurveyorId { get; set; }
}