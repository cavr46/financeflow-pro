using FinanceFlow.Client.Models;
using Microsoft.JSInterop;
using System.Text;
using System.Text.Json;

namespace FinanceFlow.Client.Services
{
    public class ExportService
    {
        private readonly IJSRuntime _jsRuntime;

        public ExportService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task ExportTransactionsToCsv(List<TransactionDto> transactions, string filename = "transactions.csv")
        {
            var csv = new StringBuilder();
            csv.AppendLine("Date,Description,Category,Account,Type,Amount,Notes");

            foreach (var transaction in transactions)
            {
                csv.AppendLine($"{transaction.Date:yyyy-MM-dd}," +
                              $"\"{EscapeCsvField(transaction.Description)}\"," +
                              $"\"{EscapeCsvField(transaction.CategoryName ?? "Uncategorized")}\"," +
                              $"\"{EscapeCsvField(transaction.AccountName)}\"," +
                              $"{transaction.Type}," +
                              $"{transaction.Amount:F2}," +
                              $"\"{EscapeCsvField(transaction.Notes ?? "")}\"");
            }

            await DownloadFile(csv.ToString(), filename, "text/csv");
        }

        public async Task ExportTransactionsToJson(List<TransactionDto> transactions, string filename = "transactions.json")
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(transactions, options);
            await DownloadFile(json, filename, "application/json");
        }

        public async Task ExportTransactionsPdf(List<TransactionDto> transactions, TransactionStatsDto stats, string filename = "transactions.pdf")
        {
            var html = GenerateTransactionsPdfHtml(transactions, stats);
            await _jsRuntime.InvokeVoidAsync("generatePDF", html, filename);
        }

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return string.Empty;

            return field.Replace("\"", "\"\"");
        }

        private async Task DownloadFile(string content, string filename, string contentType)
        {
            await _jsRuntime.InvokeVoidAsync("downloadFile", content, filename, contentType);
        }

        private string GenerateTransactionsPdfHtml(List<TransactionDto> transactions, TransactionStatsDto stats)
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset='utf-8'>");
            html.AppendLine("<title>Transaction Report</title>");
            html.AppendLine("<style>");
            html.AppendLine(@"
                body { font-family: Arial, sans-serif; margin: 20px; }
                .header { text-align: center; margin-bottom: 30px; }
                .stats { display: flex; justify-content: space-around; margin-bottom: 30px; }
                .stat-card { text-align: center; padding: 15px; border: 1px solid #ddd; border-radius: 8px; }
                .stat-value { font-size: 24px; font-weight: bold; margin: 10px 0; }
                .income { color: #22c55e; }
                .expense { color: #ef4444; }
                .balance { color: #3b82f6; }
                table { width: 100%; border-collapse: collapse; margin-top: 20px; }
                th, td { padding: 10px; text-align: left; border-bottom: 1px solid #ddd; }
                th { background-color: #f8f9fa; font-weight: bold; }
                .amount-income { color: #22c55e; font-weight: bold; }
                .amount-expense { color: #ef4444; font-weight: bold; }
                .amount-transfer { color: #06b6d4; font-weight: bold; }
                .footer { margin-top: 30px; text-align: center; color: #666; }
            ");
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            
            // Header
            html.AppendLine("<div class='header'>");
            html.AppendLine("<h1>Transaction Report</h1>");
            html.AppendLine($"<p>Generated on {DateTime.Now:MMMM dd, yyyy}</p>");
            html.AppendLine("</div>");
            
            // Stats
            html.AppendLine("<div class='stats'>");
            html.AppendLine("<div class='stat-card'>");
            html.AppendLine("<div>Total Income</div>");
            html.AppendLine($"<div class='stat-value income'>{stats.TotalIncome:C}</div>");
            html.AppendLine("</div>");
            html.AppendLine("<div class='stat-card'>");
            html.AppendLine("<div>Total Expenses</div>");
            html.AppendLine($"<div class='stat-value expense'>{stats.TotalExpenses:C}</div>");
            html.AppendLine("</div>");
            html.AppendLine("<div class='stat-card'>");
            html.AppendLine("<div>Net Amount</div>");
            html.AppendLine($"<div class='stat-value balance'>{stats.NetAmount:C}</div>");
            html.AppendLine("</div>");
            html.AppendLine("<div class='stat-card'>");
            html.AppendLine("<div>Total Transactions</div>");
            html.AppendLine($"<div class='stat-value'>{stats.TransactionCount}</div>");
            html.AppendLine("</div>");
            html.AppendLine("</div>");
            
            // Transactions Table
            html.AppendLine("<table>");
            html.AppendLine("<thead>");
            html.AppendLine("<tr>");
            html.AppendLine("<th>Date</th>");
            html.AppendLine("<th>Description</th>");
            html.AppendLine("<th>Category</th>");
            html.AppendLine("<th>Account</th>");
            html.AppendLine("<th>Amount</th>");
            html.AppendLine("</tr>");
            html.AppendLine("</thead>");
            html.AppendLine("<tbody>");
            
            foreach (var transaction in transactions.OrderByDescending(t => t.Date))
            {
                var amountClass = transaction.Type switch
                {
                    TransactionType.Income => "amount-income",
                    TransactionType.Expense => "amount-expense",
                    TransactionType.Transfer => "amount-transfer",
                    _ => ""
                };
                
                var amountPrefix = transaction.Type switch
                {
                    TransactionType.Income => "+",
                    TransactionType.Expense => "-",
                    _ => ""
                };
                
                html.AppendLine("<tr>");
                html.AppendLine($"<td>{transaction.Date:MMM dd, yyyy}</td>");
                html.AppendLine($"<td>{transaction.Description}</td>");
                html.AppendLine($"<td>{transaction.CategoryName ?? "Uncategorized"}</td>");
                html.AppendLine($"<td>{transaction.AccountName}</td>");
                html.AppendLine($"<td class='{amountClass}'>{amountPrefix}{transaction.Amount:C}</td>");
                html.AppendLine("</tr>");
            }
            
            html.AppendLine("</tbody>");
            html.AppendLine("</table>");
            
            // Footer
            html.AppendLine("<div class='footer'>");
            html.AppendLine("<p>Report generated by FinanceFlow Pro</p>");
            html.AppendLine("</div>");
            
            html.AppendLine("</body>");
            html.AppendLine("</html>");
            
            return html.ToString();
        }
    }
}