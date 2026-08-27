using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurveyFormApp.Models;
using Microsoft.Data.SqlClient;

namespace SurveyFormApp.Controllers;

public class FormBuilderController : Controller
{
    private readonly SurveyDbContext _context;

    public FormBuilderController(SurveyDbContext context)
    {
        _context = context;
    }

    // GET: /FormBuilder
    public async Task<IActionResult> Index()
    {
        var forms = await _context.SurveyForms
            .Include(f => f.SurveyFormVersions)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

        return View(forms);
    }

    // GET: /FormBuilder/CreateForm
    public IActionResult CreateForm() => View();

    // POST: /FormBuilder/CreateForm
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateForm(string formCode, string formName, string? productType)
    {
        if (string.IsNullOrWhiteSpace(formCode) || string.IsNullOrWhiteSpace(formName))
        {
            ModelState.AddModelError("", "Kode dan Nama form wajib diisi.");
            return View();
        }

        var form = new SurveyForm
        {
            FormCode = formCode,
            FormName = formName,
            ProductType = productType,
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        _context.SurveyForms.Add(form);
        await _context.SaveChangesAsync();

        // langsung bikin versi draft pertama
        var version = new SurveyFormVersion
        {
            FormId = form.Id,
            VersionNo = 1,
            IsPublished = false,
            CreatedAt = DateTime.Now
        };
        _context.SurveyFormVersions.Add(version);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Manage), new { versionId = version.Id });
    }

    // GET: /FormBuilder/Versions/5
    public async Task<IActionResult> Versions(long formId)
    {
        var form = await _context.SurveyForms
            .Include(f => f.SurveyFormVersions)
            .FirstOrDefaultAsync(f => f.Id == formId);

        if (form == null) return NotFound();
        return View(form);
    }

    // POST: /FormBuilder/NewVersion  -> bikin versi baru dari form lama
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NewVersion(long formId)
    {
        var lastVersionNo = await _context.SurveyFormVersions
            .Where(v => v.FormId == formId)
            .Select(v => (int?)v.VersionNo)
            .MaxAsync() ?? 0;

        var version = new SurveyFormVersion
        {
            FormId = formId,
            VersionNo = lastVersionNo + 1,
            IsPublished = false,
            CreatedAt = DateTime.Now
        };
        _context.SurveyFormVersions.Add(version);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Manage), new { versionId = version.Id });
    }

// // POST: /FormBuilder/DeleteVersion (yang bisa apus semuanya walaupun masih ada isinya)
// [HttpPost]
// [ValidateAntiForgeryToken]
// public async Task<IActionResult> DeleteVersion(long versionId, long formId)
// {
//     var version = await _context.SurveyFormVersions
//         .FirstOrDefaultAsync(v => v.Id == versionId);

//     if (version == null) return NotFound();

//     if (version.IsPublished)
//     {
//         TempData["Error"] = "Versi yang sedang published tidak bisa dihapus. Publish versi lain dulu, atau unpublish versi ini.";
//         return RedirectToAction(nameof(Versions), new { formId });
//     }

//     await using var transaction = await _context.Database.BeginTransactionAsync();
//     try
//     {
//         // 1. Kumpulkan semua ID section & question di versi ini
//         var sectionIds = await _context.SurveySections
//             .Where(s => s.FormVersionId == versionId)
//             .Select(s => s.Id)
//             .ToListAsync();

//         var questionIds = await _context.SurveyQuestions
//             .Where(q => sectionIds.Contains(q.SectionId))
//             .Select(q => q.Id)
//             .ToListAsync();

//         // 2. Kumpulkan semua ID assignment & response di versi ini
//         var assignmentIds = await _context.SurveyAssignments
//             .Where(a => a.FormVersionId == versionId)
//             .Select(a => a.Id)
//             .ToListAsync();

//         var responseIds = await _context.SurveyResponses
//             .Where(r => assignmentIds.Contains(r.AssignmentId))
//             .Select(r => r.Id)
//             .ToListAsync();

