using System.ComponentModel.DataAnnotations;

namespace QuestionService.Validators;

public class TagListValidator(int min, int max) : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is List<string> tags)
        {
            if (tags.Count >= min && tags.Count <= max) return ValidationResult.Success;
        }
        
        return new ValidationResult("Tags must be between " + min + " and " + max);
    }
}