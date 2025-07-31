using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.SemanticKernel.ChatCompletion;
using OllamaSharp;
using OllamaSharp.Models.Chat;
using System.Text;
using System.Text.Json;
using Ollama;
using IronOcr;


namespace ContractReader.Controllers
{


    public class FileUploadModel
    {
        public required string ImageFile { get; set; }
    }


    [Route("api/[controller]")]
    [ApiController]
    public class AIController : ControllerBase
    {
        [HttpPost("senddoc")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SendImage([FromForm] FileUploadModel imageFileModel)
        {
            var imageFile = imageFileModel.ImageFile;
            var readFile=string.Empty;
            var ocr = new IronTesseract();

            using (var ocrInput = new OcrInput())
            {
                ocrInput.LoadImage(imageFile);
                //ocrInput.LoadPdf("document.pdf");
                var ocrResult = ocr.Read(ocrInput);
                readFile = ocrResult.Text;
            }

            if (imageFileModel.ImageFile == null || imageFileModel.ImageFile.Length == 0)
            {
                return BadRequest("Invalid or missing file path.");
            }


            var requestData = new
            {
                model = "qwen2.5vl",
                stream = false,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = $"Rate this contracts fairness to the signer on a scale of 1-100 and explain why using direct evidence from each of the clauses in the contract: {readFile}"
                    }
                }
            };

            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5)
            };


            var json = JsonSerializer.Serialize(requestData);


            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("http://localhost:11434/api/chat", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, responseString);

            return Ok(responseString);
        }
    }
}