//         // 3. Hapus data turunan response (paling dalam dulu)
//         _context.SurveyAnswers.RemoveRange(
//             _context.SurveyAnswers.Where(a => responseIds.Contains(a.ResponseId)));
//         _context.SurveyAnswerGroups.RemoveRange(
//             _context.SurveyAnswerGroups.Where(a => responseIds.Contains(a.ResponseId)));
//         _context.SurveyAttachments.RemoveRange(
//             _context.SurveyAttachments.Where(a => responseIds.Contains(a.ResponseId)));
//         _context.SurveyFraudFlags.RemoveRange(
//             _context.SurveyFraudFlags.Where(f => responseIds.Contains(f.ResponseId)));
//         _context.SurveyGeoValidations.RemoveRange(
//             _context.SurveyGeoValidations.Where(g => responseIds.Contains(g.ResponseId)));
//         _context.SurveyLocationLogs.RemoveRange(
//             _context.SurveyLocationLogs.Where(l => responseIds.Contains(l.ResponseId)));
//         _context.SurveyScores.RemoveRange(
//             _context.SurveyScores.Where(s => responseIds.Contains(s.ResponseId)));
//         await _context.SaveChangesAsync();

//         // 4. Hapus response, lalu assignment
//         _context.SurveyResponses.RemoveRange(
//             _context.SurveyResponses.Where(r => responseIds.Contains(r.Id)));
//         await _context.SaveChangesAsync();

//         _context.SurveyAssignments.RemoveRange(
//             _context.SurveyAssignments.Where(a => assignmentIds.Contains(a.Id)));
//         await _context.SaveChangesAsync();

//         // 5. Hapus data turunan question: jawaban/attachment sisa (kalau ada yang nyantol ke question tapi response-nya di luar versi ini), options, rules
//         _context.SurveyAnswers.RemoveRange(
//             _context.SurveyAnswers.Where(a => questionIds.Contains(a.QuestionId)));
//         _context.SurveyAttachments.RemoveRange(
//             _context.SurveyAttachments.Where(a => a.QuestionId != null && questionIds.Contains(a.QuestionId.Value)));
//         _context.SurveyQuestionOptions.RemoveRange(
//             _context.SurveyQuestionOptions.Where(o => questionIds.Contains(o.QuestionId)));
//         _context.SurveyQuestionRules.RemoveRange(
//             _context.SurveyQuestionRules.Where(r => questionIds.Contains(r.QuestionId) || questionIds.Contains(r.DependsOnQuestionId)));
//         await _context.SaveChangesAsync();

//         // 6. Hapus question, lalu group, lalu section
//         _context.SurveyQuestions.RemoveRange(
//             _context.SurveyQuestions.Where(q => questionIds.Contains(q.Id)));
//         await _context.SaveChangesAsync();

//         _context.SurveyQuestionGroups.RemoveRange(
//             _context.SurveyQuestionGroups.Where(g => sectionIds.Contains(g.SectionId)));
//         await _context.SaveChangesAsync();

//         _context.SurveySections.RemoveRange(
//             _context.SurveySections.Where(s => sectionIds.Contains(s.Id)));
//         await _context.SaveChangesAsync();

//         // 7. Terakhir, hapus versinya sendiri
//         _context.SurveyFormVersions.Remove(version);
//         await _context.SaveChangesAsync();

//         await transaction.CommitAsync();
//         TempData["Success"] = "Versi beserta seluruh data terkait berhasil dihapus.";
//     }
//     catch (Exception)
//     {
//         await transaction.RollbackAsync();
//         TempData["Error"] = "Gagal menghapus versi. Terjadi kesalahan saat menghapus data terkait.";
//     }

