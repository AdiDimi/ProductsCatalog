using Serilog;
using Serilog.Context;
//using Serilog.Sinks.ApplicationInsights;

namespace AdsApi.Infrastructure.Logging;

public static class LoggingExtensions
	{
	public static void AddStructuredLogging (this WebApplicationBuilder builder)
		{
		builder.Host.UseSerilog((ctx, cfg) =>
		{
			//// Read general settings from configuration
			cfg.ReadFrom.Configuration(ctx.Configuration)
			   .Enrich.FromLogContext()
			   .Enrich.WithMachineName()
			   .Enrich.WithProcessId()
			   .Enrich.WithThreadId();

			//// Ensure MSSqlServer sink has proper column options (if not fully expressible in JSON)
			//var sqlColumnOptions = new ColumnOptions();

			//// Keep standard columns and add SourceContext
			//sqlColumnOptions.AdditionalColumns = new Collection<SqlColumn>
			//	{
			//		new SqlColumn
			//		{
			//			ColumnName = "SourceContext",
			//			DataType = SqlDbType.NVarChar,
			//			AllowNull = true
			//		}
			//	};

			//var sqlConnectionString = ctx.Configuration.GetConnectionString("ShoppingConn");
			//if (!string.IsNullOrWhiteSpace(sqlConnectionString))
			//	{
			//	cfg.WriteTo.MSSqlServer(
			//		connectionString: sqlConnectionString,
			//		sinkOptions: new MSSqlServerSinkOptions
			//			{
			//			TableName = "LogEvents",
			//			AutoCreateSqlTable = true
			//			},
			//		columnOptions: sqlColumnOptions,
			//		restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning
			//	);
			//	}

			// Optional: Console JSON formatting (if not already via config)
			//cfg.WriteTo.Console(new JsonFormatter());

			// ... inside AddStructuredLogging method, replace the ApplicationInsights sink configuration with:

			//cfg.WriteTo.ApplicationInsights(
			//	telemetryConfiguration: new Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration
			//		{
			//		ConnectionString = ctx.Configuration["Azure:ApplicationInsights:ConnectionString"]
			//		},
			//	telemetryConverter: new TraceTelemetryConverter(),
			//	restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning
			//);

		},
		writeToProviders: true);
		}

	public static void UseStructuredRequestLogging (this WebApplication app)
		{
		app.Use(async (ctx, next) =>
		{
			var reqId = ctx.TraceIdentifier;
			ctx.Response.Headers["X-Request-ID"] = reqId;

			using (LogContext.PushProperty("requestId", reqId))
			using (LogContext.PushProperty("method", ctx.Request.Method))
			using (LogContext.PushProperty("path", ctx.Request.Path.ToString()))
			using (LogContext.PushProperty("queryString", ctx.Request.QueryString.Value))
				{
				await next();
				}
		});

		app.UseSerilogRequestLogging(opts =>
		{
			opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} => {StatusCode} in {Elapsed:0.0000} ms";
			opts.EnrichDiagnosticContext = (diag, http) =>
			{
				diag.Set("requestId", http.TraceIdentifier);
				diag.Set("clientIp", http.Connection.RemoteIpAddress?.ToString());
				if (http.User?.Identity?.IsAuthenticated == true)
					diag.Set("user", http.User.Identity!.Name);
			};
		});
		}
	}
