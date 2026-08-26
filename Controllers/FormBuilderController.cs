using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurveyFormApp.Models;

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
        string? optionsRaw)
    {
        var question = await _context.SurveyQuestions
            .Include(q => q.SurveyQuestionOptions)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question == null) return NotFound();

        // cek kode unik, tapi kecualiin diri sendiri
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