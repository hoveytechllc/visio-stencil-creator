# Microsoft Visio Stencil Creator

This application creates a Microsoft Visio stencil file (vssx) from PNG images.

## Usage

Docker image `hoveytech/visio-stencil-creator` prebuild using this source. 
* Replace `<input-path>` with host path to images. 
* Replace `<output-path>` with host path to where stencil file should be placed.

```shell
docker run \
    -v <input-path>:/app/input-data \
    -v <output-path>:/app/output \
    hoveytech/visio-stencil-creator:v0.1 \
    "*.png" \
    "/app/input-data" \
    "/app/output/output.vssx"
```

**Additional notes:**
* First parameter, `"*.png"` supports glob pattern searching, which internally uses [Microsoft.Extensions.FileSystemGlobbing](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.filesystemglobbing?view=aspnetcore-2.2) Nuget Package.
* Third parameter can be changed to specific exact output filename.
