fn startWebServer port = (
    assembly = dotNetClass "System.Reflection.Assembly"

    localPath = GetFilenamePath (GetSourceFileName())
    assemblyfile=localPath + "MAXScriptWebServer.dll"

    assembly.LoadFile (assemblyfile)

    cls = dotNetClass "MaxScriptWebServer.MaxScriptWebServer"
    server = dotNetObject cls

    server.execute port
)

startWebServer ("9021")