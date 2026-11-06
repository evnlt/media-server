namespace Infrastructure.Enums;

public enum MediaFileStatus
{
    Uploading = 0,
    Queued = 1,
    Transcoding = 2,
    Ready = 3,
    Failed = 4,
    Missing = 5
}
