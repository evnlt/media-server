namespace Infrastructure.Enums;

// Values start at 1 so an unset Transcode:Codec (binds as 0) is undefined and fails validation.
public enum TranscodeCodec
{
    H264 = 1,
    Av1 = 2
}
