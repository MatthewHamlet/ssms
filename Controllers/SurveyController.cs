using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurveyFormApp.Models;

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
            .Where(f => f.IsActive)
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
    public async Task<IActionResult> Fill(long formVersionId, Dictionary<long, string> answers)
    {
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
            SurveyorId = "anonymous",
            StartedAt = DateTime.Now,
            SubmittedAt = DateTime.Now,
            Status = "SUBMITTED",
            FormVersionId = formVersionId
        };
        _context.SurveyResponses.Add(response);
        await _context.SaveChangesAsync();

        foreach (var kv in answers)
        {
            if (string.IsNullOrWhiteSpace(kv.Value)) continue;

            _context.SurveyAnswers.Add(new SurveyAnswer
            {
                ResponseId = response.Id,
                QuestionId = kv.Key,
                ValueText = kv.Value
            });
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