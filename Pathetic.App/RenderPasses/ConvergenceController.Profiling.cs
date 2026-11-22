namespace Pathetic.App.RenderPasses;

// Everything here only exists to be shown by the profiler.
public sealed partial class ConvergenceController
{
    [ProfileTag("convergence.spp")]
    internal uint SamplesPerPixel => _plan.SamplesPerPixel;

    [ProfileTag("convergence.frames")]
    internal uint FramesSinceReset => _framesSinceReset;

    [ProfileTag("convergence.max_frames")]
    internal uint MaxFrames => _plan.MaxFrames;

    [ProfileTag("convergence.reset")]
    internal bool IsResetting => _plan.ResetAccumulation;

    [ProfileTag("convergence.path_tracing")]
    internal bool IsPathTracing => _plan.RunPathTracer;
}
