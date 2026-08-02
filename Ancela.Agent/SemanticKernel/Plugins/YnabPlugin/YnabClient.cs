using System.Net.Http.Headers;
using Ancela.Agent.SemanticKernel.Plugins.YnabPlugin.Models;
using Ynab.Api.Client;
using Ynab.Api.Client.Extensions;
using Ynab.Api.Client.Models;

namespace Ancela.Agent.SemanticKernel.Plugins.YnabPlugin;

public class YnabClient
{
    private readonly YnabApiClient _client;

    public YnabClient()
    {
        var ynabAccessToken = Environment.GetEnvironmentVariable("YNAB_ACCESS_TOKEN") ?? throw new Exception("YNAB_ACCESS_TOKEN not set");

        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ynabAccessToken);
        _client = new YnabApiClient(httpClient);
    }

    public async Task<AccountsSummaryModel> GetAccountsAsync()
    {
        var planDetail = await _client.GetPlanDetailAsync();
        var accounts = (await _client.GetAccountsAsync(planDetail.Id.ToString())).Accounts;

        var models = accounts
                .Where(a => !a.Deleted && !a.Closed)
                .Select(a => new AccountModel
                {
                    Name = a.Name,
                    Type = a.Type,
                    OnBudget = a.OnBudget,
                    Note = a.Note,
                    Balance = a.Balance.FromMilliunits(),
                    ClearedBalance = a.ClearedBalance.FromMilliunits(),
                    UnclearedBalance = a.UnclearedBalance.FromMilliunits(),
                    LastReconciledAt = a.LastReconciledAt
                }).ToArray();

        // YNAB has no net-worth endpoint, so compute it here rather than make the model
        // sum the list. Liabilities carry negative balances, so NetWorth is a plain sum.
        return new AccountsSummaryModel
        {
            NetWorth = models.Sum(a => a.Balance),
            TotalAssets = models.Where(a => a.Balance > 0).Sum(a => a.Balance),
            TotalLiabilities = models.Where(a => a.Balance < 0).Sum(a => a.Balance),
            Accounts = models,
        };
    }

    /// <param name="month">
    /// Any date within the target month; normalized to the first of the month. Null returns
    /// the current month.
    /// </param>
    public async Task<CategoryModel[]> GetCategoriesAsync(DateTimeOffset? month = null)
    {
        var planDetail = await _client.GetPlanDetailAsync();
        var planId = planDetail.Id.ToString();

        var categories = month is null
            ? (await _client.GetCategoriesAsync(planId)).CategoryGroups.SelectMany(g => g.Categories)
            : (await _client.GetPlanMonthAsync(planId, FirstOfMonth(month.Value))).Month.Categories;

        return categories
                .Where(a => !a.Deleted && !a.Hidden)
                .Select(c => new CategoryModel
                {
                    CategoryGroupName = c.CategoryGroupName,
                    Name = c.Name,
                    Budgeted = c.Budgeted.FromMilliunits(),
                    Activity = c.Activity.FromMilliunits(),
                    Balance = c.Balance.FromMilliunits(),
                    GoalType = c.GoalType,
                    GoalTarget = c.GoalTarget.HasValue ? c.GoalTarget.Value.FromMilliunits() : null,
                    GoalPercentageComplete = c.GoalPercentageComplete,
                    MonthlyNeed = c.MonthlyNeed().FromMilliunits(),
                }).ToArray();
    }

    public async Task<MonthSummaryModel[]> GetMonthSummariesAsync()
    {
        var planDetail = await _client.GetPlanDetailAsync();
        var monthSummaries = await _client.GetPlanMonthsAsync(planDetail.Id.ToString());
        return monthSummaries.Months
                .Select(m => new MonthSummaryModel
                {
                    Month = ToDateTimeOffset(m.Month),
                    Income = m.Income.FromMilliunits(),
                    Budgeted = m.Budgeted.FromMilliunits(),
                    Activity = m.Activity.FromMilliunits(),
                    ReadyToAssign = m.ToBeBudgeted.FromMilliunits(),
                    AgeOfMoney = m.AgeOfMoney
                }).ToArray();
    }

    /// <summary>
    /// Returns transactions newest-first. When <paramref name="categoryName"/> or
    /// <paramref name="payeeName"/> is given the request is filtered server-side (which
    /// correctly expands split transactions); any remaining filters are applied client-side.
    /// </summary>
    public async Task<TransactionModel[]> GetTransactionsAsync(
        DateTimeOffset? sinceDate = null,
        string? accountName = null,
        string? categoryName = null,
        string? payeeName = null)
    {
        var planDetail = await _client.GetPlanDetailAsync();
        var planId = planDetail.Id.ToString();

        // Default to the last 30 days so an unfiltered query can't pull the entire history.
        sinceDate ??= DateTimeOffset.Now.AddDays(-30);
        var sinceDateOnly = DateOnly.FromDateTime(sinceDate.Value.Date);

        string? serverFilter;
        IEnumerable<TransactionModel> transactions;

        if (categoryName is not null)
        {
            serverFilter = "category";
            var response = await _client.GetTransactionsByCategoryAsync(planId, ResolveCategoryId(planDetail, categoryName), sinceDateOnly);
            transactions = response.Transactions.Where(t => !t.Deleted).Select(t => Map(t));
        }
        else if (payeeName is not null)
        {
            serverFilter = "payee";
            var response = await _client.GetTransactionsByPayeeAsync(planId, ResolvePayeeId(planDetail, payeeName), sinceDateOnly);
            transactions = response.Transactions.Where(t => !t.Deleted).Select(t => Map(t));
        }
        else if (accountName is not null)
        {
            serverFilter = "account";
            var response = await _client.GetTransactionsByAccountAsync(planId, ResolveAccountId(planDetail, accountName), sinceDateOnly);
            transactions = response.Transactions.Where(t => !t.Deleted).Select(t => Map(t));
        }
        else
        {
            serverFilter = null;
            var response = await _client.GetTransactionsAsync(planId, sinceDateOnly);
            transactions = response.Transactions.Where(t => !t.Deleted).Select(t => Map(t));
        }

        // Apply any filters the server-side query didn't already satisfy.
        if (accountName is not null && serverFilter != "account")
            transactions = transactions.Where(t => Matches(t.AccountName, accountName));
        if (categoryName is not null && serverFilter != "category")
            transactions = transactions.Where(t => Matches(t.CategoryName, categoryName));
        if (payeeName is not null && serverFilter != "payee")
            transactions = transactions.Where(t => Matches(t.PayeeName, payeeName));

        return transactions.OrderByDescending(t => t.Date).ToArray();
    }

    public async Task<ScheduledTransactionModel[]> GetScheduledTransactionsAsync()
    {
        var planDetail = await _client.GetPlanDetailAsync();
        var scheduled = await _client.GetScheduledTransactionsAsync(planDetail.Id.ToString());
        return scheduled.ScheduledTransactions
                .Where(s => !s.Deleted)
                .Select(s => new ScheduledTransactionModel
                {
                    DateNext = ToDateTimeOffset(s.DateNext),
                    Frequency = s.Frequency,
                    Amount = s.Amount.FromMilliunits(),
                    PayeeName = s.PayeeName,
                    CategoryName = s.CategoryName,
                    AccountName = s.AccountName,
                    Memo = s.Memo,
                })
                .OrderBy(s => s.DateNext)
                .ToArray();
    }

    private static TransactionModel Map(TransactionDetail t) => new()
    {
        Date = ToDateTimeOffset(t.Date),
        Amount = t.Amount.FromMilliunits(),
        PayeeName = t.PayeeName,
        CategoryName = t.CategoryName,
        AccountName = t.AccountName,
        Memo = t.Memo,
        Cleared = t.Cleared,
        Approved = t.Approved,
    };

    private static TransactionModel Map(HybridTransaction t) => new()
    {
        Date = ToDateTimeOffset(t.Date),
        Amount = t.Amount.FromMilliunits(),
        PayeeName = t.PayeeName,
        CategoryName = t.CategoryName,
        AccountName = t.AccountName,
        Memo = t.Memo,
        Cleared = t.Cleared,
        Approved = t.Approved,
    };

    private static string ResolveAccountId(PlanDetail plan, string name)
    {
        var accounts = plan.Accounts!.Where(a => !a.Deleted).ToList();
        var match = accounts.FirstOrDefault(a => string.Equals(a.Name, name, StringComparison.InvariantCultureIgnoreCase))
                    ?? accounts.FirstOrDefault(a => Matches(a.Name, name))
                    ?? throw new Exception($"No account found matching '{name}'.");
        return match.Id.ToString();
    }

    private static string ResolveCategoryId(PlanDetail plan, string name)
    {
        var categories = plan.Categories!.Where(c => !c.Deleted && !c.Hidden).ToList();
        var match = categories.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.InvariantCultureIgnoreCase))
                    ?? categories.FirstOrDefault(c => Matches(c.Name, name))
                    ?? throw new Exception($"No category found matching '{name}'.");
        return match.Id.ToString();
    }

    private static string ResolvePayeeId(PlanDetail plan, string name)
    {
        var payees = plan.Payees!.Where(p => !p.Deleted).ToList();
        var match = payees.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.InvariantCultureIgnoreCase))
                    ?? payees.FirstOrDefault(p => Matches(p.Name, name))
                    ?? throw new Exception($"No payee found matching '{name}'.");
        return match.Id.ToString();
    }

    private static bool Matches(string? value, string query) =>
        value is not null && value.Contains(query, StringComparison.InvariantCultureIgnoreCase);

    private static DateOnly FirstOfMonth(DateTimeOffset date) => new(date.Year, date.Month, 1);

    // The API returns date-only fields as calendar dates with no timezone; treat them as UTC
    // midnight to match what the previous DateTimeOffset-typed client returned.
    private static DateTimeOffset ToDateTimeOffset(DateOnly date) => new(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
}
