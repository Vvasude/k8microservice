
using TransactionService.Dtos;

namespace TransactionService.Clients;

public class AccountClient
{
    private readonly HttpClient _http;

    public AccountClient(HttpClient http)
    {
        _http = http;
    }

    // GET /accounts/{id} — returns the account, or null if 404
    public async Task<AccountDto?> GetAccountAsync(int id)
    {
        var response = await _http.GetAsync($"/accounts/{id}");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<AccountDto>();
    }

    // POST /accounts/{id}/debit  {amount}
    public async Task<bool> DebitAsync(int id, decimal amount)
    {
        var response = await _http.PostAsJsonAsync($"/accounts/{id}/debit", new { amount });
        return response.IsSuccessStatusCode;
    }

    // POST /accounts/{id}/credit {amount} — you write this one (mirror of DebitAsync)
    public async Task<bool> CreditAsync(int id, decimal amount)
    {
        // TODO
        var response = await _http.PostAsJsonAsync($"/accounts/{id}/credit", new { amount });
        return response.IsSuccessStatusCode;
    }
}