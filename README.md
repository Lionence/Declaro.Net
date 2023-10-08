# Declaro.Net Library
Declaro.Net is a powerful and flexible library that simplifies making HTTP requests in your .NET applications. It is designed to handle HTTP requests for Declaro.NET based on implicit definitions, allowing you to focus on your application logic without worrying about the intricacies of HTTP communication.

## Features
- Simple and Intuitive: Declaro.Net provides a straightforward way to make HTTP requests without the need to write boilerplate code for handling requests and responses.
- Implicit Definitions: Define your API endpoints and request/response types using attributes, reducing the need for manual configuration.
- Caching Support: Built-in caching mechanism allows you to cache responses and improve performance, with customizable caching intervals.
- Flexible Configuration: Customize your HTTP requests with headers, authentication, and other options to suit your specific requirements.
- Error Handling: Declaro.Net automatically handles unsuccessful HTTP responses and provides detailed exception handling for easier debugging.
- Asynchronous Operations: Perform HTTP requests asynchronously, ensuring your application remains responsive and scalable.

## Installation
To use Declaro.Net in your project, simply install the package via NuGet Package Manager:

	dotnet add package Declaro.Net

## Getting Started

### 1. Define Your API Endpoints and Data Models
	[Http(ApiEndpoint = "api/weather")]
    [HttpGet(ApiEndpoint = "api/weather?City={0}&Date={1}", RequestType = typeof(WeatherRequest))]
    public class WeatherResponse
    {
        public int Celsius { get; set; }
        public string? City { get; set; }
    }

    public class WeatherRequest
    {
        [RequestArgument(0)]
        public string? City { get; set; }

        [RequestArgument(1)]
        public string? Date { get; set; }
    }

### 2. Add to Dependency Injection

    builder.Services.AddHttpService();

### 3. Make HTTP Requests

    // Make a GET request
    WeatherResponse weather = await httpService.GetAsync<WeatherResponse, WeatherRequest>(new WeatherRequest { City = "New York", Date = "2023-10-08" });

    // Make a POST request
    var requestData = new WeatherRequest { City = "London", Date = "2023-10-08" };
    WeatherResponse response = await httpService.PostAsync<WeatherResponse, WeatherRequest>(requestData);

## API Reference

### 'HttpService'
**Methods**
- GetAsync<TResponse, TRequest>(TRequest data, CancellationToken ct = default): Sends an HTTP GET request.
- ListAsync<TResponse, TRequest>(TRequest data, CancellationToken ct = default): Sends a BULK query (HTTP POST) request.
- PostAsync<TResponse, TRequest>(TRequest data, CancellationToken ct = default): Sends an HTTP POST request.
- PutAsync<TResponse, TRequest>(TRequest data, CancellationToken ct = default): Sends an HTTP PUT request.
- PatchAsync<TResponse, TRequest>(TRequest data, CancellationToken ct = default): Sends an HTTP PATCH request.
- DeleteAsync<TData>(TData data, CancellationToken ct = default): Sends an HTTP DELETE request.

**Constructors**
- HttpService(IHttpClientFactory httpClientFactory, IMemoryCache memoryCache): Initializes the HttpService with the provided IHttpClientFactory and IMemoryCache.
### 'HttpAttribute'
- ApiEndpoint: The API endpoint to be used by the request. Supports templating.
- Authorization: Authorization method for the API.
- Headers: Default headers to add to the request.
- RequestType: The request type to be used in the HTTP body.
- ResponseType: The response type that your request is expected to get.
- ArgumentProperties: Properties to scrape for arguments before making a request.
### 'RequestArgumentAttribute'
- Index: The index to associate with the request argument.

## Contribution
We welcome contributions! If you find a bug or have a feature request, please open an issue. If you want to contribute code, please contact us so we can add you to project contributors!

## License
This project is licensed under the MIT License.