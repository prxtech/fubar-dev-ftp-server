// <copyright file="FtpServerTestsBase.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Meziantou.Extensions.Logging.Xunit.v3;

using Microsoft.Extensions.Logging;

using Xunit;

namespace FubarDev.FtpServer.Tests
{
    public class FtpServerTestsBase : IAsyncLifetime
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private ServiceProvider? _serviceProvider;
        private IFtpServer? _server;

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpServerTestsBase"/> class.
        /// </summary>
        /// <param name="testOutputHelper">The test output helper.</param>
        protected FtpServerTestsBase(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        /// <summary>
        /// Gets the FTP server.
        /// </summary>
        public IFtpServer Server => _server ?? throw new InvalidOperationException();

        /// <summary>
        /// Gets the service provider.
        /// </summary>
        public IServiceProvider ServiceProvider => _serviceProvider ?? throw new InvalidOperationException();

        /// <inheritdoc />
        public virtual async ValueTask InitializeAsync()
        {
            var services = new ServiceCollection()
               .AddLogging(
                    lb =>
                    {
                        // lb.AddConsole();
                        lb.AddProvider(new XUnitLoggerProvider(_testOutputHelper));
                        lb.SetMinimumLevel(LogLevel.Trace);
                        lb.AddFilter("System", LogLevel.Warning);
                        lb.AddFilter("Microsoft", LogLevel.Warning);
                        lb.AddFilter("FubarDev.FtpServer", LogLevel.Trace);
                    })
               .AddFtpServer(
                    opt => Configure(opt));
            services = Configure(services);
            _serviceProvider = services.BuildServiceProvider(true);
            _server = _serviceProvider.GetRequiredService<IFtpServer>();
            await _server.StartAsync(default);
        }

        /// <inheritdoc />
        public virtual async ValueTask DisposeAsync()
        {
            await Server.StopAsync(default).ConfigureAwait(false);
            if (_serviceProvider != null)
            {
                await _serviceProvider.DisposeAsync();
            }

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Basic FTP server configuration for the basic configuration through <see cref="Configure(IFtpServerBuilder)"/>.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The modified service collection.</returns>
        protected virtual IServiceCollection Configure(IServiceCollection services)
        {
            return services
               .Configure<FtpServerOptions>(
                    opt =>
                    {
                        // IPv4 localhost
                        opt.ServerAddress = "127.0.0.1";

                        // Dynamic port
                        opt.Port = 0;
                    });
        }

        /// <summary>
        /// Basic configuration using the FTP server builder.
        /// </summary>
        /// <param name="builder">The FTP server builder used for the configuration.</param>
        /// <returns>The modified FTP server builder.</returns>
        protected virtual IFtpServerBuilder Configure(IFtpServerBuilder builder)
        {
            return builder
               .EnableAnonymousAuthentication()
               .UseSingleRoot()
               .UseInMemoryFileSystem();
        }
    }
}
