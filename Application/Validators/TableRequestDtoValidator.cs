using Application.Dtos;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Validators
{
    public class TableRequestDtoValidator : AbstractValidator<TableRequestDto>
    {
        public TableRequestDtoValidator()
        {
            RuleFor(x => x.page)
                .GreaterThanOrEqualTo(1)
                .WithMessage("Номер страницы должен быть не меньше 1.");

            RuleFor(x => x.Limit)
                .InclusiveBetween(1, 100)
                .WithMessage("Лимит должен быть от 1 до 100.");

            RuleFor(x => x.Sort)
                .Must(BeValidSort)
                .When(x => !string.IsNullOrEmpty(x.Sort))
                .WithMessage("Формат сортировки: поле_порядок (например, name_asc). Допустимые порядки: asc, desc.");

            RuleFor(x => x.SearchText)
                .MaximumLength(100)
                .When(x => !string.IsNullOrEmpty(x.SearchText))
                .WithMessage("Поисковый текст не должен превышать 100 символов.");

            RuleFor(x => x.PositionFilter)
                .MaximumLength(100)
                .When(x => !string.IsNullOrEmpty(x.PositionFilter))
                .WithMessage("Фильтр по должности не должен превышать 100 символов.");

            RuleFor(x => x.DepartmentFilter)
                .MaximumLength(100)
                .When(x => !string.IsNullOrEmpty(x.DepartmentFilter))
                .WithMessage("Фильтр по отделу не должен превышать 100 символов.");
        }

        private bool BeValidSort(string sort)
        {
            if (string.IsNullOrEmpty(sort)) return true;
            var parts = sort.Split('_');
            return parts.Length == 2 && new[] { "asc", "desc" }.Contains(parts[1].ToLower());
        }
    }
}

