# Pathetic Renderer for Blender

This extension registers Pathetic as a Blender render engine. It exports the evaluated
Blender scene to Pathetic's headless bridge, renders with the real Pathetic GPU path
tracer, reads back Pathetic's `PresentColor` texture, and displays that image in
Blender's render result.

## What is supported

- Final renders (`F12`) and animation renders.
- Triangle meshes through the scene BVH and Möller–Trumbore ray/triangle intersection.
- Analytic spheres and planes (Pathetic's native primitives).
- Blender point/area/spot lights approximated as emissive spheres.
- Principled BSDF `Base Color`, plus `Emission Color` + `Emission Strength`.
- Blender camera position/orientation, vertical field of view, depth of field
  (focus distance and f-stop), and render resolution.
- Pathetic's AgX/ACES/Reinhard tonemappers and exposure.
- Blender Z-up → Pathetic Y-up conversion.

## What is not supported yet

- Interactive `Rendered` viewport shading. `F12` is the supported path.
- Image textures, procedural textures, normals, roughness, metallic, transparency, etc.
- Smooth normals and per-vertex normals. Triangles are flat-shaded.
- Multiple scattering materials, volumes, denoising, motion blur.
- Blender camera shift, lens distortion, panoramic cameras.
- Blender world/environment lighting. Pathetic has a fixed analytic sky gradient.
- Area/spot light shapes. They become emissive spheres.

## Build Pathetic

```powershell
cd F:/Projects/Others/Pathetic-blender
dotnet build Pathetic.slnx -c Release
```

The executable is produced at:

```text
Pathetic.App/bin/Release/net10.0/win-x64/Pathetic.App.exe
```

The current build is framework-dependent, so the .NET 10 runtime must be installed. If
you want a self-contained executable, publish with `-r win-x64 --self-contained true`
and point the add-on at the published `Pathetic.App.exe`. Keep `Pathetic.ini` and the
`Shaders/` directory next to the executable; the headless bridge loads both at startup.

## Install the Blender extension

The add-on lives in `Blender/` and has a `blender_manifest.toml`, so it can be installed
as a folder or built into a zip:

```powershell
blender --command extension build --source-dir Blender --output-dir dist
```

Then in Blender: `Edit > Preferences > Add-ons > Install from Disk…` and pick the zip,
or point Blender at the `Blender/` folder during development.

After enabling the add-on, open the add-on preferences and set **Pathetic Executable**
to the path above. The add-on also checks `PATHETIC_EXE` and the repository build
locations.

## Render

1. Set `Render Properties > Render Engine` to **Pathetic**.
2. Open the **Pathetic** panel.
3. Set samples, samples per frame, bounces and tonemapper.
4. Press `F12`.

The extension runs Pathetic as a subprocess, waits for the headless bridge to finish,
and writes the result into Blender's Render Result.

## GPU / driver safety

Pathetic renders in short GPU dispatches. `Samples per Frame` controls how much work is
submitted in one dispatch; smaller values are safer on GPUs/drivers that reset when a
single dispatch runs too long. If your display resets, lower it (and/or render at a
lower resolution).

The Blender add-on itself does not create or destroy a Vulkan device; only the Pathetic
subprocess does, once per rendered frame.

## Protocol

The bridge writes a raw file:

```text
uint32 width (LE)
uint32 height (LE)
width * height * 4 bytes (RGBA, sRGB, top-to-bottom)
```

Blender converts it to linear float pixels and flips it vertically because Blender's
render-result buffer is bottom-to-top. Pathetic's present texture is already tonemapped;
the add-on sets Blender's view transform to `Standard` by default so it is not tonemapped
a second time.
