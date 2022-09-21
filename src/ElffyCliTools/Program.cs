#nullable enable
using ElffyCliTools;

await ConsoleApp.Create(args, options =>
{
    options.StrictOption = true;
    options.ShowDefaultCommand = false;
    options.NoAttributeCommandAsImplicitlyDefault = true;
})
.AddCommands<Packager>()
.RunAsync();
