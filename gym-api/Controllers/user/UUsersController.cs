using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using gym_api.Models;

namespace gym_api.Controllers.user
{
    [Route("api/[controller]")]
    [ApiController]
    [Tags("會員管理")]
    public class UUsersController : ControllerBase
    {
        private readonly dbFitness2Context _context;

        public UUsersController(dbFitness2Context context)
        {
            _context = context;
        }

        // GET: api/UUsers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UUser>>> GetUUsers()
        {
            return await _context.UUsers.ToListAsync();
        }

        // GET: api/UUsers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UUser>> GetUUser(int id)
        {
            var uUser = await _context.UUsers.FindAsync(id);

            if (uUser == null)
            {
                return NotFound();
            }

            return uUser;
        }

        // POST: api/UUsers
        [HttpPost]
        public async Task<ActionResult<UUser>> PostUUser(UUser uUser)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.UUsers.Add(uUser);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUUser), new { id = uUser.UserId }, uUser);
        }

        // PUT: api/UUsers/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUUser(int id, UUser uUser)
        {
            if (id != uUser.UserId)
            {
                return BadRequest("ID 不符");
            }

            _context.Entry(uUser).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UUserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/UUsers/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUUser(int id)
        {
            var uUser = await _context.UUsers.FindAsync(id);
            if (uUser == null)
            {
                return NotFound();
            }

            _context.UUsers.Remove(uUser);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UUserExists(int id)
        {
            return _context.UUsers.Any(e => e.UserId == id);
        }
    }
}