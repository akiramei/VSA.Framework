using FluentValidation;

namespace Application.Features.LendCopy;

/// <summary>
/// 蔵書コピー貸出Validator（形式検証のみ）
///
/// 【重要】
/// - ここではDBアクセスを伴う検証は行わない
/// - 「コピーが存在するか」「会員が有効か」などはHandler内で確認
/// </summary>
public sealed class LendCopyValidator : AbstractValidator<LendCopyCommand>
{
    public LendCopyValidator()
    {
        RuleFor(x => x.CopyId)
            .Must(id => id.Value != Guid.Empty)
            .WithMessage("蔵書コピーIDは必須です");

        RuleFor(x => x.MemberId)
            .Must(id => id.Value != Guid.Empty)
            .WithMessage("利用者IDは必須です");
    }
}
