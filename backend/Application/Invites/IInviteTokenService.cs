namespace Application.Invites;

public interface IInviteTokenService
{
    string Generate();
    string Hash(string token);
}