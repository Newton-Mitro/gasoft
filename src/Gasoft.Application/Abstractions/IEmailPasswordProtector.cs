namespace Gasoft.Application.Abstractions;

public interface IEmailPasswordProtector
{
    string Protect(string password);
    string Unprotect(string protectedPassword);
}
