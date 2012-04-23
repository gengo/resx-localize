@REM Generates localized .DLL files for each language translated

@SET PATH="c:\Program Files (x86)\Microsoft SDKs\Windows\v7.0A\Bin"

@FOR /F "tokens=*" %%G IN ('dir /B /A:D') DO (
    @cd %%G
    @REM Generate .resources
    @resgen ResourceUI.%%G.resx

    @REM Compile to DLL
    @REM Change MyAssemblyName and MyExeModuleName to actual names
    @al /t:lib /embed:ResourceUI.%%G.resources,MyAssemblyName.ResourceUI.%%G.resources /culture:%%G /out:MyExeModuleName.resources.dll
    
    @cd..
)