using System;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Common.HttpRequest
{
    public class ApiClient : IApiClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly string _accessToken;
        private readonly TimeSpan _defaultTimeout;

        public ApiClient(IHttpClientFactory httpClientFactory, string accessToken = null, TimeSpan? defaultTimeout = null)
        {
            _httpClientFactory = httpClientFactory;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            _accessToken = accessToken;
            _defaultTimeout = defaultTimeout ?? TimeSpan.FromSeconds(60);
        }

        public async Task<TResponse> GetAsync<TResponse>(string url, TimeSpan? timeout = null)
        {
            using (HttpClient httpClient = CreateHttpClientWithTimeout(timeout))
            {
                AddAuthorizationHeader(httpClient);

                try
                {
                    HttpResponseMessage response = await httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    string responseBody = await response.Content.ReadAsStringAsync();
                    TResponse result = JsonSerializer.Deserialize<TResponse>(responseBody, _jsonOptions);

                    return result;
                }
                catch (Exception ex)
                {
                    // 处理异常
                    Console.WriteLine($"An error occurred while making GET request: {ex.Message}");
                    throw;
                }
            }
        }

        public async Task<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest request, TimeSpan? timeout = null)
        {
            using (HttpClient httpClient = CreateHttpClientWithTimeout(timeout))
            {
                AddAuthorizationHeader(httpClient);

                try
                {
                    string requestBody = JsonSerializer.Serialize(request, _jsonOptions);
                    HttpContent content = new StringContent(requestBody, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await httpClient.PostAsync(url, content);
                    response.EnsureSuccessStatusCode();

                    string responseBody = await response.Content.ReadAsStringAsync();
                    TResponse result = JsonSerializer.Deserialize<TResponse>(responseBody, _jsonOptions);

                    return result;
                }
                catch (Exception ex)
                {
                    // 处理异常
                    Console.WriteLine($"An error occurred while making POST request: {ex.Message}");
                    throw;
                }
            }
        }

        // 添加其他 RESTful 风格的请求方法，如 PutAsync、DeleteAsync 等

        private HttpClient CreateHttpClientWithTimeout(TimeSpan? timeout)
        {
            HttpClient httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = timeout ?? _defaultTimeout;
            return httpClient;
        }

        private void AddAuthorizationHeader(HttpClient httpClient)
        {
            if (!string.IsNullOrEmpty(_accessToken))
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            }
        }
    }
}
