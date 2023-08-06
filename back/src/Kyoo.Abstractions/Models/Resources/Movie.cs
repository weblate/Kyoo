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

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Kyoo.Abstractions.Controllers;
using Kyoo.Abstractions.Models.Attributes;
using Kyoo.Utils;
using Newtonsoft.Json;

namespace Kyoo.Abstractions.Models
{
	/// <summary>
	/// A series or a movie.
	/// </summary>
	public class Movie : IResource, IMetadata, IOnMerge, IThumbnails
	{
		/// <inheritdoc />
		public int Id { get; set; }

		/// <inheritdoc />
		[MaxLength(256)]
		public string Slug { get; set; }

		/// <summary>
		/// The title of this show.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// A catchphrase for this movie.
		/// </summary>
		public string? Tagline { get; set; }

		/// <summary>
		/// The list of alternative titles of this show.
		/// </summary>
		public string[] Aliases { get; set; } = Array.Empty<string>();

		/// <summary>
		/// The path of the movie video file.
		/// </summary>
		public string Path { get; set; }

		/// <summary>
		/// The summary of this show.
		/// </summary>
		public string? Overview { get; set; }

		/// <summary>
		/// A list of tags that match this movie.
		/// </summary>
		public string[] Tags { get; set; } = Array.Empty<string>();

		/// <summary>
		/// The list of genres (themes) this show has.
		/// </summary>
		public Genre[] Genres { get; set; } = Array.Empty<Genre>();

		/// <summary>
		/// Is this show airing, not aired yet or finished?
		/// </summary>
		public Status Status { get; set; }

		/// <summary>
		/// The date this movie aired.
		/// </summary>
		public DateTime? AirDate { get; set; }

		/// <inheritdoc />
		public Image? Poster { get; set; }

		/// <inheritdoc />
		public Image? Thumbnail { get; set; }

		/// <inheritdoc />
		public Image? Logo { get; set; }

		/// <summary>
		/// A video of a few minutes that tease the content.
		/// </summary>
		public string? Trailer { get; set; }

		/// <inheritdoc />
		public Dictionary<string, MetadataId> ExternalId { get; set; } = new();

		/// <summary>
		/// The ID of the Studio that made this show.
		/// </summary>
		[SerializeIgnore] public int? StudioID { get; set; }

		/// <summary>
		/// The Studio that made this show.
		/// This must be explicitly loaded via a call to <see cref="ILibraryManager.Load"/>.
		/// </summary>
		[LoadableRelation(nameof(StudioID))][EditableRelation] public Studio? Studio { get; set; }

		/// <summary>
		/// The list of people that made this show.
		/// </summary>
		[LoadableRelation][EditableRelation] public ICollection<PeopleRole>? People { get; set; }

		/// <summary>
		/// The list of collections that contains this show.
		/// </summary>
		[LoadableRelation] public ICollection<Collection>? Collections { get; set; }

		/// <inheritdoc />
		public void OnMerge(object merged)
		{
			if (People != null)
			{
				foreach (PeopleRole link in People)
					link.Movie = this;
			}
		}

		public Movie() { }

		[JsonConstructor]
		public Movie(string name)
		{
			Slug = Utility.ToSlug(name);
			Name = name;
		}
	}
}
