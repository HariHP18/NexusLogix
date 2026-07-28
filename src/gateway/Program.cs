var builder = WebApplication.CreateBuilder(args);

// Load the YARP configurations from appsettings.json
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Enable YARP routing
app.MapReverseProxy();

app.Run();