using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Rasterizer.Core
{
    public class Meshy3DResult
    {
        public string id { get; set; }
        public Dictionary<string, string> model_urls { get; set; }
        public string status { get; set; }
    }

    public class MeshyClient
    {
        private readonly string apiKey;
        private readonly string downloadFolder;
        private readonly HttpClient httpClient;
        private const string CreateUrl = "https://api.meshy.ai/openapi/v2/text-to-3d";

        public MeshyClient(string apiKey, string downloadFolder)
        {
            this.apiKey = apiKey;
            this.downloadFolder = downloadFolder;
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        }

        public async Task<string> GenerateAndDownloadObjAsync(string prompt)
        {
            // Prepare the payload
            var payload = new
            {
                mode = "preview",
                prompt = prompt,
                topology = "triangle",
                target_polycount = 750
            };

            string postData = JsonSerializer.Serialize(payload);

            // Submit job
            var response = await httpClient.PostAsync(
                CreateUrl,
                new StringContent(postData, Encoding.UTF8, "application/json")
            );
            string responseBody = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Error submitting task: {response.StatusCode}\n{responseBody}");

            var resultJson = JsonDocument.Parse(responseBody);
            string taskId = resultJson.RootElement.GetProperty("result").GetString();

            // Polling
            Meshy3DResult taskResult = null;
            string status = "";
            while (true)
            {
                await Task.Delay(2500);
                var req = await httpClient.GetAsync($"{CreateUrl}/{taskId}");
                var body = await req.Content.ReadAsStringAsync();
                if (!req.IsSuccessStatusCode)
                    throw new Exception($"Error polling task: {req.StatusCode}\n{body}");
                taskResult = JsonSerializer.Deserialize<Meshy3DResult>(body);
                status = taskResult.status;
                if (status == "SUCCEEDED" || status == "FAILED" || status == "CANCELED")
                    break;
            }

            if (status == "SUCCEEDED" && taskResult.model_urls != null && taskResult.model_urls.TryGetValue("obj", out string objUrl))
            {
                Directory.CreateDirectory(downloadFolder);
                string safePrompt = string.Concat(prompt.Split(Path.GetInvalidFileNameChars()));
                string filename = $"meshy_{safePrompt}_{taskResult.id}.obj".Replace(" ", "_");
                string path = Path.Combine(downloadFolder, filename);

                using (var modelResp = await httpClient.GetAsync(objUrl))
                {
                    modelResp.EnsureSuccessStatusCode();
                    using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
                    {
                        await modelResp.Content.CopyToAsync(fs);
                    }
                }
                return path;
            }
            else
            {
                throw new Exception("Model generation failed, was cancelled, or OBJ is not available.");
            }
        }
    }
}
