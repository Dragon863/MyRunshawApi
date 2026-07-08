using MyRunshaw.Contracts.Responses;

namespace MyRunshaw.Application.Common;

public interface ISyncService
{
    Task<SyncResponse> GetSyncPayloadAsync(string studentId);
}