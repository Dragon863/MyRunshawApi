using MyRunshaw.Domain.Entities;

public interface IInAppNoticeRepository
{
    Task<List<InAppNotice>> GetNoticesAsync();
}