//     return RedirectToAction(nameof(Versions), new { formId });
// }

    // POST: /FormBuilder/DeleteVersion (yang gak bisa didelete kalau masih ada isi)
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteVersion(long versionId, long formId)
{
    var version = await _context.SurveyFormVersions
        .FirstOrDefaultAsync(v => v.Id == versionId);

    if (version == null) return NotFound();

    if (version.IsPublished)
    {
        TempData["Error"] = "Versi yang sedang published tidak bisa dihapus. Publish versi lain dulu, atau unpublish versi ini.";
        return RedirectToAction(nameof(Versions), new { formId });
    }

    try
    {
        _context.SurveyFormVersions.Remove(version);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Versi berhasil dihapus.";
    }
    catch (DbUpdateException)
    {
        TempData["Error"] = "Versi ini tidak bisa dihapus karena masih punya data terkait (section/pertanyaan/jawaban survey). Hapus data terkait dulu sebelum menghapus versi.";
    }

    return RedirectToAction(nameof(Versions), new { formId });
}

    // GET: /FormBuilder/Manage/5 (versionId)
    public async Task<IActionResult> Manage(long versionId)
    {
        var version = await _context.SurveyFormVersions
            .Include(v => v.Form)
            .Include(v => v.SurveySections)
                .ThenInclude(s => s.SurveyQuestions.OrderBy(q => q.OrderNo))
                    .ThenInclude(q => q.SurveyQuestionOptions)
            .FirstOrDefaultAsync(v => v.Id == versionId);

        if (version == null) return NotFound();
        return View(version);
    }

    // POST: /FormBuilder/AddSection
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSection(long versionId, string sectionCode, string sectionTitle, int orderNo)
    {
        _context.SurveySections.Add(new SurveySection
        {
            FormVersionId = versionId,
            SectionCode = sectionCode,
            SectionTitle = sectionTitle,
            OrderNo = orderNo,
            CreatedAt = DateTime.Now
        });
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Manage), new { versionId });
    }

    // POST: /FormBuilder/DeleteSection
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSection(long sectionId, long versionId)
    {
        var section = await _context.SurveySections.FindAsync(sectionId);
        if (section != null)
        {
            _context.SurveySections.Remove(section);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Manage), new { versionId });
    }

    // POST: /FormBuilder/AddQuestion
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> AddQuestion(
    long sectionId, long versionId,
    string questionCode, string questionText, string questionType,
    bool isRequired, int orderNo,
    string? placeholder, string? helpText,
    decimal? minValue, decimal? maxValue, int? maxLength,
    string? validationRegex,
    string? optionsRaw)
{
    // cek kode unik dulu sebelum insert
    var codeExists = await _context.SurveyQuestions
        .AnyAsync(q => q.QuestionCode == questionCode);

    if (codeExists)
    {
        TempData["Error"] = $"Kode pertanyaan '{questionCode}' sudah dipakai pertanyaan lain. Pakai kode lain yang belum ada.";
        return RedirectToAction(nameof(Manage), new { versionId });
    }

    var question = new SurveyQuestion
    {
        SectionId = sectionId,
        QuestionCode = questionCode,
        QuestionText = questionText,
        QuestionType = questionType,
        IsRequired = isRequired,
        OrderNo = orderNo,
        Placeholder = placeholder,
        HelpText = helpText,
        MinValue = minValue,
        MaxValue = maxValue,
        MaxLength = maxLength,
        ValidationRegex = validationRegex,
        CreatedAt = DateTime.Now
    };
    _context.SurveyQuestions.Add(question);
    await _context.SaveChangesAsync();

        if (questionType.Equals("dropdown", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(optionsRaw))
        {
            var pairs = optionsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries);
            int order = 0;
            foreach (var pair in pairs)
            {
                var parts = pair.Split(':', 2);
                var label = parts[0].Trim();
                var value = parts.Length > 1 ? parts[1].Trim() : label;

                _context.SurveyQuestionOptions.Add(new SurveyQuestionOption
                {
                    QuestionId = question.Id,
                    OptionLabel = label,
                    OptionValue = value,
                    OrderNo = order++,
                    IsDefault = false,
                    CreatedAt = DateTime.Now
                });
            }
            await _context.SaveChangesAsync();
        }

        TempData["Success"] = "Pertanyaan berhasil ditambahkan.";
        return RedirectToAction(nameof(Manage), new { versionId });
    }

        // GET: /FormBuilder/EditQuestion/5
    public async Task<IActionResult> EditQuestion(long id)
    {
        var question = await _context.SurveyQuestions
            .Include(q => q.SurveyQuestionOptions)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question == null) return NotFound();

        ViewBag.VersionId = (await _context.SurveySections
            .Where(s => s.Id == question.SectionId)
            .Select(s => s.FormVersionId)
            .FirstOrDefaultAsync());

        ViewBag.OptionsRaw = string.Join(", ",
            question.SurveyQuestionOptions
                .OrderBy(o => o.OrderNo)
                .Select(o => $"{o.OptionLabel}:{o.OptionValue}"));

        return View(question);
    }

    // POST: /FormBuilder/EditQuestion
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> EditQuestion(
    long id, long versionId,
    string questionCode, string questionText, string questionType,
    bool isRequired, int orderNo,
    string? placeholder, string? helpText,
    decimal? minValue, decimal? maxValue, int? maxLength,
    string? validationRegex,
    string? optionsRaw)
{
    var question = await _context.SurveyQuestions
        .Include(q => q.SurveyQuestionOptions)
        .FirstOrDefaultAsync(q => q.Id == id);

    if (question == null) return NotFound();

    var codeExists = await _context.SurveyQuestions
        .AnyAsync(q => q.QuestionCode == questionCode && q.Id != id);

    if (codeExists)
    {
        TempData["Error"] = $"Kode pertanyaan '{questionCode}' sudah dipakai pertanyaan lain.";
        return RedirectToAction(nameof(EditQuestion), new { id });
    }

    question.QuestionCode = questionCode;
    question.QuestionText = questionText;
    question.QuestionType = questionType;
    question.IsRequired = isRequired;
    question.OrderNo = orderNo;
    question.Placeholder = placeholder;
    question.HelpText = helpText;
    question.MinValue = minValue;
    question.MaxValue = maxValue;
    question.MaxLength = maxLength;
    question.ValidationRegex = validationRegex;

        _context.SurveyQuestionOptions.RemoveRange(question.SurveyQuestionOptions);

        if (questionType.Equals("dropdown", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(optionsRaw))
        {
            var pairs = optionsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries);
            int order = 0;
            foreach (var pair in pairs)
            {
                var parts = pair.Split(':', 2);
                var label = parts[0].Trim();
                var value = parts.Length > 1 ? parts[1].Trim() : label;

                _context.SurveyQuestionOptions.Add(new SurveyQuestionOption
                {
                    QuestionId = question.Id,
                    OptionLabel = label,
                    OptionValue = value,
                    OrderNo = order++,
                    IsDefault = false,
                    CreatedAt = DateTime.Now
                });
            }
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "Pertanyaan berhasil diperbarui.";
        return RedirectToAction(nameof(Manage), new { versionId });
    }

    // POST: /FormBuilder/DeleteQuestion
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuestion(long questionId, long versionId)
    {
        var question = await _context.SurveyQuestions.FindAsync(questionId);
        if (question != null)
        {
            _context.SurveyQuestions.Remove(question);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Manage), new { versionId });
    }

    // POST: /FormBuilder/Publish
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(long versionId)
    {
        var version = await _context.SurveyFormVersions
            .FirstOrDefaultAsync(v => v.Id == versionId);
        if (version == null) return NotFound();

        // matiin publish di versi lain milik form yang sama
        var others = await _context.SurveyFormVersions
            .Where(v => v.FormId == version.FormId && v.Id != versionId)
            .ToListAsync();
        foreach (var v in others) v.IsPublished = false;

        version.IsPublished = true;
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Versions), new { formId = version.FormId });
    }
}