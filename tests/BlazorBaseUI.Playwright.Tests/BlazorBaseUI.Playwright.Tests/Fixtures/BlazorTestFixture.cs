using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace BlazorBaseUI.Playwright.Tests.Fixtures;

public class BlazorTestFixture : IAsyncLifetime
{
    private Process? serverProcess;
    private bool isInitialized;

    public string ServerAddress { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync()
    {
        if (isInitialized)
        {
            Console.WriteLine($"[BlazorTestFixture] Already initialized. ServerAddress: {ServerAddress}");
            return;
        }

        Console.WriteLine("[BlazorTestFixture] Initializing...");

        var projectDir = GetProjectDirectory();
        Console.WriteLine($"[BlazorTestFixture] Project directory: {projectDir}");

        var port = GetAvailablePort();
        ServerAddress = $"http://127.0.0.1:{port}";

        // Determine configuration from build output path
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var configuration = assemblyLocation.Contains("Release") ? "Release" : "Debug";

        serverProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --no-build -c {configuration} --no-launch-profile --urls {ServerAddress}",
                WorkingDirectory = projectDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                Environment =
                {
                    ["ASPNETCORE_ENVIRONMENT"] = "Development"
                }
            }
        };

        serverProcess.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Console.WriteLine($"[Server] {e.Data}");
        };
        serverProcess.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Console.WriteLine($"[Server Error] {e.Data}");
        };

        serverProcess.Start();
        serverProcess.BeginOutputReadLine();
        serverProcess.BeginErrorReadLine();

        await WaitForServerReadyAsync(ServerAddress, TimeSpan.FromSeconds(60));

        isInitialized = true;
        Console.WriteLine($"[BlazorTestFixture] Initialized. ServerAddress: {ServerAddress}");
    }

    public async ValueTask DisposeAsync()
    {
        Console.WriteLine("[BlazorTestFixture] Disposing...");

        if (serverProcess is not null && !serverProcess.HasExited)
        {
            try
            {
                serverProcess.Kill(entireProcessTree: true);
                await serverProcess.WaitForExitAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BlazorTestFixture] Error stopping server: {ex.Message}");
            }
            finally
            {
                serverProcess.Dispose();
            }
        }

        Console.WriteLine("[BlazorTestFixture] Disposed.");
    }

    private static string GetProjectDirectory()
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDir = Path.GetDirectoryName(assemblyLocation)!;

        var projectDir = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", ".."));

        if (Directory.Exists(projectDir) &&
            File.Exists(Path.Combine(projectDir, "BlazorBaseUI.Playwright.Tests.csproj")))
        {
            return projectDir;
        }

        Console.WriteLine($"[BlazorTestFixture] Warning: Could not find project directory at {projectDir}");
        return Directory.GetCurrentDirectory();
    }

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static async Task WaitForServerReadyAsync(string url, TimeSpan timeout)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < timeout)
        {
            try
            {
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound)
                {
                    return;
                }
            }
            catch
            {
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"Server at {url} did not become ready within {timeout.TotalSeconds} seconds");
    }
}
