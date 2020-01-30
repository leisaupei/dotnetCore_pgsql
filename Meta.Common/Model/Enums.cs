using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Meta.Common.Model
{
	public enum UnionEnum
	{
		INNER_JOIN = 1, LEFT_JOIN, RIGHT_JOIN, LEFT_OUTER_JOIN, RIGHT_OUTER_JOIN
	}
	public enum PipeReturnType
	{
		One = 1, List, Rows
	}
	public enum ExpressionExcutionType
	{
		None = 0, Single, Union, Condition, SingleForNoAlias
	}

	public enum DatabaseType
	{
		Postgres = 1,
		[EditorBrowsable(EditorBrowsableState.Never), Obsolete("Future")]
		Mysql = 2,
	}
	public enum TableStrategyType
	{
		/// <summary>
		/// 当值相等
		/// </summary>
		WhenValueEqual = 1,
		/// <summary>
		/// 当值比较
		/// </summary>
		WhenValueCompare = 2
	}
	public enum TableStrategyOptions
	{
		EveryEnum = 1,
		SomeEnum = 2,
		EveryYear = 3,
		EveryMonth = 4,
		DatetimeInterval,
	}
}
