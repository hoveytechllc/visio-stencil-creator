# Microsoft Visio Stencil Creator

This application creates a Microsoft Visio stencil file (vssx) from PNG images.

## Usage

Docker image `hoveytech/visio-stencil-creator` prebuild using this source. Replace `<content-path>` with host path that contains images to be processed and where generated Visio stencil should be written to. 

```shell
docker run \
    -v <content-path>:/content \
    hoveytech/visio-stencil-creator:v0.2 \
    "--image-path=/app/content" \
    "--image-pattern=*.png" \
    "--output-filename=/content/output.vssx"
```

**Additional notes:**
* Parameter `--image-pattern` supports glob pattern searching, which internally uses [Microsoft.Extensions.FileSystemGlobbing](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.filesystemglobbing?view=aspnetcore-2.2) Nuget Package.
