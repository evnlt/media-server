namespace Infrastructure.Settings;

// Crf and Preset use different scales per codec, hence one profile each.
public class CodecProfileSettings
{
    public int Crf { get; set; }

    public string Preset { get; set; } = default!;
}
