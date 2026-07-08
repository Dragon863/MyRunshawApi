using Ical.Net;
using Microsoft.Extensions.Logging;
using MyRunshaw.Domain.Entities;
using MyRunshaw.Application.Timetables;

namespace MyRunshaw.Infrastructure.Services;

public class TimetableSyncService : ITimetableSyncService
{
    private readonly ITimetableRepository _timetableRepository;
    private readonly HttpClient _httpClient;
    private readonly ILogger<TimetableSyncService> _logger;

    public TimetableSyncService(ITimetableRepository timetableRepository, HttpClient httpClient, ILogger<TimetableSyncService> logger)
    {
        _timetableRepository = timetableRepository;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task SyncTimetableAsync(string studentId, string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            uri.Scheme != "https" ||
            uri.Host != "webservices.runshaw.ac.uk" ||
            uri.AbsolutePath != "/timetable.ashx" ||
            !string.IsNullOrEmpty(uri.Query) && !uri.Query.Contains("id="))
        {
            throw new ArgumentException("Invalid Runshaw timetable URL.");
        }

        var icsData = await _httpClient.GetStringAsync(url);

        var calendar = Calendar.Load(icsData);
        var londonTz = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");

        var timetableDoc = new TimetableDocument
        {
            Version = "2.0",
            ProdId = "-//Runshaw College//EN",
            Method = "PUBLISH",
            Data = new List<TimetableEvent>()
        };

        if (calendar == null || calendar.Events == null || !calendar.Events.Any())
        {
            _logger.LogWarning("No events found in the ICS for {StudentId}", studentId);
        }

        if (calendar == null || calendar.Events == null)
        {
            _logger.LogWarning("Failed to parse ICS for {StudentId}", studentId);
            throw new Exception("Failed to parse ICS data.");
        }

        foreach (var evt in calendar.Events)
        {
            // skip broken events that have no start or end time (never seen before but just in case)
            if (evt.DtStart == null || evt.DtEnd == null) continue;

            // please don't break with DST!
            var dtStart = TimeZoneInfo.ConvertTimeFromUtc(evt.DtStart.AsUtc, londonTz);
            var dtEnd = TimeZoneInfo.ConvertTimeFromUtc(evt.DtEnd.AsUtc, londonTz);

            // DtStamp is sometimes null, fallback to DtStart if it is
            var dtStamp = evt.DtStamp != null
                ? TimeZoneInfo.ConvertTimeFromUtc(evt.DtStamp.AsUtc, londonTz)
                : dtStart;

            timetableDoc.Data.Add(new TimetableEvent
            {
                Type = "VEVENT",
                Uid = evt.Uid ?? Guid.NewGuid().ToString(),
                Summary = evt.Summary ?? string.Empty,
                Location = evt.Location ?? string.Empty,
                Description = evt.Description ?? string.Empty,
                DtStart = new TimetableDate { Dt = dtStart.ToString("yyyyMMddTHHmmss") },
                DtEnd = new TimetableDate { Dt = dtEnd.ToString("yyyyMMddTHHmmss") },
                DtStamp = new TimetableDate { Dt = dtStamp.ToString("yyyyMMddTHHmmss") }
            });
        }

        var cache = await _timetableRepository.GetByStudentIdAsync(studentId);

        if (cache == null)
        {
            cache = new TimetableCache { StudentId = studentId, Data = timetableDoc };
            await _timetableRepository.AddAsync(cache);
        }
        else
        {
            cache.Data = timetableDoc;
            cache.UpdatedAt = DateTime.UtcNow;
            await _timetableRepository.UpdateAsync(cache);
        }

        _logger.LogInformation("Timetable synced for {StudentId}", studentId);
    }
}