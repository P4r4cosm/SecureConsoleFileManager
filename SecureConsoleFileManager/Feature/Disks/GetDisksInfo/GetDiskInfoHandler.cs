using MediatR;
using Microsoft.Extensions.Logging;
using Serilog;

namespace SecureConsoleFileManager.Feature.Disks.GetDisksInfo;

public class GetDiskInfoHandler(ILogger<GetDiskInfoHandler> logger)
    : IRequestHandler<GetDisksInfoCommand, List<DriveInfo>>
{
    public Task<List<DriveInfo>> Handle(GetDisksInfoCommand request, CancellationToken cancellationToken)
    {
        var drives =DriveInfo.GetDrives().ToList();
        return Task.FromResult(drives);
    }
}