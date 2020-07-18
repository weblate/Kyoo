using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Kyoo.CommonApi;
using Kyoo.Models;
using Kyoo.Models.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Kyoo.Controllers
{
	public abstract class LocalRepository<T> : IRepository<T> where T : class, IRessource
	{
		private readonly DbContext _database;

		protected abstract Expression<Func<T, object>> DefaultSort { get; }


		protected LocalRepository(DbContext database)
		{
			_database = database;
		}
		
		public virtual void Dispose()
		{
			_database.Dispose();
		}

		public virtual ValueTask DisposeAsync()
		{
			return _database.DisposeAsync();
		}

		public virtual Task<T> Get(int id)
		{
			return _database.Set<T>().FirstOrDefaultAsync(x => x.ID == id);
		}

		public virtual Task<T> Get(string slug)
		{
			return _database.Set<T>().FirstOrDefaultAsync(x => x.Slug == slug);
		}

		public abstract Task<ICollection<T>> Search(string query);

		public virtual async Task<ICollection<T>> GetAll(Expression<Func<T, bool>> where = null,
			Sort<T> sort = default, 
			Pagination limit = default)
		{
			IQueryable<T> query = _database.Set<T>();

			if (where != null)
				query = query.Where(where);

			Expression<Func<T, object>> sortKey = sort.Key ?? DefaultSort;
			query = sort.Descendant ? query.OrderByDescending(sortKey) : query.OrderBy(sortKey);

			if (limit.AfterID != 0)
			{
				T after = await Get(limit.AfterID);
				object afterObj = sortKey.Compile()(after);
				query = query.Where(Expression.Lambda<Func<T, bool>>(
					ApiHelper.StringCompatibleExpression(Expression.GreaterThan, sortKey.Body, Expression.Constant(afterObj)),
					(ParameterExpression)((MemberExpression)sortKey.Body).Expression
				));
			}
			if (limit.Count > 0)
				query = query.Take(limit.Count);

			return await query.ToListAsync();
		}

		public abstract Task<T> Create(T obj);

		public virtual async Task<T> CreateIfNotExists(T obj)
		{
			if (obj == null)
				throw new ArgumentNullException(nameof(obj));

			T old = await Get(obj.Slug);
			if (old != null)
				return old;
			try
			{
				return await Create(obj);
			}
			catch (DuplicatedItemException)
			{
				old = await Get(obj.Slug);
				if (old == null)
					throw new SystemException("Unknown database state.");
				return old;
			}
		}

		public virtual async Task<T> Edit(T edited, bool resetOld)
		{
			if (edited == null)
				throw new ArgumentNullException(nameof(edited));
			
			T old = await Get(edited.Slug);

			if (old == null)
				throw new ItemNotFound($"No ressource found with the slug {edited.Slug}.");
			
			if (resetOld)
				Utility.Nullify(old);
			Utility.Merge(old, edited);
			await Validate(old);
			await _database.SaveChangesAsync();
			return old;
		}

		protected abstract Task Validate(T ressource);

		public virtual async Task Delete(int id)
		{
			T ressource = await Get(id);
			await Delete(ressource);
		}

		public virtual async Task Delete(string slug)
		{
			T ressource = await Get(slug);
			await Delete(ressource);
		}

		public abstract Task Delete(T obj);

		public virtual async Task DeleteRange(IEnumerable<T> objs)
		{
			foreach (T obj in objs)
				await Delete(obj);
		}
		
		public virtual async Task DeleteRange(IEnumerable<int> ids)
		{
			foreach (int id in ids)
				await Delete(id);
		}
		
		public virtual async Task DeleteRange(IEnumerable<string> slugs)
		{
			foreach (string slug in slugs)
				await Delete(slug);
		}
		
		public static bool IsDuplicateException(DbUpdateException ex)
		{
			return ex.InnerException is PostgresException inner
			       && inner.SqlState == PostgresErrorCodes.UniqueViolation;
		}
	}
}