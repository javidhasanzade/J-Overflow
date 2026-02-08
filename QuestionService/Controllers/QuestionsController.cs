using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestionService.Data;
using QuestionService.DTOs;
using QuestionService.Models;

namespace QuestionService.Controllers;

[ApiController]
[Route("[controller]")]
public class QuestionsController(QuestionDbContext dbContext) : ControllerBase
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
        
        return Created($"/questions/{question.Id}", question);
    }
}