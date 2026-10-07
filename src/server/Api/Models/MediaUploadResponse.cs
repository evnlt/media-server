namespace Api.Models;

public record MediaUploadResponse(Guid Id, string FileName, long SizeBytes, string Status);
