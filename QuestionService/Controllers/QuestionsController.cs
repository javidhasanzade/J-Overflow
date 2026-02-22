using System.Security.Claims;
using Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestionService.Data;
using QuestionService.DTOs;
using QuestionService.Models;
using Wolverine;

namespace QuestionService.Controllers;

[ApiController]
[Route("[controller]")]
public class QuestionsController(QuestionDbContext dbContext, IMessageBus messageBus) : ControllerBase
{
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<Question>> CreateQuestion([FromBody] CreateQuestionDto questionDto)
    {
        var validTags = await dbContext.Tags.Where(x => questionDto.Tags.Contains(x.Slug)).ToListAsync();

        var missing = questionDto.Tags.Except(validTags.Select(x => x.Slug).ToList()).ToList();

        if (missing.Count != 0)
            return BadRequest($"Invalid tags: {string.Join(", ", missing)}");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userName = User.FindFirstValue("name");

        if (userId is null || userName is null) return BadRequest("Cannot get user details");

        var question = new Question
        {
            Title = questionDto.Title,
            Content = questionDto.Content,
            TagSlugs = questionDto.Tags,
            AskerId = userId,
            AskerDisplayName = userName
        };

        dbContext.Questions.Add(question);
        await dbContext.SaveChangesAsync();

        await messageBus.PublishAsync(new QuestionCreated(question.Id, question.Title, question.Content,
            question.CreatedAt, question.TagSlugs));

        return Created($"/questions/{question.Id}", question);
    }

    [HttpGet]
    public async Task<ActionResult<List<Question>>> GetQuestions(string? tag)
    {
        var query = dbContext.Questions.AsQueryable();

        if (!string.IsNullOrEmpty(tag))
        {
            query = query.Where(x => x.TagSlugs.Contains(tag));
        }

        return await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Question>> GetQuestion(string id)
    {
        var question = await dbContext.Questions.FindAsync(id);

        if (question is null) return NotFound();

        await dbContext.Questions.Where(x => x.Id == id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.ViewCount,
                x => x.ViewCount + 1));

        return question;
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateQuestion(string id, CreateQuestionDto questionDto)
    {
        var question = await dbContext.Questions.FindAsync(id);
        if (question is null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != question.AskerId) return Forbid();

        var validTags = await dbContext.Tags.Where(x => questionDto.Tags.Contains(x.Slug)).ToListAsync();

        var missing = questionDto.Tags.Except(validTags.Select(x => x.Slug).ToList()).ToList();

        if (missing.Count != 0)
            return BadRequest($"Invalid tags: {string.Join(", ", missing)}");

        question.Title = questionDto.Title;
        question.Content = questionDto.Content;
        question.TagSlugs = questionDto.Tags;
        question.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
        return NoContent();
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteQuestion(string id)
    {
        var question = await dbContext.Questions.FindAsync(id);
        if (question is null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != question.AskerId) return Forbid();

        dbContext.Questions.Remove(question);
        await dbContext.SaveChangesAsync();
        return NoContent();
    }
}