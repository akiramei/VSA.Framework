using Microsoft.AspNetCore.Components;
using VSA.Application;
using VSA.Application.Interfaces;
using VSA.Blazor.Services;

namespace VSA.Blazor.Components;

/// <summary>
/// フォームページの基底クラス（結果を返すコマンド用）。
/// UIテンプレートは含まず、ロジックのみを提供します。
/// デザイナーはUIを完全に自由に実装できます。
/// </summary>
/// <typeparam name="TCommand">コマンドの型（ICommand&lt;Result&lt;TResult&gt;&gt;を実装）</typeparam>
/// <typeparam name="TResult">結果の型</typeparam>
public abstract class FormPageBase<TCommand, TResult> : ComponentBase
    where TCommand : ICommand<Result<TResult>>
{
    /// <summary>
    /// MediatRサービス。
    /// </summary>
    [Inject]
    protected IMediatorService Mediator { get; set; } = default!;

    /// <summary>
    /// ナビゲーションマネージャー。
    /// </summary>
    [Inject]
    protected NavigationManager Navigation { get; set; } = default!;

    /// <summary>
    /// フォームが有効かどうか。
    /// バリデーション結果をバインドします。
    /// </summary>
    protected bool FormValid { get; set; }

    /// <summary>
    /// 送信中かどうか。
    /// </summary>
    protected bool Submitting { get; private set; }

    /// <summary>
    /// エラーメッセージ（エラーがない場合はnull）。
    /// </summary>
    protected string? Error { get; private set; }

    /// <summary>
    /// 送信ボタンが無効かどうか。
    /// </summary>
    protected bool SubmitDisabled => !FormValid || Submitting;

    /// <summary>
    /// コマンドを作成します。
    /// 派生クラスでオーバーライドしてフォームデータからコマンドを生成します。
    /// </summary>
    /// <returns>実行するコマンド</returns>
    protected abstract TCommand CreateCommand();

    /// <summary>
    /// 成功時のメッセージを取得します。
    /// </summary>
    protected abstract string SuccessMessage { get; }

    /// <summary>
    /// 成功時のナビゲーション先を取得します。
    /// </summary>
    protected abstract string SuccessNavigateTo { get; }

    /// <summary>
    /// 成功時に呼び出されます。
    /// 派生クラスでオーバーライドしてSnackbar表示などを行います。
    /// </summary>
    /// <param name="message">成功メッセージ</param>
    /// <param name="result">コマンドの結果</param>
    protected virtual void OnSuccess(string message, TResult result)
    {
        // 派生クラスでSnackbar.Add(message, Severity.Success)などを呼び出す
    }

    /// <summary>
    /// エラー発生時に呼び出されます。
    /// 派生クラスでオーバーライドしてSnackbar表示などを行います。
    /// </summary>
    /// <param name="errorMessage">エラーメッセージ</param>
    protected virtual void OnError(string errorMessage)
    {
        Error = errorMessage;
    }

    /// <summary>
    /// フォームを送信します。
    /// </summary>
    protected async Task SubmitAsync()
    {
        if (!FormValid || Submitting) return;

        Submitting = true;
        Error = null;
        StateHasChanged();

        try
        {
            var command = CreateCommand();
            var result = await Mediator.CommandAsync(command, OnError);

            if (result is not null)
            {
                OnSuccess(SuccessMessage, result);
                Navigation.NavigateTo(SuccessNavigateTo);
            }
        }
        finally
        {
            Submitting = false;
            StateHasChanged();
        }
    }
}

/// <summary>
/// フォームページの基底クラス（結果を返さないコマンド用）。
/// UIテンプレートは含まず、ロジックのみを提供します。
/// </summary>
/// <typeparam name="TCommand">コマンドの型（ICommand&lt;Result&gt;を実装）</typeparam>
public abstract class FormPageBase<TCommand> : ComponentBase
    where TCommand : ICommand<Result>
{
    /// <summary>
    /// MediatRサービス。
    /// </summary>
    [Inject]
    protected IMediatorService Mediator { get; set; } = default!;

    /// <summary>
    /// ナビゲーションマネージャー。
    /// </summary>
    [Inject]
    protected NavigationManager Navigation { get; set; } = default!;

    /// <summary>
    /// フォームが有効かどうか。
    /// </summary>
    protected bool FormValid { get; set; }

    /// <summary>
    /// 送信中かどうか。
    /// </summary>
    protected bool Submitting { get; private set; }

    /// <summary>
    /// エラーメッセージ（エラーがない場合はnull）。
    /// </summary>
    protected string? Error { get; private set; }

    /// <summary>
    /// 送信ボタンが無効かどうか。
    /// </summary>
    protected bool SubmitDisabled => !FormValid || Submitting;

    /// <summary>
    /// コマンドを作成します。
    /// </summary>
    /// <returns>実行するコマンド</returns>
    protected abstract TCommand CreateCommand();

    /// <summary>
    /// 成功時のメッセージを取得します。
    /// </summary>
    protected abstract string SuccessMessage { get; }

    /// <summary>
    /// 成功時のナビゲーション先を取得します。
    /// </summary>
    protected abstract string SuccessNavigateTo { get; }

    /// <summary>
    /// 成功時に呼び出されます。
    /// </summary>
    /// <param name="message">成功メッセージ</param>
    protected virtual void OnSuccess(string message)
    {
        // 派生クラスでSnackbar.Add(message, Severity.Success)などを呼び出す
    }

    /// <summary>
    /// エラー発生時に呼び出されます。
    /// </summary>
    /// <param name="errorMessage">エラーメッセージ</param>
    protected virtual void OnError(string errorMessage)
    {
        Error = errorMessage;
    }

    /// <summary>
    /// フォームを送信します。
    /// </summary>
    protected async Task SubmitAsync()
    {
        if (!FormValid || Submitting) return;

        Submitting = true;
        Error = null;
        StateHasChanged();

        try
        {
            var command = CreateCommand();
            var success = await Mediator.CommandAsync(command, OnError);

            if (success)
            {
                OnSuccess(SuccessMessage);
                Navigation.NavigateTo(SuccessNavigateTo);
            }
        }
        finally
        {
            Submitting = false;
            StateHasChanged();
        }
    }
}
