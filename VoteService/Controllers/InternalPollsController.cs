using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VoteService.Data;
using VoteService.Models;

namespace VoteService.Controllers
{
    [ApiController]
    [Route("api/internal/polls")]
    public class InternalPollsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public InternalPollsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        public async Task<IActionResult> CreateInternalPoll([FromBody] CreateInternalPollDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Code))
            {
                return BadRequest("Code is required.");
            }

            var existing = await _db.Polls.FirstOrDefaultAsync(p => p.Code.ToLower() == dto.Code.ToLower());
            if (existing != null)
            {
                return Ok(existing);
            }

            var poll = new Poll
            {
                Id = Guid.NewGuid(),
                Code = dto.Code,
                Question = dto.Question,
                OptionsJson = JsonSerializer.Serialize(dto.Options),
                IsClosed = false
            };

            _db.Polls.Add(poll);
            await _db.SaveChangesAsync();

            return Ok(poll);
        }

        // MỚI: PollService gọi endpoint này để lấy số liệu vote THẬT (vì vote chỉ được lưu ở VoteService)
        [HttpGet("{code}/results")]
        public async Task<IActionResult> GetResults(string code)
        {
            var poll = await _db.Polls
                .Include(p => p.Votes)
                .FirstOrDefaultAsync(p => p.Code == code);

            if (poll is null)
            {
                return NotFound(new { error = "Poll not found in VoteService." });
            }

            var options = JsonSerializer.Deserialize<List<string>>(poll.OptionsJson) ?? new();
            var counts = options
                .Select((_, index) => poll.Votes.Count(v => v.OptionIndex == index))
                .ToList();

            return Ok(new
            {
                code = poll.Code,
                question = poll.Question,
                options = options.Select((text, i) => new { index = i, text }).ToList(),
                counts,
                totalVotes = counts.Sum(),
                status = poll.IsClosed ? "closed" : "open"
            });
        }
    }

    public class CreateInternalPollDto
    {
        public string Code { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
    }
}