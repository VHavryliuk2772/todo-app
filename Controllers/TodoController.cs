using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net;
using todo_app.Services;

namespace todo_app.Controllers
{
    [ApiController]
    [Route("")]
    public class TodoController : ControllerBase
    {
        private readonly ImageCacheService _imageService;
        private readonly HttpClient _todosHttpClient;

        public TodoController(ImageCacheService imageService, IHttpClientFactory httpClientFactory)
        {
            _imageService = imageService;
            _todosHttpClient = httpClientFactory.CreateClient("todosHttpClient");
        }

        [HttpGet("todo")]
        public async Task<IActionResult> GetTodoPage()
        {
            List<string> todos = new();
            string? loadError = null;

            try
            {
                var json = await _todosHttpClient.GetStringAsync("/api/todos");
                todos = JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
            }
            catch (Exception ex)
            {
                loadError = ex.Message;
            }

            var todosHtml = todos.Count == 0
                ? "<li><i>No todos yet</i></li>"
                : string.Join(Environment.NewLine, todos.Select(t =>
                    $"<li>{WebUtility.HtmlEncode(t)}</li>"));

            var errorHtml = string.IsNullOrWhiteSpace(loadError)
                ? ""
                : $@"<div class=""error"">Failed to load todos: {WebUtility.HtmlEncode(loadError!)}</div>";

           
            var apiBase = _todosHttpClient.BaseAddress?.ToString()?.TrimEnd('/') ?? "";

            var postUrl = $"/api/todos";

            // append apiBase to postUrl for local development
            //postUrl = $"{apiBase}{postUrl}";

            var html = $@"
                <!DOCTYPE html>
                <html lang=""en"">
                <head>
                  <meta charset=""utf-8"" />
                  <title>The project App</title>
                  <style>
                    body {{
                      font-family: Arial, sans-serif;
                      max-width: 600px;
                      margin: 0 auto;
                      padding: 20px;
                    }}
                    h1 {{
                      text-align: center;
                      margin-bottom: 20px;
                    }}
                    img {{
                      display: block;
                      margin: 0 auto 20px auto;
                      width: 500px;
                      height: 500px;
                      object-fit: cover;
                      border-radius: 8px;
                    }}
                    .input-row {{
                      display: flex;
                      gap: 10px;
                      margin-bottom: 12px;
                    }}
                    .input-row input[type='text'] {{
                      flex: 1;
                      padding: 8px;
                    }}
                    .input-row button {{
                      padding: 8px 16px;
                    }}
                    ul {{
                      margin: 0;
                      padding-left: 20px;
                    }}
                    .footer {{
                      margin-top: 30px;
                      text-align: center;
                      font-weight: bold;
                    }}
                    .error {{
                      color: #b00020;
                      margin: 10px 0;
                      padding: 8px;
                      border: 1px solid #b00020;
                      border-radius: 6px;
                    }}
                    #clientError {{
                      display: none;
                      color: #b00020;
                      margin: 8px 0 14px 0;
                    }}
                  </style>
                </head>
                <body>
                  <h1>The project App</h1>

                  <img src=""/todo/image"" alt=""Random image"" />

                  <div class=""input-row"">
                    <input id=""todoInput"" type=""text"" maxlength=""140"" placeholder=""Enter todo (max 140 chars)"" />
                    <button id=""todoButton"">Send</button>
                  </div>

                  <div id=""clientError""></div>
                  {errorHtml}

                  <ul>
                    {todosHtml}
                  </ul>

                  <div class=""footer"">Devops with Kubernetes 2025</div>

                  <script>
                    const input = document.getElementById('todoInput');
                    const btn = document.getElementById('todoButton');
                    const err = document.getElementById('clientError');

                    function showError(msg) {{
                      err.textContent = msg;
                      err.style.display = 'block';
                    }}

                    function clearError() {{
                      err.textContent = '';
                      err.style.display = 'none';
                    }}

                    btn.addEventListener('click', async function() {{
                      clearError();

                      const todo = (input.value || '').trim();
                      if (!todo) {{
                        showError('Todo is empty');
                        return;
                      }}
                      if (todo.length > 140) {{
                        showError('Max length is 140 chars');
                        return;
                      }}

                      btn.disabled = true;
                      try {{
                        const res = await fetch('{postUrl}', {{
                          method: 'POST',
                          headers: {{
                            'Content-Type': 'application/json'
                          }},
                          body: JSON.stringify(todo)
                        }});

                        if (!res.ok) {{
                          const text = await res.text();
                          showError(text || ('Failed: ' + res.status));
                          return;
                        }}

                        // trigger SSR re-fetch & re-render
                        window.location.reload();
                      }} catch (e) {{
                        showError('Network error while saving todo');
                      }} finally {{
                        btn.disabled = false;
                      }}
                    }});

                    input.addEventListener('input', clearError);
                  </script>
                </body>
                </html>";

            return Content(html, "text/html; charset=utf-8");
        }

        // endpoint, що віддає картинку
        [HttpGet("todo/image")]
        public async Task<IActionResult> GetImage()
        {
            var path = await _imageService.GetCurrentImagePathAsync();
            if (!System.IO.File.Exists(path))
            {
                return NotFound("Image not found");
            }

            var bytes = await System.IO.File.ReadAllBytesAsync(path);
            return File(bytes, "image/jpeg");
        }
    }
}
