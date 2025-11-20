using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Validators
{
    public class UserIdValidator : AbstractValidator<Guid>
    {
        public UserIdValidator()
        {
            RuleFor(x => x)
                .NotEmpty()
                .WithMessage("User ID cannot be empty")
                .NotEqual(Guid.Empty)
                .WithMessage("User ID cannot be empty GUID");
        }
    }
}
