using MyRunshaw.Domain.Entities;

namespace MyRunshaw.Application.Notices;

public interface INoticeService
{
    Task<List<InAppNotice>> GetNoticesAsync();
}