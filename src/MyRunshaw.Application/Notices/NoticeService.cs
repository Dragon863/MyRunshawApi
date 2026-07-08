using MyRunshaw.Domain.Entities;

namespace MyRunshaw.Application.Notices;

public class NoticeService : INoticeService
{
    private readonly IInAppNoticeRepository _noticeRepository;

    public NoticeService(IInAppNoticeRepository noticeRepository)
    {
        _noticeRepository = noticeRepository;
    }

    public async Task<List<InAppNotice>> GetNoticesAsync()
    {
        return await _noticeRepository.GetNoticesAsync();
    }
}
