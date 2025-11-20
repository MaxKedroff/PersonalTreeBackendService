using Application.Dtos;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Validators
{
    public class UpdateProfileDtoValidator : AbstractValidator<UpdateProfileDto>
    {
        private static readonly string[] AllowedSchemes = { "http://", "https://", "data:image/" };

        public UpdateProfileDtoValidator()
        {
            RuleFor(x => x.Phone)
                .Matches(@"^\+?[1-9]\d{1,14}$")
                .When(x => !string.IsNullOrWhiteSpace(x.Phone))
                .WithMessage("Некорректный формат телефона. Используйте международный формат (например, +79991234567).");

            RuleFor(x => x.City)
                .MaximumLength(100)
                .When(x => !string.IsNullOrWhiteSpace(x.City))
                .WithMessage("Город не должен превышать 100 символов.");

            RuleFor(x => x.Interests)
                .MaximumLength(500)
                .When(x => !string.IsNullOrWhiteSpace(x.Interests))
                .WithMessage("Интересы не должны превышать 500 символов.");

            RuleFor(x => x.Avatar)
                .Must(BeValidUrlOrBase64)
                .When(x => !string.IsNullOrWhiteSpace(x.Avatar))
                .WithMessage("Аватар должен быть URL (http/https) или Base64-изображение (data:image/...).");

            RuleFor(x => x.Position)
                .MaximumLength(100)
                .When(x => !string.IsNullOrWhiteSpace(x.Position))
                .WithMessage("Должность не должна превышать 100 символов.");

            RuleFor(x => x.Department)
                .MaximumLength(100)
                .When(x => !string.IsNullOrWhiteSpace(x.Department))
                .WithMessage("Название отдела не должно превышать 100 символов.");

            RuleFor(x => x.Contacts)
                .Must(HaveValidContactEntries)
                .When(x => x.Contacts != null)
                .WithMessage("Каждый контакт: ключ — до 50, значение — до 200 символов.");
        }

        private bool BeValidUrlOrBase64(string url)
        {
            return AllowedSchemes.Any(s => url.StartsWith(s, StringComparison.OrdinalIgnoreCase));
        }

        private bool HaveValidContactEntries(IDictionary<string, object> contacts)
        {
            return contacts.All(kvp =>
                !string.IsNullOrWhiteSpace(kvp.Key) &&
                kvp.Key.Length <= 50 &&
                (kvp.Value?.ToString()?.Length ?? 0) <= 200);
        }
    }
}

