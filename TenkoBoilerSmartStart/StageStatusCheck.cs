using Flurl.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace TenkoBoilerSmartStart
{
    public static class StageStatusCheck
    {
        [FunctionName("StageStatusCheck")]
        public static async Task Run(
        [TimerTrigger("0 */1 * * * *")] TimerInfo myTimer, // This CRON expression runs the function every 5 minutes
        ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            string login = Environment.GetEnvironmentVariable("LOGIN");
            string password = Environment.GetEnvironmentVariable("PASSWORD");

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                log.LogError("Login and password must be set in application settings.");
                return;
            }

            try
            {
                log.LogInformation($"Login {login}");

                var authResponse = await "https://my.tenko.ua/api/v1/auth"
                    .PostUrlEncodedAsync(new { login, password })
                    .ReceiveJson<AuthResponse>();

                if (authResponse.status != "ok")
                {
                    log.LogError("Authentication failed");
                    return;
                }

                var token = authResponse.token;

                // Determine stage_1 and stage_2 based on current time
                var currentTime = DateTime.UtcNow.AddHours(2); // Assuming the server is in UTC+2 time zone
                var currentHour = currentTime.Hour;
                var currentMinutes = currentTime.Minute;
                string stage_1, stage_2;

                if ((currentHour > 22 || currentHour < 4) || (currentHour == 22 && currentMinutes >= 0) || (currentHour == 4 && currentMinutes < 30))
                {
                    stage_1 = "On";
                    stage_2 = "Off";
                }
                else
                {
                    stage_1 = "Off";
                    stage_2 = "On";
                }

                var stageResponse = await new Flurl.Url("https://my.tenko.ua/api/v1/stages")
                    .WithOAuthBearerToken(token)
                    .PostJsonAsync(new
                    {
                        STG = new
                        {
                            stage_1,
                            stage_2
                        }
                    })
                    .ReceiveJson<StageResponse>();

                log.LogInformation($"Stage result: {stageResponse.message}");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error processing request");
            }
        }
    }

    public class AuthResponse
    {
        public string status { get; set; }
        public string token { get; set; }
    }

    public class StageResponse
    {
        public string message { get; set; }
    }
}
