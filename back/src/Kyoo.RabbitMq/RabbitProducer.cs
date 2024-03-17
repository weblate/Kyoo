// Kyoo - A portable and vast media library solution.
// Copyright (c) Kyoo.
//
// See AUTHORS.md and LICENSE file in the project root for full license information.
//
// Kyoo is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// any later version.
//
// Kyoo is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Kyoo. If not, see <https://www.gnu.org/licenses/>.

using Kyoo.Abstractions.Controllers;
using Kyoo.Abstractions.Models;
using RabbitMQ.Client;

namespace Kyoo.RabbitMq;

public class RabbitProducer
{
	private readonly IModel _channel;

	public RabbitProducer(IConnection rabbitConnection)
	{
		_channel = rabbitConnection.CreateModel();

		_channel.ExchangeDeclare("events.resource", ExchangeType.Topic);
		_ListenResourceEvents<Collection>("events.resource");
		_ListenResourceEvents<Movie>("events.resource");
		_ListenResourceEvents<Show>("events.resource");
		_ListenResourceEvents<Season>("events.resource");
		_ListenResourceEvents<Episode>("events.resource");
		_ListenResourceEvents<Studio>("events.resource");
		_ListenResourceEvents<User>("events.resource");

		_channel.ExchangeDeclare("events.watched", ExchangeType.Topic);
		IWatchStatusRepository.OnMovieStatusChangedHandler += _PublishWatchStatus<MovieWatchStatus>(
			"movie"
		);
		IWatchStatusRepository.OnShowStatusChangedHandler += _PublishWatchStatus<ShowWatchStatus>(
			"show"
		);
		IWatchStatusRepository.OnEpisodeStatusChangedHandler +=
			_PublishWatchStatus<EpisodeWatchStatus>("episode");
	}

	private void _ListenResourceEvents<T>(string exchange)
		where T : IResource, IQuery
	{
		string type = typeof(T).Name.ToLowerInvariant();

		IRepository<T>.OnCreated += _Publish<T>(exchange, type, "created");
		IRepository<T>.OnEdited += _Publish<T>(exchange, type, "edited");
		IRepository<T>.OnDeleted += _Publish<T>(exchange, type, "deleted");
	}

	private IRepository<T>.ResourceEventHandler _Publish<T>(
		string exchange,
		string type,
		string action
	)
		where T : IResource, IQuery
	{
		return (T resource) =>
		{
			Message message =
				new()
				{
					Action = action,
					Type = type,
					Value = resource,
				};
			_channel.BasicPublish(
				exchange,
				routingKey: message.AsRoutingKey(),
				body: message.AsBytes()
			);
			return Task.CompletedTask;
		};
	}

	private IWatchStatusRepository.ResourceEventHandler<T> _PublishWatchStatus<T>(string resource)
		where T : IWatchStatus
	{
		return (status) =>
		{
			Message message =
				new()
				{
					Type = resource,
					Action = status.Status.ToString().ToLowerInvariant(),
					Value = status,
				};
			_channel.BasicPublish(
				exchange: "events.watched",
				routingKey: message.AsRoutingKey(),
				body: message.AsBytes()
			);
			return Task.CompletedTask;
		};
	}
}
