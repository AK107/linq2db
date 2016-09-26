﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	using Extensions;

	abstract class ExpressionQueryNew<T> : IExpressionQuery<T>
	{
		protected ExpressionQueryNew(IDataContextEx dataContext, Expression expression)
		{
			_dataContext = dataContext;

			Expression = expression ?? Expression.Constant(this);
		}

		readonly IDataContextEx _dataContext;

		public Expression     Expression  { get; set; }
		public Type           ElementType => typeof(T);
		public IQueryProvider Provider    => this;

		#region SqlText

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		string _sqlTextHolder;

		// ReSharper disable once InconsistentNaming
		// ReSharper disable once UnusedMember.Local
		string _sqlText => SqlText;

		public  string  SqlText
		{
			get
			{
				var hasQueryHints = _dataContext.QueryHints.Count > 0 || _dataContext.NextQueryHints.Count > 0;

				if (_sqlTextHolder == null || hasQueryHints)
				{
//					var info    = GetQuery(Expression, true);
//					var sqlText = info.GetSqlText(_dataContext, Expression, null/*Parameters*/, 0);
//
//					if (hasQueryHints)
//						return sqlText;
//
//					_sqlTextHolder = sqlText;
				}

				return _sqlTextHolder;
			}
		}

		public IDataContextInfo DataContextInfo { get; set; }

		#endregion

		public IQueryable CreateQuery(Expression expression)
		{
			if (expression == null)
				throw new ArgumentNullException(nameof(expression));

			var elementType = expression.Type.GetItemType() ?? expression.Type;

			try
			{
				return (IQueryable)Activator
					.CreateInstance(typeof(ExpressionQueryImplNew<>).MakeGenericType(elementType), _dataContext, expression);
			}
			catch (TargetInvocationException ex)
			{
				throw ex.InnerException;
			}
		}

		public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
		{
			if (expression == null)
				throw new ArgumentNullException(nameof(expression));

			return new ExpressionQueryImplNew<TElement>(_dataContext, expression);
		}

		public IEnumerator<T> GetEnumerator()
		{
			return GetQuery(Expression, true).GetIEnumerable(_dataContext, Expression).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetQuery(Expression, true).GetIEnumerable(_dataContext, Expression).GetEnumerator();
		}

		public object Execute(Expression expression)
		{
			return GetQuery(expression, false).GetElement(_dataContext, expression);
		}

		public Task GetForEachAsync(Action<T> action, CancellationToken cancellationToken)
		{
			return GetQuery(Expression, true).GetForEachAsync(_dataContext, Expression, action, cancellationToken);
		}

		public TResult Execute<TResult>(Expression expression)
		{
#if DEBUG
			if (typeof(TResult) != typeof(T))
				throw new InvalidOperationException();
#endif

			return (TResult)(object)GetQuery(expression, false).GetElement(_dataContext, expression);
		}

		QueryNew<T> _info;

		QueryNew<T> GetQuery(Expression expression, bool isEnumerable)
		{
			if (isEnumerable && _info != null)
				throw new InvalidOperationException();
				//return _info;

			var info = QueryNew<T>.GetQuery(_dataContext, expression, isEnumerable);

			if (isEnumerable)
				_info = info;

			return info;
		}
	}
}
