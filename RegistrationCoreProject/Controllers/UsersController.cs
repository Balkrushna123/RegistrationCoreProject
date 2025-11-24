using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Model;

namespace RegistrationCoreProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly PasswordHasher<User> _passwordHasher;

        public UsersController(ApplicationDbContext db)
        {
            _db = db;
            _passwordHasher = new PasswordHasher<User>();
        }

        // GET: api/users
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _db.Users
                .Select(u => new {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    u.DateOfBirth,
                    u.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        // POST: api/users
        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Check password match
            if (dto.Password != dto.ConfirmPassword)
                return BadRequest(new { message = "Passwords do not match." });

            // Check age >= 18
            var today = DateTime.UtcNow.Date;
            var age = today.Year - dto.DateOfBirth.Year;
            if (dto.DateOfBirth.Date > today.AddYears(-age)) age--;
            if (age < 18)
                return BadRequest(new { message = "User must be at least 18 years old." });

            // Check unique email
            var exists = await _db.Users.AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower());
            if (exists)
                return Conflict(new { message = "Email already registered." });

            var user = new User
            {
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Email = dto.Email.Trim(),
                DateOfBirth = dto.DateOfBirth
            };

            // Hash password
            user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAll), new { id = user.Id }, new { message = "Registration successful." });
        }

    }
}
