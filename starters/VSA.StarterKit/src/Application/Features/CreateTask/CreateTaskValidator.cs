using FluentValidation;

namespace Application.Features.CreateTask;

/// <summary>
/// タスク作成Validator（形式検証のみ）
/// </summary>
public sealed class CreateTaskValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("タイトルは必須です")
            .MaximumLength(200).WithMessage("タイトルは200文字以内で入力してください");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("説明は2000文字以内で入力してください");

        RuleFor(x => x.DueDate)
            .GreaterThan(DateTime.UtcNow).WithMessage("期限は現在より後の日時を指定してください")
            .When(x => x.DueDate.HasValue);
    }

    // 注意: DBアクセスを伴う検証（存在確認など）はここでやらない！
    // そのような検証は Handler 内で実施する
}
