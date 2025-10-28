using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tender_Tool_Logs_Lambda.Data;
using Tender_Tool_Logs_Lambda.Interfaces;

namespace Tender_Tool_Logs_Lambda.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;

        // Your ApplicationDbContext is injected here by the service container
        public AuthService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Checks if a user is a designated super user.
        /// </summary>
        /// <param name="userId">The Guid of the user to check.</param>
        /// <returns>True if the user is a super user, otherwise false.</returns>
        public async Task<bool> IsSuperUserAsync(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return false;
            }

            // This is the most efficient way to check if the record exists.
            // It queries the "TenderUser" table based on your DbContext.
            return await _context.Users
                .AnyAsync(u => u.UserID == userId && u.IsSuperUser == true);
        }
    }
}
