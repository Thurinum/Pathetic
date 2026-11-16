using NeoVeldrid;

[assembly: ProfileCalls(typeof(CommandList),
    nameof(CommandList.SetPipeline),
    nameof(CommandList.SetComputeResourceSet),
    nameof(CommandList.Dispatch),
    nameof(CommandList.CopyTexture))]
[assembly: ProfileCalls(typeof(GraphicsDevice),
    nameof(GraphicsDevice.SubmitCommands),
    nameof(GraphicsDevice.SwapBuffers),
    nameof(GraphicsDevice.WaitForIdle),
    nameof(GraphicsDevice.ResizeMainWindow))]
[assembly: ProfileCalls(typeof(ResourceFactory),
    nameof(ResourceFactory.CreateBuffer),
    nameof(ResourceFactory.CreateTexture),
    nameof(ResourceFactory.CreateShader),
    nameof(ResourceFactory.CreateComputePipeline),
    nameof(ResourceFactory.CreateResourceSet),
    nameof(ResourceFactory.CreateResourceLayout))]
