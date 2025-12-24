namespace LIN.Cloud.Caddy.Models;

public record RegistrationRequest(string Id, string Host, int Port, string Target = "127.0.0.1");
