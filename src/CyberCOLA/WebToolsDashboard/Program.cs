using Com.Cybersoft4u.Test.Web.WebToolsDashboard.Components;
using Com.Cybersoft4u.Test.Web.WebToolsDashboard.Services;

var builder = WebApplication.CreateBuilder(args);

// Framework assets such as _framework/blazor.web.js are mapped only in Development when the app runs from
// source (dotnet run); without this a Production-environment run serves a blank page. No effect when published.
builder.WebHost.UseStaticWebAssets();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSingleton<RepoPaths>();
builder.Services.AddSingleton<JobRunner>();
builder.Services.AddSingleton<TranscriptHistory>();
builder.Services.AddSingleton<EnvironmentStatusService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<EnvironmentStatusService>());

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
