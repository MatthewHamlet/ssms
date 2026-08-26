using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurveyFormApp.Models;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace SurveyFormApp.Controllers;

public class SurveyController : Controller
{
    private readonly SurveyDbContext _context;

    public SurveyController(SurveyDbContext context)
    {
        _context = context;
    }

public async Task<IActionResult> Index()
{
    var forms = await _context.SurveyForms
        .Where(f => f.IsActive && f.SurveyFormVersions.Any(v => v.IsPublished))
        .ToListAsync();

    return View(forms);
}

    public async Task<IActionResult> Fill(long id)
    {
        var version = await _context.SurveyFormVersions
            .Where(v => v.FormId == id && v.IsPublished)
            .OrderByDescending(v => v.VersionNo)
            .Include(v => v.SurveySections)
                .ThenInclude(s => s.SurveyQuestions.OrderBy(q => q.OrderNo))
                    .ThenInclude(q => q.SurveyQuestionOptions)
            .Include(v => v.SurveySections)
                .ThenInclude(s => s.SurveyQuestions)
                    .ThenInclude(q => q.SurveyQuestionRuleQuestions)
            .FirstOrDefaultAsync();

        if (version == null)
        {
            return NotFound("Form ini belum punya versi yang di-publish.");
        }

        return View(version);
    }

    // POST: /Survey/Fill  -> simpan jawaban
[HttpPost]
public async Task<IActionResult> Fill(
    long formVersionId,
    Dictionary<long, string> answers,
    Dictionary<long, IFormFile> photos,
    string? surveyorName,
    decimal? latitude,
    decimal? longitude,
    string? deviceId,
    string? appVersion,
    int? durationSeconds)
{
    // Sementara belum ada login surveyor, simpan nama yang diisi manual di form
    // (atau reuse dari session kalau surveyor buka form lagi tanpa isi ulang)
    var surveyorId = !string.IsNullOrWhiteSpace(surveyorName)
        ? surveyorName.Trim()
        : (HttpContext.Session.GetString("SurveyorId") ?? "anonymous");

    if (!string.IsNullOrWhiteSpace(surveyorName))
        HttpContext.Session.SetString("SurveyorId", surveyorId);

    var assignment = new SurveyAssignment
    {
        ApplicationId = Guid.NewGuid().ToString(),
        FormVersionId = formVersionId,
        AssignedAt = DateTime.Now
    };
    _context.SurveyAssignments.Add(assignment);
    await _context.SaveChangesAsync();

    var response = new SurveyResponse
    {
        AssignmentId = assignment.Id,
        SurveyorId = surveyorId,
        StartedAt = DateTime.Now,
        SubmittedAt = DateTime.Now,
        Status = "SUBMITTED",
        FormVersionId = formVersionId,
        Latitude = latitude,
        Longitude = longitude,
        DeviceId = deviceId,
        AppVersion = appVersion,
        DurationSeconds = durationSeconds
    };
    _context.SurveyResponses.Add(response);
    await _context.SaveChangesAsync();

    // Ambil QuestionType biar tahu jawaban ini harus masuk ke kolom mana
    var questionIds = answers?.Keys.ToList() ?? new List<long>();
    var questionTypes = await _context.SurveyQuestions
        .Where(q => questionIds.Contains(q.Id))
        .ToDictionaryAsync(q => q.Id, q => (q.QuestionType ?? "text").ToLower());

    if (answers != null)
    {
        foreach (var kv in answers)
        {
            if (string.IsNullOrWhiteSpace(kv.Value)) continue;

            var answer = new SurveyAnswer
            {
                ResponseId = response.Id,
                QuestionId = kv.Key
            };

            var type = questionTypes.TryGetValue(kv.Key, out var t) ? t : "text";

            switch (type)
            {
                case "number":
                    if (decimal.TryParse(kv.Value, out var numVal))
                        answer.ValueNumber = numVal;
                    else
                        answer.ValueText = kv.Value; // fallback biar gak hilang datanya
                    break;

                case "date":
                    if (DateTime.TryParse(kv.Value, out var dateVal))
                        answer.ValueDate = dateVal;
                    else
                        answer.ValueText = kv.Value;
                    break;

                case "boolean":
                    if (bool.TryParse(kv.Value, out var boolVal))
                        answer.ValueBoolean = boolVal;
                    else
                        answer.ValueText = kv.Value;
                    break;

                default: // text, textarea, dropdown, dll
                    answer.ValueText = kv.Value;
                    break;
            }

            _context.SurveyAnswers.Add(answer);
        }
    }

    // Handle upload foto
    if (photos != null && photos.Count > 0)
    {
        var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "survey-attachments");
        Directory.CreateDirectory(uploadsRoot);

        foreach (var kv in photos)
        {
            var file = kv.Value;
            if (file == null || file.Length == 0) continue;

            var ext = Path.GetExtension(file.FileName);
            var safeFileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(uploadsRoot, safeFileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            _context.SurveyAttachments.Add(new SurveyAttachment
            {
                ResponseId = response.Id,
                QuestionId = kv.Key,
                FileName = file.FileName,
                FileUrl = $"/uploads/survey-attachments/{safeFileName}",
                FileType = file.ContentType,
                FileSize = file.Length,
                UploadedAt = DateTime.Now
            });
        }
    }

    await _context.SaveChangesAsync();

    return RedirectToAction("ThankYou");
}

