using Microsoft.Extensions.Options;
using Pathetic.Renderer.Resources;

namespace Pathetic.App.RenderPasses;

public sealed class ConvergenceController(
    IOptions<TemporalAccumulationOptions> temporalAccumulation,
    IOptions<PathTracerOptions> pathTracer)
{
    private uint? _lastFrameIndex;
    private uint _framesSinceReset;
    private bool _runPathTracer = true;

    public uint MaxFrames
    {
        get
        {
            var taa = temporalAccumulation.Value;
            if (!taa.Enabled || taa.SampleBudget == 0)
                return 0;

            uint samplesPerPixel = Math.Max(1u, pathTracer.Value.SamplesPerPixel);
            return (taa.SampleBudget - 1) / samplesPerPixel + 1;
        }
    }

    public bool ShouldRunPathTracer(uint frameIndex, SceneChange sceneRevision)
    {
        if (_lastFrameIndex == frameIndex)
            return _runPathTracer;

        _lastFrameIndex = frameIndex;

        if (!temporalAccumulation.Value.Enabled || sceneRevision != SceneChange.None)
        {
            _framesSinceReset = 0;
            _runPathTracer = true;
            return _runPathTracer;
        }

        if (_runPathTracer && _framesSinceReset < uint.MaxValue)
            _framesSinceReset++;

        _runPathTracer = MaxFrames == 0 || _framesSinceReset < MaxFrames;
        return _runPathTracer;
    }
}
