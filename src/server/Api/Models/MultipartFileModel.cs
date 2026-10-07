namespace Api.Models;

public sealed record MultipartFileModel(string FileName, Stream Content);
