# Task 9 of 18: The WhatsApp channel over the Meta Cloud API

> Part of [Student Notifications (stage 1)](README.md), milestone B. Work on branch `notifications-whatsapp`. Independent of tasks 5–8 (it only needs `main`), so it may run in parallel with them.

**Files:**
- Create: `src\DrivingLessons.Application\Abstractions\IWhatsAppSender.cs` (+ `WhatsAppMessage`, `WhatsAppTemplate`, `WhatsAppSendResult`, `WhatsAppUnavailableException`)
- Modify: `src\DrivingLessons.Infrastructure\DrivingLessons.Infrastructure.csproj` (+ `Microsoft.Extensions.Http` 10.0.9)
- Create: `src\DrivingLessons.Infrastructure\WhatsAppChannel\WhatsAppOptions.cs`
- Create: `src\DrivingLessons.Infrastructure\WhatsAppChannel\WhatsAppCloudApiRequest.cs` (the JSON body, built from a `WhatsAppMessage`)
- Create: `src\DrivingLessons.Infrastructure\WhatsAppChannel\WhatsAppCloudApiSender.cs`
- Modify: `src\DrivingLessons.Infrastructure\DependencyInjection.cs`
- Modify: `src\DrivingLessons.Presentation.Web\appsettings.json`, `appsettings.Development.json`
- Modify: `docker-compose.yml`, `deploy\docker-compose.prod.yml` (if it lists `Email__*` itself), `.env.example`
- Test: `tests\DrivingLessons.Application.Test\WhatsAppChannel\WhatsAppCloudApiSenderTest.cs`, `WhatsAppOptionsTest.cs`, `StubHttpMessageHandler.cs` (new)
- Create: `docs\decisions\0011-whatsapp-over-cloud-api.md`
- Modify: `docs\tech-stack.md` (Notifications row), `docs\development\running-the-project.md` ("WhatsApp locally")

**Interfaces:**
- Consumes: nothing new from earlier tasks.
- Produces (task 10 calls it):

```csharp
namespace DrivingLessons.Application.Abstractions;

public interface IWhatsAppSender
{
    Task<WhatsAppSendResult> SendAsync(WhatsAppMessage message);
}

public enum WhatsAppTemplate
{
    Opening = 10,
    WindowChange = 20,
    Reminder = 30
}

public record WhatsAppMessage(
    string ToNumber,
    WhatsAppTemplate Template,
    IReadOnlyList<string> BodyParameters,
    string LinkToken);

public record WhatsAppSendResult
{
    public string? ProviderMessageId { get; }
    public string? RejectionReason { get; }
    public bool IsSent => ProviderMessageId is not null;
    public bool IsRejected => RejectionReason is not null;
    public bool IsDisabled => !IsSent && !IsRejected;

    public static WhatsAppSendResult Sent(string providerMessageId) { … }
    public static WhatsAppSendResult Rejected(string reason) { … }
    public static WhatsAppSendResult Disabled { get; } = …;
}

public class WhatsAppUnavailableException(string reason, Exception? inner = null) : Exception(reason, inner);
```

`ToNumber` is a raw string here on purpose: Application → Infrastructure ports carry primitives, as `EmailMessage` does. Task 10 fills it from `WhatsAppNumber.Value`.

- [ ] **Step 1: Write the failing sender tests**

`StubHttpMessageHandler : HttpMessageHandler` records the last `HttpRequestMessage` and its body, and returns a configured response or throws a configured exception. The sender is built with `new HttpClient(stub) { BaseAddress = … }`, `Options.Create(new WhatsAppOptions { … })` and a fake logger.

| Test | Asserts |
|------|---------|
| `Posts_To_The_Phone_Number_Messages_Endpoint_With_The_Token` | `POST https://graph.facebook.com/v{ApiVersion}/{PhoneNumberId}/messages`, `Authorization: Bearer {AccessToken}` |
| `Sends_The_Configured_Template_In_Hebrew` | body `messaging_product = "whatsapp"`, `to = "972501234567"`, `type = "template"`, `template.name = Templates.Opening`, `template.language.code = "he"` |
| `Sends_The_Body_Parameters_In_Order` | `components[0] = {type: "body", parameters: [{type: "text", text: p1}, …]}` |
| `Sends_The_Link_Token_As_The_Url_Button_Suffix` | `components[1] = {type: "button", sub_type: "url", index: "0", parameters: [{type: "text", text: token}]}` |
| `Maps_Each_Template_To_Its_Configured_Name` `[DataRow]` × 3 | |
| `Returns_The_Provider_Message_Id` | 200 `{"messages":[{"id":"wamid.X"}]}` → `Sent("wamid.X")` |
| `A_Client_Error_Is_A_Rejection_With_Metas_Code_And_Title` | 400 `{"error":{"code":131026,"error_data":{"details":"…"},"message":"Message undeliverable"}}` → `Rejected("131026 Message undeliverable")` |
| `Throttling_And_Server_Errors_Are_Unavailable` `[DataRow(429)] [DataRow(500)] [DataRow(503)]` | → `WhatsAppUnavailableException` |
| `Throttling_Codes_In_A_400_Are_Unavailable` `[DataRow(130429)] [DataRow(131000)] [DataRow(131016)]` | → `WhatsAppUnavailableException` |
| `A_Timeout_Is_Unavailable` | the stub throws `TaskCanceledException` → `WhatsAppUnavailableException` |
| `Disabled_Sends_Nothing_And_Says_So` | `Enabled = false` → no request, `Disabled`, one Information log `"WhatsApp disabled. Skipped {Template} message."` with no number in it |
| `Never_Logs_The_Number_Or_The_Parameters` | across a sent, a rejected and a disabled call, no log argument contains `972501234567` or a parameter |

