using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using SurveyFormApp.Models;

namespace SurveyFormApp.Services;

public static class SurveyAnswerValidator
{
    /// <summary>
    /// Validasi 1 jawaban terhadap definisi pertanyaan (tipe, required, min/max, maxlength, regex, opsi dropdown).
    /// Ini SUMBER KEBENARAN validasi — jangan andalkan JS di client.
    /// </summary>
    public static bool TryValidate(
        SurveyQuestion question,
        string? rawValue,
        out decimal? valueNumber,
        out DateTime? valueDate,
        out bool? valueBoolean,
        out string? valueText,
        out string? error)
    {
        valueNumber = null;
        valueDate = null;
        valueBoolean = null;
        valueText = null;
        error = null;

        var type = (question.QuestionType ?? "text").Trim().ToLowerInvariant();
        var isEmpty = string.IsNullOrWhiteSpace(rawValue);

        if (isEmpty)
        {
            if (question.IsRequired)
            {
                error = $"'{question.QuestionText}' wajib diisi.";
                return false;
            }
            return true; // kosong tapi optional, gak masalah
        }

        var trimmed = rawValue!.Trim();

        switch (type)
        {
            case "number":
                if (!decimal.TryParse(trimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
                {
                    error = $"'{question.QuestionText}' harus berupa angka.";
                    return false;
                }
                if (question.MinValue.HasValue && num < question.MinValue.Value)
                {
                    error = $"'{question.QuestionText}' tidak boleh kurang dari {question.MinValue.Value}.";
                    return false;
                }
                if (question.MaxValue.HasValue && num > question.MaxValue.Value)
                {
                    error = $"'{question.QuestionText}' tidak boleh lebih dari {question.MaxValue.Value}.";
                    return false;
                }
                valueNumber = num;
                return true;

            case "date":
                if (!DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                {
                    error = $"'{question.QuestionText}' harus berupa tanggal yang valid.";
                    return false;
                }
                valueDate = dt;
                return true;

            case "boolean":
                if (!bool.TryParse(trimmed, out var b))
                {
                    error = $"'{question.QuestionText}' harus diisi Ya/Tidak.";
                    return false;
                }
                valueBoolean = b;
                return true;

            case "dropdown":
                var allowedValues = question.SurveyQuestionOptions.Select(o => o.OptionValue).ToList();
                if (allowedValues.Count > 0 && !allowedValues.Contains(trimmed))
                {
                    error = $"Jawaban '{question.QuestionText}' tidak valid, pilih dari opsi yang tersedia.";
                    return false;
                }
                valueText = trimmed;
                return true;

            case "textarea":
            case "text":
            default:
                if (question.MaxLength.HasValue && trimmed.Length > question.MaxLength.Value)
                {
                    error = $"'{question.QuestionText}' maksimal {question.MaxLength.Value} karakter.";
                    return false;
                }
                if (!string.IsNullOrWhiteSpace(question.ValidationRegex))
                {
                    try
                    {
                        if (!Regex.IsMatch(trimmed, question.ValidationRegex))
                        {
                            error = $"Format '{question.QuestionText}' tidak sesuai.";
                            return false;
                        }
                    }
                    catch (ArgumentException)
                    {
                        // regex di DB rusak/gak valid, jangan sampai nge-crash — anggap lolos
                    }
                }
                valueText = trimmed;
                return true;
        }
    }
}