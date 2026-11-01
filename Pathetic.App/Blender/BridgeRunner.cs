using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Pathetic.App.Blender;

/// <summary>
/// Persistent stdio bridge. Blender keeps this process alive, sends tiny JSON commands
/// on stdin, and receives length-prefixed RGBA frames on stdout.
/// </summary>
public sealed class BridgeRunner(IServiceProvider provider)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public void Run()
    {
        RenderSession? session = null;

        using Stream stdin = Console.OpenStandardInput();
        using Stream stdout = Console.OpenStandardOutput();
        using StreamReader reader = new(stdin, Encoding.UTF8, false, 4096);
        using BinaryWriter writer = new(stdout, Encoding.UTF8, leaveOpen: true);

        ConcurrentQueue<string> commands = new();
        int closed = 0;

        Thread readerThread = new(() =>
        {
            try
            {
                while (reader.ReadLine() is { } line)
                    commands.Enqueue(line);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
            }
            finally
            {
                Volatile.Write(ref closed, 1);
            }
        })
        {
            IsBackground = true,
            Name = "Pathetic bridge command reader",
        };
        readerThread.Start();

        Stopwatch frameTimer = new();

        try
        {
            while (true)
            {
                while (commands.TryDequeue(out string? line))
                {
                    if (!HandleCommand(line, ref session))
                        return;
                }

                if (Volatile.Read(ref closed) != 0 && commands.IsEmpty)
                    return;

                if (session is { IsLoaded: true })
                {
                    frameTimer.Restart();
                    byte[] pixels = session.RenderFrame();
                    if (pixels.Length > 0)
                    {
                        SendFrame(writer, session.Width, session.Height, session.FrameIndex, pixels);
                    }

                    int elapsed = (int)frameTimer.ElapsedMilliseconds;
                    if (elapsed < 16)
                        Thread.Sleep(16 - elapsed);
                }
                else
                {
                    Thread.Sleep(5);
                }
            }
        }
        finally
        {
            session?.Dispose();
        }
    }

    private bool HandleCommand(string line, ref RenderSession? session)
    {
        using JsonDocument document = JsonDocument.Parse(line);
        JsonElement root = document.RootElement;
        string type = root.GetProperty("type").GetString() ?? string.Empty;

        switch (type)
        {
            case "load":
            {
                BlenderRenderRequest request = root.TryGetProperty("scene", out JsonElement sceneElement)
                    ? sceneElement.Deserialize<BlenderRenderRequest>(SerializerOptions)
                        ?? throw new InvalidOperationException("load command has an empty scene.")
                    : BlenderRenderRequest.Load(
                        root.GetProperty("path").GetString()
                        ?? throw new InvalidOperationException("load command has no scene or path."));

                session ??= ActivatorUtilities.CreateInstance<RenderSession>(provider);
                session.LoadScene(request);
                return true;
            }
            case "camera":
            {
                CameraDto? camera = root.GetProperty("camera").Deserialize<CameraDto>(SerializerOptions);
                if (camera is not null)
                    session?.UpdateCamera(camera);
                return true;
            }
            case "settings":
                session?.UpdateSettings(
                    root.TryGetProperty("samplesPerPixel", out JsonElement samples) ? samples.GetInt32() : null,
                    root.TryGetProperty("maxBounces", out JsonElement bounces) ? bounces.GetInt32() : null,
                    root.TryGetProperty("tonemapper", out JsonElement tonemapper) ? tonemapper.GetString() : null,
                    root.TryGetProperty("exposure", out JsonElement exposure) ? exposure.GetSingle() : null);
                return true;
            case "close":
                return false;
            default:
                Console.Error.WriteLine($"Unknown bridge command: {type}");
                return true;
        }
    }

    private static void SendFrame(
        BinaryWriter writer,
        uint width,
        uint height,
        uint frameIndex,
        byte[] pixels)
    {
        int rowSize = checked((int)width * 4);
        byte[] flipped = new byte[pixels.Length];

        for (int y = 0; y < (int)height; y++)
        {
            int source = y * rowSize;
            int destination = ((int)height - 1 - y) * rowSize;
            Buffer.BlockCopy(pixels, source, flipped, destination, rowSize);
        }

        writer.Write(12 + flipped.Length);
        writer.Write((int)width);
        writer.Write((int)height);
        writer.Write((int)frameIndex);
        writer.Write(flipped);
        writer.Flush();
    }
}