`WhatsAppOptionsTest` (as `EmailOptionsTest`): `Disabled_Needs_Nothing`; `Enabled_Needs_Token_Phone_Number_Id_Version_And_Three_Templates` (each one missing yields its own `ValidationResult` naming the member); `Api_Base_Url_Must_Be_Absolute`.

- [ ] **Step 2: Run them to see them fail**

`dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~WhatsAppChannel"` → FAIL (types missing).

- [ ] **Step 3: Implement**

```bash
dotnet add src/DrivingLessons.Infrastructure package Microsoft.Extensions.Http --version 10.0.9
```

`WhatsAppOptions` (`SectionName = "WhatsApp"`, `IValidatableObject` like `EmailOptions`): `Enabled`, `ApiBaseUrl = "https://graph.facebook.com"`, `ApiVersion` (no default in code; `appsettings.json` pins the current Graph API version at implementation time), `PhoneNumberId`, `BusinessAccountId` (kept for the runbook and future template sync, not used to send), `AccessToken`, `LanguageCode = "he"`, `Templates { Opening, WindowChange, Reminder }`, `TimeoutSeconds = 15`.

`WhatsAppCloudApiRequest.From(WhatsAppMessage, WhatsAppOptions)` builds the anonymous-free DTO graph (records with `[JsonPropertyName]` snake_case) of Step 1.

`WhatsAppCloudApiSender(HttpClient, IOptions<WhatsAppOptions>, ILogger<WhatsAppCloudApiSender>)`:
1. Disabled → log, return `Disabled`.
2. `PostAsJsonAsync($"{ApiVersion}/{PhoneNumberId}/messages", request)`. Catch `HttpRequestException` and `TaskCanceledException` → `WhatsAppUnavailableException`.
3. 2xx → read `messages[0].id` → `Sent`.
4. 429 or ≥500 → `WhatsAppUnavailableException($"WhatsApp HTTP {status}")`.
5. Other 4xx → read `error.code` and `error.message` (fall back to the status line if the body isn't Meta's shape). A throttling code → unavailable; anything else → `Rejected($"{code} {message}")`.

DI:

```csharp
services.AddOptions<WhatsAppOptions>()
    .Bind(configuration.GetSection(WhatsAppOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

services.AddHttpClient<IWhatsAppSender, WhatsAppCloudApiSender>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<WhatsAppOptions>>().Value;
    client.BaseAddress = new Uri(options.ApiBaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.AccessToken);
});
```

- [ ] **Step 4: Configuration, compose and env**

`appsettings.json`: a `WhatsApp` section with the full shape, `Enabled: false`, blanks, template names `drivecal_opening`, `drivecal_window_change` and `drivecal_reminder` (they must match WhatsApp Manager, runbook). `appsettings.Development.json`: `Enabled: false`, `ApiBaseUrl: "http://localhost:9099"` (the local fake of `running-the-project.md`).

`docker-compose.yml`, beside `Email__*`: `WhatsApp__Enabled: ${WHATSAPP_ENABLED:-false}`, `WhatsApp__AccessToken`, `WhatsApp__PhoneNumberId`, `WhatsApp__BusinessAccountId`, `WhatsApp__ApiVersion` and the three `WhatsApp__Templates__*`. `.env.example`: the same `WHATSAPP_*` names, with a header comment naming ADR 0011 and "keep disabled until the templates are approved; the token is a permanent system-user token, production `.env` only".

- [ ] **Step 5: Record the decision and the local setup**

`docs\decisions\0011-whatsapp-over-cloud-api.md`:
- **Context:** intent §2 and §5.7.
- **Decision:** the Cloud API directly over `HttpClient`, no SDK and no BSP; Utility templates in Hebrew (decision 15); consent (decision 12); quiet hours (decision 16); the error classification (decision 22); configuration names.
- **Rejected:** a BSP (Twilio and the like) adds a contract and a margin for nothing this needs; the WhatsApp Business app on the same number ("Coexistence", open question 6) is an onboarding choice, not a code one.
- **Consequences:** per-message cost (Open Item 6); templates are owned in WhatsApp Manager, so a wording change is a re-approval there and not a deploy; no delivery status until the webhook stage.

`running-the-project.md` → "WhatsApp locally": with `WhatsApp:Enabled=false` every notification is recorded `Skipped (WhatsApp disabled)`. To see real requests, run the fake (`node <scratchpad>/fake-whatsapp.js`, listening on 9099, task 14 Step 2 gives the script) and start the API with `--WhatsApp:Enabled=true --WhatsApp:AccessToken=dev --WhatsApp:PhoneNumberId=dev --WhatsApp:ApiVersion=v0`. `tech-stack.md`: add WhatsApp to the integrations table.

- [ ] **Step 6: Run and commit**

```bash
dotnet build
dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj
git add src tests docker-compose.yml deploy .env.example docs/decisions/0011-whatsapp-over-cloud-api.md docs/tech-stack.md docs/development/running-the-project.md
git commit -m "feat(whatsapp): Cloud API sender for approved templates, disabled by default"
```
