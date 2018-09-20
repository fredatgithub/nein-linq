﻿using System;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace NeinLinq
{
    /// <summary>
    /// Proxy for query provider.
    /// </summary>
    public class RewriteDbQueryProvider : RewriteQueryProvider, IDbAsyncQueryProvider
    {
        /// <summary>
        /// Create a new rewrite query provider.
        /// </summary>
        /// <param name="provider">The actual query provider.</param>
        /// <param name="rewriter">The rewriter to rewrite the query.</param>
        public RewriteDbQueryProvider(IQueryProvider provider, ExpressionVisitor rewriter)
            : base(provider, rewriter)
        {
        }

        /// <inheritdoc />
        public override IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            // create query and make proxy again for rewritten query chaining
            var queryable = Provider.CreateQuery<TElement>(expression);
            return new RewriteDbQueryable<TElement>(queryable, this);
        }

        /// <inheritdoc />
        public override IQueryable CreateQuery(Expression expression)
        {
            // create query and make proxy again for rewritten query chaining
            var queryable = Provider.CreateQuery(expression);
            return (IQueryable)Activator.CreateInstance(
                typeof(RewriteDbQueryable<>).MakeGenericType(queryable.ElementType),
                queryable, this);
        }

        /// <inheritdoc />
        public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            // execute query with rewritten expression; async, if possible
            if (Provider is IDbAsyncQueryProvider asyncProvider)
                return asyncProvider.ExecuteAsync<TResult>(Rewriter.Visit(expression), cancellationToken);
            return Task.FromResult(Provider.Execute<TResult>(Rewriter.Visit(expression)));
        }

        /// <inheritdoc />
        public Task<object> ExecuteAsync(Expression expression, CancellationToken cancellationToken)
        {
            // execute query with rewritten expression; async, if possible
            if (Provider is IDbAsyncQueryProvider asyncProvider)
                return asyncProvider.ExecuteAsync(Rewriter.Visit(expression), cancellationToken);
            return Task.FromResult(Provider.Execute(Rewriter.Visit(expression)));
        }
    }
}
