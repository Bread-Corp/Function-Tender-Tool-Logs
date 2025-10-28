namespace Tender_Tool_Logs_Lambda.Interfaces
{
    public interface IAuthService
    {
        /// <summary>
        /// Checks if a user is a designated super user.
        /// </summary>
        /// <param name="userId">The Guid of the user to check.</param>
        /// <returns>True if the user is a super user, otherwise false.</returns>
        Task<bool> IsSuperUserAsync(Guid userId);
    }
}
