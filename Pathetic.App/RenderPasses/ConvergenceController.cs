using Microsoft.Extensions.Options;
using Pathetic.Renderer.Resources;

namespace Pathetic.App.RenderPasses;

public readonly record struct ConvergencePlan(
    bool RunPathTracer,
    bool ResetAccumulation,
    uint SamplesPerPixel,
    uint MaxFrames);

public sealed class ConvergenceController(
    IOptions<TemporalAccumulationOptions> temporalAccumulation,
    IOptions<PathTracerOptions> pathTracer,
    RenderDebugSettings debug)
{
    private uint? _lastFrameIndex;
    private uint _framesSinceReset;
    private bool _runPathTracer = true;
    private bool _wasMoving;
    private bool _wasTaaEnabled = true;
    private uint _debugRevision;
    private bool _invalidated;
    private ConvergencePlan _plan;

    public void Invalidate() => _invalidated = true;

    public ConvergencePlan GetPlan(uint frameIndex, SceneChange sceneRevision)
    {
        if (_lastFrameIndex == frameIndex)
            return _plan;

        bool firstFrame = _lastFrameIndex is null;
        _lastFrameIndex = frameIndex;

        var taa = temporalAccumulation.Value;
        bool taaEnabled = taa.Enabled;
        bool isMoving = sceneRevision != SceneChange.None;

        bool settingsChanged = _debugRevision != debug.Revision;
        _debugRevision = debug.Revision;

        bool resetAccumulation = firstFrame || _invalidated || !taaEnabled || isMoving || _wasMoving || !_wasTaaEnabled || settingsChanged;
        _invalidated = false;
        uint maxFrames = GetMaxFrames(taa);

        if (resetAccumulation)
        {
            _framesSinceReset = 0;
            _runPathTracer = true;
        }
        else
        {
            if (_runPathTracer && _framesSinceReset < uint.MaxValue)
                _framesSinceReset++;

            _runPathTracer = maxFrames == 0 || _framesSinceReset < maxFrames;
        }

        uint samplesPerPixel = isMoving
            ? pathTracer.Value.MovingSamplesPerPixel
            : pathTracer.Value.SamplesPerPixel;

        _plan = new ConvergencePlan(
            _runPathTracer,
            resetAccumulation,
            Math.Max(1u, samplesPerPixel),
            maxFrames);

        _wasMoving = isMoving;
        _wasTaaEnabled = taaEnabled;
        return _plan;
    }

    private uint GetMaxFrames(TemporalAccumulationOptions taa)
    {
        if (!taa.Enabled || taa.SampleBudget == 0)
            return 0;

        uint samplesPerPixel = Math.Max(1u, pathTracer.Value.SamplesPerPixel);
        return (taa.SampleBudget - 1) / samplesPerPixel + 1;
    }
}
