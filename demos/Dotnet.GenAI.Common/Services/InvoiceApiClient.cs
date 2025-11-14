using Dotnet.GenAI.Common.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Dotnet.GenAI.Common.Services
{
    public class InvoiceApiClient
    {
        private readonly HttpClient _httpClient = new();
        private readonly string _baseUrl = "https://localhost:5000/api/invoices";
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public async Task<List<Invoice>> ListInvoicesAsync(
            CancellationToken ct = default)
        {
            var response = await _httpClient
                .GetAsync(
                    _baseUrl, 
                    ct);

            response.EnsureSuccessStatusCode();
            
            var json = await response
                .Content
                .ReadAsStringAsync(ct);

            var invoices = JsonSerializer.Deserialize<List<Invoice>>(
                json, 
                _jsonOptions);
            
            return invoices ?? [];
        }

        public async Task<Invoice> FindInvoiceByNameAsync(
            string name,
            CancellationToken ct = default)
        {
            var url = $"{_baseUrl}/by-description?description={Uri.EscapeDataString(name)}";
            
            var response = await _httpClient
                .GetAsync(
                    url, 
                    ct);
            
            response.EnsureSuccessStatusCode();
            
            var json = await response
                .Content
                .ReadAsStringAsync(ct);
            
            var invoice = JsonSerializer.Deserialize<Invoice>(
                json, 
                _jsonOptions);
            
            return invoice!;
        }

        public async Task<Invoice> CreateInvoiceAsync(
            CreateInvoiceRequest request,
            CancellationToken ct = default)
        {
            var newInvoice = new Invoice()
            {
                Id = 0,
                Description = request.Description,
                Status = "Pending",
                Amount = request.Amount,
                Date = DateTime.Now,
                Due = request.Due ?? DateTime.Today.AddMonths(1)
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(
                    newInvoice, 
                    _jsonOptions), 
                Encoding.UTF8, 
                "application/json");
            
            var response = await _httpClient
                .PostAsync(
                    _baseUrl, 
                    jsonContent,
                    ct);
            
            response.EnsureSuccessStatusCode();
            
            var responseJson = await response
                .Content
                .ReadAsStringAsync(ct);
            
            var createdInvoice = JsonSerializer.Deserialize<Invoice>(
                responseJson, 
                _jsonOptions)!;
            
            return createdInvoice;
        }

        public async Task MarkAsPaidAsync(
            string invoiceId,
            CancellationToken ct = default)
        {
            var request = new UpdateInvoiceRequest
            {
                Status = "Paid"
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(
                    request, 
                    _jsonOptions), 
                Encoding.UTF8, 
                "application/json");
            
            var url = $"{_baseUrl}/{invoiceId}/status";
            
            var response = await _httpClient
                .PostAsync(
                    url, 
                    jsonContent,
                    ct);
            
            response.EnsureSuccessStatusCode();
        }
    }
}
