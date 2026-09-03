using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using POSSystem.Services;

namespace POSSystem.Controllers
{
    // FR-005 to FR-008: sirf Administrator users manage kar sake
    [Authorize(Roles = "Administrator")]
    public class UsersController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AuditLogger _audit;

        private static readonly string[] AvailableRoles = { "Administrator", "Manager", "Cashier" };

        public UsersController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, AuditLogger audit)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _audit = audit;
        }

        // GET: /Users
        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList();
            var rows = new List<(IdentityUser User, IList<string> Roles)>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                rows.Add((user, roles));
            }

            ViewBag.Rows = rows;
            return View();
        }

        // GET: /Users/Create
        public IActionResult Create()
        {
            ViewBag.Roles = AvailableRoles;
            return View();
        }

        // POST: /Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string email, string password, string role)
        {
            ViewBag.Roles = AvailableRoles;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(string.Empty, "Email and password are required.");
                return View();
            }

            var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            var result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View();
            }

            await _userManager.AddToRoleAsync(user, role);
            _audit.Log(_userManager.GetUserId(User)!, "Create", "User", user.Id, $"Created user {email} with role {role}");

            TempData["Message"] = $"User {email} created.";
            return RedirectToAction(nameof(Index));
        }

        public class ChangeRoleDto
        {
            public string UserId { get; set; }
            public string NewRole { get; set; }
        }

        // POST: /Users/ChangeRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(string userId, string newRole)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);

            // FR-008: last active Administrator ko demote nahi hone dena
            if (currentRoles.Contains("Administrator") && newRole != "Administrator")
            {
                var admins = await _userManager.GetUsersInRoleAsync("Administrator");
                if (admins.Count <= 1)
                {
                    TempData["Error"] = "Cannot change role -- at least one Administrator must remain.";
                    return RedirectToAction(nameof(Index));
                }
            }

            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, newRole);

            _audit.Log(_userManager.GetUserId(User)!, "ChangeRole", "User", userId, $"Changed {user.Email} role to {newRole}");

            TempData["Message"] = $"{user.Email} is now {newRole}.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Users/ResetPassword -- FR-006, admin sets a temporary password
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string userId, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Index));
            }

            _audit.Log(_userManager.GetUserId(User)!, "ResetPassword", "User", userId, $"Reset password for {user.Email}");

            TempData["Message"] = $"Password reset for {user.Email}.";
            return RedirectToAction(nameof(Index));
        }
    }
}
