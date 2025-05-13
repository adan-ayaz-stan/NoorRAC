using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NoorRAC.Models;
using System.Threading.Tasks;

namespace NoorRAC.Services
{
    public interface IAuthenticationService
    {
        Task<User?> LoginAsync(string username, string password);
        // Add Logout, Register, etc. if needed
    }
}