    public IActionResult ThankYou() => View();

    // GET: /Survey/Responses  -> daftar semua jawaban yang udah disubmit
    public async Task<IActionResult> Responses()
    {
        var responses = await _context.SurveyResponses
            .Include(r => r.Assignment)
                .ThenInclude(a => a.FormVersion)
                    .ThenInclude(v => v.Form)
            .OrderByDescending(r => r.SubmittedAt)
            .ToListAsync();

        return View(responses);
    }

        // GET: /Survey/EditAnswer/5
    public async Task<IActionResult> EditAnswer(long id)
    {
        var answer = await _context.SurveyAnswers
            .Include(a => a.Question)
                .ThenInclude(q => q.SurveyQuestionOptions)
            .Include(a => a.Response)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (answer == null) return NotFound();

        return View(answer);
    }

    // POST: /Survey/EditAnswer
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAnswer(long id, string? valueText, decimal? valueNumber, DateTime? valueDate, bool? valueBoolean)
    {
        var answer = await _context.SurveyAnswers
            .Include(a => a.Question)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (answer == null) return NotFound();

        var type = answer.Question.QuestionType?.ToLower();

        switch (type)
        {
            case "number":
                answer.ValueNumber = valueNumber;
                break;
            case "date":
                answer.ValueDate = valueDate;
                break;
            case "boolean":
                answer.ValueBoolean = valueBoolean;
                break;
            default: // text, textarea, dropdown, dll
                answer.ValueText = valueText;
                break;
        }

        await _context.SaveChangesAsync();

        return RedirectToAction("ResponseDetail", new { id = answer.ResponseId });
    }

    // POST: /Survey/DeleteAnswer
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAnswer(long id, long responseId)
    {
        var answer = await _context.SurveyAnswers.FindAsync(id);
        if (answer != null)
        {
            _context.SurveyAnswers.Remove(answer);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("ResponseDetail", new { id = responseId });
    }

    // GET: /Survey/ResponseDetail/5  -> detail jawaban satu response
    public async Task<IActionResult> ResponseDetail(long id)
    {
        var response = await _context.SurveyResponses
            .Include(r => r.Assignment)
                .ThenInclude(a => a.FormVersion)
                    .ThenInclude(v => v.Form)
            .Include(r => r.SurveyAnswers)
                .ThenInclude(a => a.Question)
                    .ThenInclude(q => q.Section)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (response == null) return NotFound();

        return View(response);
    }
}