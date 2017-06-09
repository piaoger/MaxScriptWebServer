# MaxScriptWebServer

Embedded MaxScript WebServer to remote execute Maxscript

## how to use

```
cd dist
start startmax.bat
```

## how to tweak

1. You can change listening port in runwebserver.ms

2. You can even modify C# source for more customizations.

```
open the source in visual studio 2013
add reference: Autodesk.Model.dll, UiViewModes.dll
build in release mode
copy the result to dist folder from bin/release
```

## Reference

[Embedding a Web Server in 3ds Max using .NET](http://area.autodesk.com/blogs/the-3ds-max-blog/embedding-a-web-server-in-3ds-max-using-net)

[Running Scripts from the Command Line](https://knowledge.autodesk.com/support/3ds-max/learn-explore/caas/CloudHelp/cloudhelp/2015/ENU/3DSMax/files/GUID-BCB04DEC-7967-4091-B980-638CFDFE47EC-htm.html)