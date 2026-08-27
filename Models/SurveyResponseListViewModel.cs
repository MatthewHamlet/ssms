using System;
using System.Collections.Generic;

namespace SurveyFormApp.Models;

public class SurveyResponseListViewModel
{
    public List<SurveyResponse> Items { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    // Filter yang lagi aktif (buat di-bind balik ke form filter)
    public long? FilterFormId { get; set; }
    public string? FilterStatus { get; set; }
    public string? FilterSurveyorName { get; set; }
    public DateTime? FilterDateFrom { get; set; }
    public DateTime? FilterDateTo { get; set; }

    // Data buat isi dropdown filter form
    public List<SurveyForm> AvailableForms { get; set; } = new();
}