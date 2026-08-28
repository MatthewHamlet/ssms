using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurveyFormApp.Models;

namespace SurveyFormApp.Controllers;

public class AssignmentController : Controller
{
    private readonly SurveyDbContext _context;

    public AssignmentController(SurveyDbContext context)
    {
        _context = context;
    }

    // GET: /Assignment
    public async Task<IActionResult> Index(string? status, string? surveyorId)
    {
        var query = _context.SurveyAssignments
            .Include(a => a.FormVersion)
                .ThenInclude(v => v.Form)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(a => a.Status == status);

        if (!string.IsNullOrWhiteSpace(surveyorId))
            query = query.Where(a => a.SurveyorId != null && a.SurveyorId.Contains(surveyorId));

        var items = await query.OrderByDescending(a => a.AssignedAt).ToListAsync();

        ViewData["ActiveMenu"] = "assignment";
        ViewData["FilterStatus"] = status;
        ViewData["FilterSurveyorId"] = surveyorId;
        return View(items);
    }

    // GET: /Assignment/Create
    public async Task<IActionResult> Create()
    {
        ViewData["ActiveMenu"] = "assignment";
        ViewBag.Forms = await _context.SurveyForms
            .Where(f => f.IsActive && f.SurveyFormVersions.Any(v => v.IsPublished))
            .Include(f => f.SurveyFormVersions)
            .ToListAsync();

        return View();
    }

    // POST: /Assignment/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        long formId, string applicationId, string surveyorId,
        string? branchId, int? priority, DateTime? dueDate)
    {
        if (string.IsNullOrWhiteSpace(applicationId) || string.IsNullOrWhiteSpace(surveyorId))
        {
            TempData["Error"] = "Application ID dan Surveyor wajib diisi.";
            return RedirectToAction(nameof(Create));
        }

        var version = await _context.SurveyFormVersions
            .Where(v => v.FormId == formId && v.IsPublished)
            .OrderByDescending(v => v.VersionNo)
            .FirstOrDefaultAsync();

        if (version == null)
        {
            TempData["Error"] = "Form yang dipilih belum punya versi yang di-publish.";
            return RedirectToAction(nameof(Create));
        }

        var assignment = new SurveyAssignment
        {
            ApplicationId = applicationId.Trim(),
            SurveyorId = surveyorId.Trim(),
            FormVersionId = version.Id,
            Status = "ASSIGNED",
            BranchId = string.IsNullOrWhiteSpace(branchId) ? null : branchId.Trim(),
            Priority = priority,
            AssignedAt = DateTime.Now,
            DueDate = dueDate,
            CreatedAt = DateTime.Now
        };
        _context.SurveyAssignments.Add(assignment);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Survey berhasil di-assign ke '{assignment.SurveyorId}'.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Assignment/Edit/5
    public async Task<IActionResult> Edit(long id)
    {
        var assignment = await _context.SurveyAssignments
            .Include(a => a.FormVersion)
                .ThenInclude(v => v.Form)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (assignment == null) return NotFound();

        if (assignment.Status != "ASSIGNED")
        {
            TempData["Error"] = "Hanya assignment berstatus ASSIGNED yang bisa diedit.";
            return RedirectToAction(nameof(Index));
        }

        ViewData["ActiveMenu"] = "assignment";
        ViewBag.Forms = await _context.SurveyForms
            .Where(f => f.IsActive && f.SurveyFormVersions.Any(v => v.IsPublished))
            .Include(f => f.SurveyFormVersions)
            .ToListAsync();

        return View(assignment);
    }

    // POST: /Assignment/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        long id, long formId, string applicationId, string surveyorId,
        string? branchId, int? priority, DateTime? dueDate)
    {
        var assignment = await _context.SurveyAssignments.FindAsync(id);
        if (assignment == null) return NotFound();

        if (assignment.Status != "ASSIGNED")
        {
            TempData["Error"] = "Hanya assignment berstatus ASSIGNED yang bisa diedit.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(applicationId) || string.IsNullOrWhiteSpace(surveyorId))
        {
            TempData["Error"] = "Application ID dan Surveyor wajib diisi.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        var version = await _context.SurveyFormVersions
            .Where(v => v.FormId == formId && v.IsPublished)
            .OrderByDescending(v => v.VersionNo)
            .FirstOrDefaultAsync();

        if (version == null)
        {
            TempData["Error"] = "Form yang dipilih belum punya versi yang di-publish.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        assignment.ApplicationId = applicationId.Trim();
        assignment.SurveyorId = surveyorId.Trim();
        assignment.FormVersionId = version.Id;
        assignment.BranchId = string.IsNullOrWhiteSpace(branchId) ? null : branchId.Trim();
        assignment.Priority = priority;
        assignment.DueDate = dueDate;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Assignment berhasil diperbarui.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Assignment/Cancel
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(long id)
    {
        var assignment = await _context.SurveyAssignments.FindAsync(id);
        if (assignment == null) return NotFound();

        if (assignment.Status == "ASSIGNED")
        {
            assignment.Status = "CANCELED";
            assignment.CanceledAt = DateTime.Now;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Assignment berhasil dibatalkan.";
        }

        return RedirectToAction(nameof(Index));
    }
}