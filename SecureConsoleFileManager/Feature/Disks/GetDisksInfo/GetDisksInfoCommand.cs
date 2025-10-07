
using MediatR;

namespace SecureConsoleFileManager.Feature.Disks.GetDisksInfo;

public record GetDisksInfoCommand : IRequest<List<DriveInfo>>;