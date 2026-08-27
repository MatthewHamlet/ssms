using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurveyFormApp.Models;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace SurveyFormApp.Controllers;

public class SurveyController : Controller
{
    private readonly SurveyDbContext _context;

    // ==== Konfigurasi validasi upload foto ====
    private static readonly string[] AllowedPhotoExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private static readonly string[] AllowedPhotoContentTypes = { "image/jpeg", "image/png", "image/webp" };
    private const long MaxPhotoSizeBytes = 5 * 1024 * 1024; // 5 MB

    // ==== Konfigurasi fraud/geo heuristic (silakan sesuaikan) ====
    private const int MinDurationSecondsThreshold = 30;      // submit lebih cepat dari ini dicurigai
    private const decimal MaxValidDistanceMeters = 500m;      // toleransi jarak surveyor vs lokasi debitur

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

        ViewData["ActiveMenu"] = "surveyindex";
        return View(version);
    }

    // POST: /Survey/Fill  -> simpan jawaban
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Fill(
        long formVersionId,
        Dictionary<long, string> answers,
        Dictionary<long, IFormFile> photos,
        string? surveyorName,
        decimal? latitude,
        decimal? longitude,
        decimal? debtorLatitude,
        decimal? debtorLongitude,
        string? deviceId,
        string? appVersion,
        int? durationSeconds)
    {
        answers ??= new Dictionary<long, string>();
        photos ??= new Dictionary<long, IFormFile>();

        // ===== 1. Validasi formVersionId: harus ada & masih published =====
        var version = await _context.SurveyFormVersions
            .Where(v => v.Id == formVersionId && v.IsPublished)
            .Include(v => v.SurveySections)
                .ThenInclude(s => s.SurveyQuestions.OrderBy(q => q.OrderNo))
                    .ThenInclude(q => q.SurveyQuestionOptions)
            .Include(v => v.SurveySections)
                .ThenInclude(s => s.SurveyQuestions)
                    .ThenInclude(q => q.SurveyQuestionRuleQuestions)
            .FirstOrDefaultAsync();

        if (version == null)
        {
            return NotFound("Versi form ini tidak valid atau sudah tidak di-publish. Silakan buka ulang form dari daftar.");
        }

        var allQuestions = version.SurveySections.SelectMany(s => s.SurveyQuestions).ToList();

        // ===== 2. Validasi server-side: field wajib (menghormati rule show/hide) =====
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(surveyorName) && string.IsNullOrWhiteSpace(HttpContext.Session.GetString("SurveyorId")))
        {
            errors.Add("Nama surveyor wajib diisi.");
        }

        foreach (var q in allQuestions)
        {
            if (!q.IsRequired) continue;
            if (IsQuestionHiddenByRules(q, answers)) continue; // lagi disembunyikan rule, skip validasi

            if ((q.QuestionType ?? "").ToLower() == "photo")
            {
                if (!photos.TryGetValue(q.Id, out var pf) || pf == null || pf.Length == 0)
                    errors.Add($"'{q.QuestionText}' wajib diisi (upload foto).");
            }
            else
            {
                if (!answers.TryGetValue(q.Id, out var val) || string.IsNullOrWhiteSpace(val))
                    errors.Add($"'{q.QuestionText}' wajib diisi.");
            }
        }

        // ===== 3. Validasi file foto (ekstensi, mime type, ukuran) =====
        foreach (var kv in photos)
        {
            var file = kv.Value;
            if (file == null || file.Length == 0) continue;

            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? "";
            var contentType = file.ContentType?.ToLowerInvariant() ?? "";

            if (!AllowedPhotoExtensions.Contains(ext) || !AllowedPhotoContentTypes.Contains(contentType))
            {
                errors.Add($"File '{file.FileName}' bukan format gambar yang diizinkan (hanya jpg/jpeg/png/webp).");
                continue;
            }

            if (file.Length > MaxPhotoSizeBytes)
            {
                errors.Add($"File '{file.FileName}' melebihi batas ukuran {MaxPhotoSizeBytes / (1024 * 1024)}MB.");
            }
        }

        if (errors.Count > 0)
        {
            ViewData["ActiveMenu"] = "surveyindex";
            ViewData["Errors"] = errors;
            return View(version);
        }

        // ===== 4. Simpan assignment & response =====
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

        var questionTypes = allQuestions.ToDictionary(q => q.Id, q => (q.QuestionType ?? "text").ToLower());

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
                    if (decimal.TryParse(kv.Value, out var numVal)) answer.ValueNumber = numVal;
                    else answer.ValueText = kv.Value;
                    break;
                case "date":
                    if (DateTime.TryParse(kv.Value, out var dateVal)) answer.ValueDate = dateVal;
                    else answer.ValueText = kv.Value;
                    break;
                case "boolean":
                    if (bool.TryParse(kv.Value, out var boolVal)) answer.ValueBoolean = boolVal;
                    else answer.ValueText = kv.Value;
                    break;
                default:
                    answer.ValueText = kv.Value;
                    break;
            }

            _context.SurveyAnswers.Add(answer);
        }

        if (photos.Count > 0)
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

        // ===== 5. Log lokasi surveyor =====
        if (latitude.HasValue && longitude.HasValue)
        {
            _context.SurveyLocationLogs.Add(new SurveyLocationLog
            {
                ResponseId = response.Id,
                Latitude = latitude.Value,
                Longitude = longitude.Value,
                RecordedAt = DateTime.Now
            });
        }

        // ===== 6. Validasi geo (kalau lokasi debitur ikut dikirim) =====
        if (latitude.HasValue && longitude.HasValue && debtorLatitude.HasValue && debtorLongitude.HasValue)
        {
            var distance = CalculateDistanceMeters(latitude.Value, longitude.Value, debtorLatitude.Value, debtorLongitude.Value);
            var isValid = distance <= MaxValidDistanceMeters;

            _context.SurveyGeoValidations.Add(new SurveyGeoValidation
            {
                ResponseId = response.Id,
                SurveyLatitude = latitude.Value,
                SurveyLongitude = longitude.Value,
                DebtorLatitude = debtorLatitude.Value,
                DebtorLongitude = debtorLongitude.Value,
                DistanceMeters = distance,
                IsValid = isValid,
                CreatedAt = DateTime.Now
            });

            if (!isValid)
            {
                _context.SurveyFraudFlags.Add(new SurveyFraudFlag
                {
                    ResponseId = response.Id,
                    FlagCode = "LOCATION_MISMATCH",
                    Description = $"Jarak lokasi survey vs lokasi debitur {distance:N0}m, melebihi toleransi {MaxValidDistanceMeters:N0}m.",
                    Severity = 2,
                    CreatedAt = DateTime.Now
                });
            }
        }

        // ===== 7. Fraud flag heuristik: durasi pengisian terlalu cepat =====
        if (durationSeconds.HasValue && durationSeconds.Value < MinDurationSecondsThreshold)
        {
            _context.SurveyFraudFlags.Add(new SurveyFraudFlag
            {
                ResponseId = response.Id,
                FlagCode = "TOO_FAST_SUBMISSION",
                Description = $"Survey diisi hanya dalam {durationSeconds.Value} detik, di bawah batas wajar {MinDurationSecondsThreshold} detik.",
                Severity = 1,
                CreatedAt = DateTime.Now
            });
        }

        // ===== 8. Scoring otomatis (konvensi QuestionCode: SCORE_HOUSING / SCORE_ENVIRONMENT / SCORE_INCOME) =====
        decimal? scoreHousing = SumNumberAnswerByCodePrefix(allQuestions, answers, "SCORE_HOUSING");
        decimal? scoreEnvironment = SumNumberAnswerByCodePrefix(allQuestions, answers, "SCORE_ENVIRONMENT");
        decimal? scoreIncome = SumNumberAnswerByCodePrefix(allQuestions, answers, "SCORE_INCOME");

        if (scoreHousing.HasValue || scoreEnvironment.HasValue || scoreIncome.HasValue)
        {
            var total = (scoreHousing ?? 0) + (scoreEnvironment ?? 0) + (scoreIncome ?? 0);
            _context.SurveyScores.Add(new SurveyScore
            {
                ResponseId = response.Id,
                ScoreHousing = scoreHousing,
                ScoreEnvironment = scoreEnvironment,
                ScoreIncome = scoreIncome,
                ScoreTotal = total,
                CalculatedAt = DateTime.Now
            });
        }

        await _context.SaveChangesAsync();

        return RedirectToAction("ThankYou");
    }

    public IActionResult ThankYou() => View();

    // GET: /Survey/Responses  -> daftar jawaban, dengan filter + pagination server-side
    public async Task<IActionResult> Responses(
        long? formId, string? status, string? surveyorName,
        DateTime? dateFrom, DateTime? dateTo, int page = 1)
    {
        const int pageSize = 10;
        if (page < 1) page = 1;

        var query = _context.SurveyResponses
            .Include(r => r.Assignment)
                .ThenInclude(a => a.FormVersion)
                    .ThenInclude(v => v.Form)
            .AsQueryable();

        if (formId.HasValue)
            query = query.Where(r => r.Assignment.FormVersion.FormId == formId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(r => r.Status == status);

        if (!string.IsNullOrWhiteSpace(surveyorName))
            query = query.Where(r => r.SurveyorId.Contains(surveyorName));

        if (dateFrom.HasValue)
            query = query.Where(r => r.SubmittedAt >= dateFrom.Value.Date);

        if (dateTo.HasValue)
            query = query.Where(r => r.SubmittedAt < dateTo.Value.Date.AddDays(1));

        query = query.OrderByDescending(r => r.SubmittedAt);

        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var vm = new SurveyResponseListViewModel
        {
            Items = items,
            CurrentPage = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            FilterFormId = formId,
            FilterStatus = status,
            FilterSurveyorName = surveyorName,
            FilterDateFrom = dateFrom,
            FilterDateTo = dateTo,
            AvailableForms = await _context.SurveyForms.OrderBy(f => f.FormName).ToListAsync()
        };

        ViewData["ActiveMenu"] = "responses";
        return View(vm);
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
            case "number": answer.ValueNumber = valueNumber; break;
            case "date": answer.ValueDate = valueDate; break;
            case "boolean": answer.ValueBoolean = valueBoolean; break;
            default: answer.ValueText = valueText; break;
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

    // POST: /Survey/DeleteResponse -> hapus 1 response beserta semua data anak-anaknya
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteResponse(long id)
    {
        var response = await _context.SurveyResponses
            .Include(r => r.SurveyAnswers)
            .Include(r => r.SurveyAnswerGroups)
            .Include(r => r.SurveyAttachments)
            .Include(r => r.SurveyFraudFlags)
            .Include(r => r.SurveyGeoValidation)
            .Include(r => r.SurveyLocationLogs)
            .Include(r => r.SurveyScore)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (response == null) return NotFound();

        // hapus file fisik attachment biar gak jadi sampah di disk
        foreach (var att in response.SurveyAttachments)
        {
            if (string.IsNullOrWhiteSpace(att.FileUrl)) continue;
            var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", att.FileUrl.TrimStart('/'));
            if (System.IO.File.Exists(physicalPath))
            {
                try { System.IO.File.Delete(physicalPath); } catch { /* jangan sampai gagal hapus data gara2 file */ }
            }
        }

        _context.SurveyAnswers.RemoveRange(response.SurveyAnswers);
        _context.SurveyAnswerGroups.RemoveRange(response.SurveyAnswerGroups);
        _context.SurveyAttachments.RemoveRange(response.SurveyAttachments);
        _context.SurveyFraudFlags.RemoveRange(response.SurveyFraudFlags);
        if (response.SurveyGeoValidation != null) _context.SurveyGeoValidations.Remove(response.SurveyGeoValidation);
        _context.SurveyLocationLogs.RemoveRange(response.SurveyLocationLogs);
        if (response.SurveyScore != null) _context.SurveyScores.Remove(response.SurveyScore);
        _context.SurveyResponses.Remove(response);

        await _context.SaveChangesAsync();

        TempData["Success"] = "Jawaban survey berhasil dihapus.";
        return RedirectToAction(nameof(Responses));
    }

    // GET: /Survey/ResponseDetail/5
    public async Task<IActionResult> ResponseDetail(long id)
    {
        var response = await _context.SurveyResponses
            .Include(r => r.Assignment)
                .ThenInclude(a => a.FormVersion)
                    .ThenInclude(v => v.Form)
            .Include(r => r.SurveyAnswers)
                .ThenInclude(a => a.Question)
                    .ThenInclude(q => q.Section)
            .Include(r => r.SurveyGeoValidation)
            .Include(r => r.SurveyFraudFlags)
            .Include(r => r.SurveyScore)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (response == null) return NotFound();

        ViewData["ActiveMenu"] = "responses";
        return View(response);
    }

    // ================= Helper methods =================

    private static bool IsQuestionHiddenByRules(SurveyQuestion question, Dictionary<long, string> answers)
    {
        var rules = question.SurveyQuestionRuleQuestions;
        if (rules == null || rules.Count == 0) return false;

        foreach (var rule in rules)
        {
            var conditionMet = EvaluateRule(rule, answers);
            var action = (rule.Action ?? "SHOW").ToUpperInvariant();
            var visible = action == "HIDE" ? !conditionMet : conditionMet;
            if (visible) return false; // ada rule yang bikin visible -> jangan dianggap hidden
        }
        return true; // gak ada satupun rule yang bikin visible -> hidden
    }

    private static bool EvaluateRule(SurveyQuestionRule rule, Dictionary<long, string> answers)
    {
        if (!answers.TryGetValue(rule.DependsOnQuestionId, out var currentValue) || string.IsNullOrEmpty(currentValue))
            return false;

        var op = (rule.Operator ?? "EQUAL").ToUpperInvariant().Trim();
        var target = (rule.Value ?? "").Trim();

        switch (op)
        {
            case "EQUAL": case "=": case "==": case "EQUALS":
                return string.Equals(currentValue, target, StringComparison.OrdinalIgnoreCase);
            case "NOT_EQUAL": case "!=":
                return !string.Equals(currentValue, target, StringComparison.OrdinalIgnoreCase);
            case "CONTAINS":
                return currentValue.Contains(target, StringComparison.OrdinalIgnoreCase);
            case "GREATER_THAN": case ">":
                return decimal.TryParse(currentValue, out var g1) && decimal.TryParse(target, out var g2) && g1 > g2;
            case "LESS_THAN": case "<":
                return decimal.TryParse(currentValue, out var l1) && decimal.TryParse(target, out var l2) && l1 < l2;
            case ">=":
                return decimal.TryParse(currentValue, out var ge1) && decimal.TryParse(target, out var ge2) && ge1 >= ge2;
            case "<=":
                return decimal.TryParse(currentValue, out var le1) && decimal.TryParse(target, out var le2) && le1 <= le2;
            default:
                return string.Equals(currentValue, target, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static decimal CalculateDistanceMeters(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
    {
        const double earthRadiusM = 6371000d;
        double dLat = ToRadians((double)(lat2 - lat1));
        double dLon = ToRadians((double)(lon2 - lon1));

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(ToRadians((double)lat1)) * Math.Cos(ToRadians((double)lat2)) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return (decimal)(earthRadiusM * c);
    }

    private static double ToRadians(double deg) => deg * Math.PI / 180.0;

    private static decimal? SumNumberAnswerByCodePrefix(List<SurveyQuestion> allQuestions, Dictionary<long, string> answers, string prefix)
    {
        var matchingIds = allQuestions
            .Where(q => (q.QuestionCode ?? "").StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(q => q.Id)
            .ToList();

        if (matchingIds.Count == 0) return null;

        decimal sum = 0;
        bool found = false;
        foreach (var id in matchingIds)
        {
            if (answers.TryGetValue(id, out var raw) && decimal.TryParse(raw, out var val))
            {
                sum += val;
                found = true;
            }
        }
        return found ? sum : null;
    }
}