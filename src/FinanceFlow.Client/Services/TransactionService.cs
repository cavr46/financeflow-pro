using System.Net.Http.Json;
using FinanceFlow.Client.Models;

namespace FinanceFlow.Client.Services
{
    public class TransactionService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "api/transactions";

        public TransactionService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<TransactionListResponse> GetTransactionsAsync(TransactionFilterRequest filter)
        {
            var queryParams = new List<string>();
            
            if (filter.StartDate.HasValue)
                queryParams.Add($"startDate={filter.StartDate.Value:yyyy-MM-dd}");
            
            if (filter.EndDate.HasValue)
                queryParams.Add($"endDate={filter.EndDate.Value:yyyy-MM-dd}");
            
            if (filter.Type.HasValue)
                queryParams.Add($"type={filter.Type.Value}");
            
            if (filter.CategoryId.HasValue)
                queryParams.Add($"categoryId={filter.CategoryId.Value}");
            
            if (!string.IsNullOrEmpty(filter.SearchTerm))
                queryParams.Add($"search={Uri.EscapeDataString(filter.SearchTerm)}");
            
            if (filter.AccountId.HasValue)
                queryParams.Add($"accountId={filter.AccountId.Value}");
            
            if (filter.MinAmount.HasValue)
                queryParams.Add($"minAmount={filter.MinAmount.Value}");
            
            if (filter.MaxAmount.HasValue)
                queryParams.Add($"maxAmount={filter.MaxAmount.Value}");
            
            queryParams.Add($"page={filter.Page}");
            queryParams.Add($"pageSize={filter.PageSize}");

            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var response = await _httpClient.GetFromJsonAsync<TransactionListResponse>($"{_baseUrl}{queryString}");
            
            return response ?? new TransactionListResponse();
        }

        public async Task<TransactionDto?> GetTransactionAsync(int id)
        {
            return await _httpClient.GetFromJsonAsync<TransactionDto>($"{_baseUrl}/{id}");
        }

        public async Task<TransactionDto> CreateTransactionAsync(CreateTransactionDto transaction)
        {
            var response = await _httpClient.PostAsJsonAsync(_baseUrl, transaction);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TransactionDto>() ?? throw new Exception("Failed to create transaction");
        }

        public async Task<TransactionDto> UpdateTransactionAsync(int id, UpdateTransactionDto transaction)
        {
            var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}/{id}", transaction);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TransactionDto>() ?? throw new Exception("Failed to update transaction");
        }

        public async Task DeleteTransactionAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/{id}");
            response.EnsureSuccessStatusCode();
        }

        public async Task<List<CategoryDto>> GetCategoriesAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<CategoryDto>>("api/categories") ?? new List<CategoryDto>();
        }

        public async Task<List<AccountDto>> GetAccountsAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<AccountDto>>("api/accounts") ?? new List<AccountDto>();
        }

        public async Task<TransactionStatsDto> GetMonthlyStatsAsync()
        {
            var now = DateTime.Now;
            var startDate = new DateTime(now.Year, now.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            
            return await _httpClient.GetFromJsonAsync<TransactionStatsDto>(
                $"{_baseUrl}/stats?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}") 
                ?? new TransactionStatsDto();
        }
    }

    public class TransactionFilterRequest
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public TransactionType? Type { get; set; }
        public int? CategoryId { get; set; }
        public int? AccountId { get; set; }
        public string? SearchTerm { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class TransactionListResponse
    {
        public List<TransactionDto> Transactions { get; set; } = new();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    public class TransactionDto
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public TransactionType Type { get; set; }
        public DateTime Date { get; set; }
        public int AccountId { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? CategoryColor { get; set; }
        public string? CategoryIcon { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateTransactionDto
    {
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public TransactionType Type { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public int AccountId { get; set; }
        public int? CategoryId { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateTransactionDto : CreateTransactionDto
    {
    }

    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string? Color { get; set; }
        public CategoryType Type { get; set; }
    }

    public class AccountDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public AccountType Type { get; set; }
        public decimal Balance { get; set; }
        public string Currency { get; set; } = "USD";
    }

    public class TransactionStatsDto
    {
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetAmount { get; set; }
        public int TransactionCount { get; set; }
    }

    public enum CategoryType
    {
        Income = 0,
        Expense = 1,
        Both = 2
    }
}