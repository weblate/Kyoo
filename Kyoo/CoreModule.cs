using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Core;
using Autofac.Core.Registration;
using Autofac.Extras.AttributeMetadata;
using Kyoo.Abstractions;
using Kyoo.Abstractions.Controllers;
using Kyoo.Abstractions.Models.Permissions;
using Kyoo.Api;
using Kyoo.Controllers;
using Kyoo.Database;
using Kyoo.Models.Options;
using Kyoo.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using IMetadataProvider = Kyoo.Abstractions.Controllers.IMetadataProvider;

namespace Kyoo
{
	/// <summary>
	/// The core module containing default implementations
	/// </summary>
	public class CoreModule : IPlugin
	{
		/// <inheritdoc />
		public string Slug => "core";
		
		/// <inheritdoc />
		public string Name => "Core";
		
		/// <inheritdoc />
		public string Description => "The core module containing default implementations.";

		/// <inheritdoc />
		public Dictionary<string, Type> Configuration => new()
		{
			{ BasicOptions.Path, typeof(BasicOptions) },
			{ TaskOptions.Path, typeof(TaskOptions) },
			{ MediaOptions.Path, typeof(MediaOptions) },
			{ "database", null },
			{ "logging", null }
		};


		/// <summary>
		/// The configuration to use.
		/// </summary>
		private readonly IConfiguration _configuration;


		/// <summary>
		/// Create a new core module instance and use the given configuration.
		/// </summary>
		/// <param name="configuration">The configuration to use</param>
		public CoreModule(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		/// <inheritdoc />
		public void Configure(ContainerBuilder builder)
		{
			builder.RegisterModule<AttributedMetadataModule>();
			
			builder.RegisterComposite<FileSystemComposite, IFileSystem>().InstancePerLifetimeScope();
			builder.RegisterType<LocalFileSystem>().As<IFileSystem>().SingleInstance();
			builder.RegisterType<HttpFileSystem>().As<IFileSystem>().SingleInstance();
			
			builder.RegisterType<ConfigurationManager>().As<IConfigurationManager>().SingleInstance();
			builder.RegisterType<Transcoder>().As<ITranscoder>().SingleInstance();
			builder.RegisterType<ThumbnailsManager>().As<IThumbnailsManager>().InstancePerLifetimeScope();
			builder.RegisterType<TaskManager>().As<ITaskManager>().SingleInstance();
			builder.RegisterType<LibraryManager>().As<ILibraryManager>().InstancePerLifetimeScope();
			builder.RegisterType<RegexIdentifier>().As<IIdentifier>().SingleInstance();
			
			builder.RegisterComposite<ProviderComposite, IMetadataProvider>();
			builder.Register(x => (AProviderComposite)x.Resolve<IMetadataProvider>());

			builder.RegisterTask<Crawler>();
			builder.RegisterTask<Housekeeping>();
			builder.RegisterTask<RegisterEpisode>();
			builder.RegisterTask<RegisterSubtitle>();
			builder.RegisterTask<MetadataProviderLoader>();

			static bool DatabaseIsPresent(IComponentRegistryBuilder x)
				=> x.IsRegistered(new TypedService(typeof(DatabaseContext)));

			builder.RegisterRepository<ILibraryRepository, LibraryRepository>().OnlyIf(DatabaseIsPresent);
			builder.RegisterRepository<ILibraryItemRepository, LibraryItemRepository>().OnlyIf(DatabaseIsPresent);
			builder.RegisterRepository<ICollectionRepository, CollectionRepository>().OnlyIf(DatabaseIsPresent);
			builder.RegisterRepository<IShowRepository, ShowRepository>().OnlyIf(DatabaseIsPresent);
			builder.RegisterRepository<ISeasonRepository, SeasonRepository>().OnlyIf(DatabaseIsPresent);
			builder.RegisterRepository<IEpisodeRepository, EpisodeRepository>().OnlyIf(DatabaseIsPresent);
			builder.RegisterRepository<ITrackRepository, TrackRepository>().OnlyIf(DatabaseIsPresent);
			builder.RegisterRepository<IPeopleRepository, PeopleRepository>().OnlyIf(DatabaseIsPresent);
			builder.RegisterRepository<IStudioRepository, StudioRepository>().OnlyIf(DatabaseIsPresent);
			builder.RegisterRepository<IGenreRepository, GenreRepository>().OnlyIf(DatabaseIsPresent);
			builder.RegisterRepository<IProviderRepository, ProviderRepository>().OnlyIf(DatabaseIsPresent);
			builder.RegisterRepository<IUserRepository, UserRepository>().OnlyIf(DatabaseIsPresent);

			builder.RegisterType<PassthroughPermissionValidator>().As<IPermissionValidator>()
				.IfNotRegistered(typeof(IPermissionValidator));
			
			builder.RegisterType<FileExtensionContentTypeProvider>().As<IContentTypeProvider>().SingleInstance()
				.OnActivating(x =>
				{
					x.Instance.Mappings[".data"] = "application/octet-stream";
					x.Instance.Mappings[".mkv"] = "video/x-matroska";
					x.Instance.Mappings[".ass"] = "text/x-ssa";
					x.Instance.Mappings[".srt"] = "application/x-subrip";
					x.Instance.Mappings[".m3u8"] = "application/x-mpegurl";
				});
		}
		
		/// <inheritdoc />
		public void Configure(IServiceCollection services)
		{
			string publicUrl = _configuration.GetPublicUrl();

			services.AddMvc().AddControllersAsServices();
			services.AddControllers()
				.AddNewtonsoftJson(x =>
				{
					x.SerializerSettings.ContractResolver = new JsonPropertyIgnorer(publicUrl);
					x.SerializerSettings.Converters.Add(new PeopleRoleConverter());
				});
			
			services.AddResponseCompression(x =>
			{
				x.EnableForHttps = true;
			});
			
			services.AddHttpClient();
			
			services.AddHostedService(x => x.GetService<ITaskManager>() as TaskManager);
		}

		/// <inheritdoc />
		public IEnumerable<IStartupAction> ConfigureSteps => new IStartupAction[]
		{
			SA.New<IApplicationBuilder, IHostEnvironment>((app, env) =>
			{
				if (env.IsDevelopment())
					app.UseDeveloperExceptionPage();
				else
				{
					app.UseExceptionHandler("/error");
					app.UseHsts();
				}
			}, SA.Before),
			SA.New<IApplicationBuilder>(app => app.UseResponseCompression(), SA.Routing + 1),
			SA.New<IApplicationBuilder>(app => app.UseRouting(), SA.Routing),
			SA.New<IApplicationBuilder>(app => app.UseEndpoints(x => x.MapControllers()), SA.Endpoint)
		};
	}
}