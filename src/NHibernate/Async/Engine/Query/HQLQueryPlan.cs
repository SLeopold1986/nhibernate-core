﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using System;
using System.Collections;
using System.Collections.Generic;

using NHibernate.Event;
using NHibernate.Hql;
using NHibernate.Linq;
using NHibernate.Type;
using NHibernate.Util;

namespace NHibernate.Engine.Query
{
    using System.Threading.Tasks;
    using System.Threading;
    public partial interface IQueryPlan
    {
        Task PerformListAsync(QueryParameters queryParameters, ISessionImplementor statelessSessionImpl, IList results, CancellationToken cancellationToken);
        Task<int> PerformExecuteUpdateAsync(QueryParameters queryParameters, ISessionImplementor statelessSessionImpl, CancellationToken cancellationToken);
        Task<IEnumerable<T>> PerformIterateAsync<T>(QueryParameters queryParameters, IEventSource session, CancellationToken cancellationToken);
        Task<IEnumerable> PerformIterateAsync(QueryParameters queryParameters, IEventSource session, CancellationToken cancellationToken);
    }
	public partial class HQLQueryPlan : IQueryPlan
	{

		public async Task PerformListAsync(QueryParameters queryParameters, ISessionImplementor session, IList results, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (Log.IsDebugEnabled())
			{
				Log.Debug("find: {0}", _sourceQuery);
				queryParameters.LogParameters(session.Factory);
			}

			bool hasLimit = queryParameters.RowSelection != null && queryParameters.RowSelection.DefinesLimits;
			bool needsLimit = hasLimit && Translators.Length > 1;
			QueryParameters queryParametersToUse;
			if (needsLimit)
			{
				Log.Warn("firstResult/maxResults specified on polymorphic query; applying in memory!");
				RowSelection selection = new RowSelection();
				selection.FetchSize = queryParameters.RowSelection.FetchSize;
				selection.Timeout = queryParameters.RowSelection.Timeout;
				selection.Hint = queryParameters.RowSelection.Hint;
				queryParametersToUse = queryParameters.CreateCopyUsing(selection);
			}
			else
			{
				queryParametersToUse = queryParameters;
			}

			IList combinedResults = results ?? new List<object>();
			var distinction = new HashSet<object>(ReferenceComparer<object>.Instance);
			int includedCount = -1;
			for (int i = 0; i < Translators.Length; i++)
			{
				IList tmp = await (Translators[i].ListAsync(session, queryParametersToUse, cancellationToken)).ConfigureAwait(false);
				if (needsLimit)
				{
					// NOTE : firstRow is zero-based
					int first = queryParameters.RowSelection.FirstRow == RowSelection.NoValue
												? 0
												: queryParameters.RowSelection.FirstRow;

					int max = queryParameters.RowSelection.MaxRows == RowSelection.NoValue
											? RowSelection.NoValue
											: queryParameters.RowSelection.MaxRows;

					int size = tmp.Count;
					for (int x = 0; x < size; x++)
					{
						object result = tmp[x];
						if (!distinction.Add(result))
						{
							continue;
						}
						includedCount++;
						if (includedCount < first)
						{
							continue;
						}
						combinedResults.Add(result);
						if (max >= 0 && includedCount > max)
						{
							// break the outer loop !!!
							return;
						}
					}
				}
				else
					ArrayHelper.AddAll(combinedResults, tmp);
			}
		}

		public async Task<IEnumerable> PerformIterateAsync(QueryParameters queryParameters, IEventSource session, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (Log.IsDebugEnabled())
			{
				Log.Debug("enumerable: {0}", _sourceQuery);
				queryParameters.LogParameters(session.Factory);
			}
			if (Translators.Length == 0)
			{
				return CollectionHelper.EmptyEnumerable;
			}
			if (Translators.Length == 1)
			{
				return await (Translators[0].GetEnumerableAsync(queryParameters, session, cancellationToken)).ConfigureAwait(false);
			}
			var results = new IEnumerable[Translators.Length];
			for (int i = 0; i < Translators.Length; i++)
			{
				var result = await (Translators[i].GetEnumerableAsync(queryParameters, session, cancellationToken)).ConfigureAwait(false);
				results[i] = result;
			}
			return new JoinedEnumerable(results);
		}

		public async Task<IEnumerable<T>> PerformIterateAsync<T>(QueryParameters queryParameters, IEventSource session, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return (await (PerformIterateAsync(queryParameters, session, cancellationToken)).ConfigureAwait(false)).CastOrDefault<T>();
		}

        public async Task<int> PerformExecuteUpdateAsync(QueryParameters queryParameters, ISessionImplementor session, CancellationToken cancellationToken)
        {
               cancellationToken.ThrowIfCancellationRequested();
            if (Log.IsDebugEnabled())
            {
                Log.Debug("executeUpdate: {0}", _sourceQuery);
                queryParameters.LogParameters(session.Factory);
            }
            if (Translators.Length != 1)
            {
                Log.Warn("manipulation query [{0}] resulted in [{1}] split queries", _sourceQuery, Translators.Length);
            }
            int result = 0;
            for (int i = 0; i < Translators.Length; i++)
            {
                result += await (Translators[i].ExecuteUpdateAsync(queryParameters, session, cancellationToken)).ConfigureAwait(false);
            }
            return result;
        }
    }
}
