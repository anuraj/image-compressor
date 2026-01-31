using Actions.Core.Extensions;
using Actions.Core.Services;
using ImageCompressor.Handlers;
using ImageCompressor.Services;
using Microsoft.Extensions.DependencyInjection;

using var services = new ServiceCollection()
    .AddGitHubActionsCore()
    .BuildServiceProvider();

var core = services.GetRequiredService<ICoreService>();
var compressorService = new ImageCompressorService(core);
var commandLineHandler = new CommandLineHandler(core, compressorService);

var rootCommand = commandLineHandler.CreateRootCommand();

return rootCommand.Parse(args).Invoke();