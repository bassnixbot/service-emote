namespace EmoteService.Models;

public class ApiResponse<T>
{
    public bool success { get; set; }
    public T? result { get; set; }
    public Error? error { get; set; }
}

public class Error
{
    public string errorCode { get; set; }
    public string errorMessage { get; set; }
    public string? errorStackTrace {get; set;}
}
