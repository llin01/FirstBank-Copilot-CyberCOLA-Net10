using Com.Cybersoft4u.Test.Web.WebSMSWebApiServer.Components;
using Com.Cybersoft4u.Test.Web.WebSMSWebApiServer;

var builder = WebApplication.CreateBuilder(args);

// Add Blazor services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register SMS server manager as singleton
builder.Services.AddSingleton<SMSWebApiServerManager>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found");
// app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
