using Meta.Common.DbHelper;
using Meta.Common.Interface;
using Meta.Common.Model;
using Meta.Common.SqlBuilder.AnalysisExpression;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
namespace Meta.Common.SqlBuilder
{
    public abstract class UpdateBuilder<TSQL, TModel> : WhereBase<TSQL, TModel> where TSQL : class, new() where TModel : IDbModel, new()
    {
        /// <summary>
        /// 设置列表
        /// </summary>
        readonly List<string> _setList = new List<string>();

        static readonly Dictionary<NpgsqlDbType, string> _incrementDic = new Dictionary<NpgsqlDbType, string>
        {
            { NpgsqlDbType.Date, "now()::date" }, { NpgsqlDbType.Interval, "'00:00:00'" }, { NpgsqlDbType.Time, "'00:00:00'" },
            { NpgsqlDbType.Timestamp, "now()" }, { NpgsqlDbType.Money, "0::money" }
        };
        /// <summary>
        /// 是否返回实体类
        /// </summary>
        bool _isReturn = false;
        TSQL This => this as TSQL;
        public int Count => _setList.Count;
        #region Contructor
        public UpdateBuilder(string table) : base(table) { }
        public UpdateBuilder(string table, string alias) : base(table, alias) { }
        public UpdateBuilder() : base(EntityHelper.GetTableName<TModel>()) { }

        #endregion

        /// <summary>
        /// 字段自增
        /// </summary>
        /// <param name="field">字段名称</param>
        /// <param name="value">自增值</param>
        /// <param name="size"></param>
        /// <returns></returns>
        private TSQL SetIncrement(string field, object value, int? size = null, NpgsqlDbType? dbType = null)
        {
            var param_name = EntityHelper.ParamsIndex;
            var coalesce = dbType.HasValue && _incrementDic.ContainsKey(dbType.Value) ? _incrementDic[dbType.Value] : "0";
            return SetExp(string.Format("{0} = COALESCE({0}, {1}) + @{2}", field, coalesce, param_name), param_name, value, size, value is TimeSpan ? NpgsqlDbType.Interval : dbType);
        }

        /// <summary>
        /// 添加元素到数组
        /// </summary>
        /// <param name="field">字段名称</param>
        /// <param name="value">值或数组</param>
        /// <param name="size"></param>
        /// <returns></returns>
        private TSQL SetJoin(string field, object value, int? size = null, NpgsqlDbType? dbType = null)
        {
            var param_name = EntityHelper.ParamsIndex;
            return SetExp(string.Format("{0} = {0} || @{1}", field, param_name), param_name, value, size, dbType);
        }

        /// <summary>
        /// geometry字段
        /// </summary>
        /// <param name="field">字段名称</param>
        /// <param name="x">经度</param>
        /// <param name="y">纬度</param>
        /// <param name="srid">空间坐标系唯一标识</param>
        /// <returns></returns>
        protected TSQL SetGeometry(string field, float x, float y, int srid)
        {
            var pointName = EntityHelper.ParamsIndex;
            var sridName = EntityHelper.ParamsIndex;
            AddParameter(pointName, $"POINT({x} {y})", -1);
            AddParameter(sridName, srid, -1);
            _setList.Add($"{field} = ST_GeomFromText(@{pointName}, @{sridName})");
            return This;
        }
        /// <summary>
        /// 从数组移除元素
        /// </summary>
        /// <param name="field">字段名称</param>
        /// <param name="value">需要移除的值</param>
        /// <param name="size"></param>
        /// <returns></returns>
        private TSQL SetRemove(string field, object value, int? size = null, NpgsqlDbType? dbType = null)
        {
            var param_name = EntityHelper.ParamsIndex;
            return SetExp(string.Format("{0} = array_remove({0}, @{1})", field, param_name), param_name, value, size, dbType);
        }

        /// <summary>
        /// 设置字段
        /// </summary>
        /// <param name="exp">带@param的表达式</param>
        /// <param name="param">param名称</param>
        /// <param name="value">值</param>
        /// <param name="size"></param>
        /// <returns></returns>
        private TSQL SetExp(string exp, string param, object value, int? size = null, NpgsqlDbType? dbType = null)
        {
            AddParameter(param, value, size, dbType);
            _setList.Add(exp);
            return This;
        }

        /// <summary>
        /// 是否添加set语句
        /// </summary>
        /// <param name="isAdd"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <param name="size"></param>
        /// <param name="dbType"></param>
        /// <returns></returns>
        private TSQL Set(bool isAdd, string field, object value, int? size = null, NpgsqlDbType? dbType = null)
        {
            if (!isAdd) return This;
            var param_name = EntityHelper.ParamsIndex;
            return SetExp($"{field} = @{param_name}", param_name, value, size, dbType);
        }

        /// <summary>
        /// 设置字段等于value(同一个update语句不能调用置两次)
        /// </summary>
        /// <param name="field">字段名称</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        private TSQL Set(string field, object value, int? size = null, NpgsqlDbType? dbType = null)
        {
            var param_name = EntityHelper.ParamsIndex;
            return SetExp($"{field} = @{param_name}", param_name, value, size, dbType);
        }

        /// <summary>
        /// 设置字段等于SQL
        /// </summary>
        /// <param name="selector">key selector</param>
        /// <param name="sqlBuilder">SQL语句</param>
        /// <returns></returns>
        public TSQL SetBuilder(Expression<Func<TModel, object>> selector, ISqlBuilder sqlBuilder)
        {
            var exp = string.Concat(SqlExpressionVisitor.Instance.VisitSingleForNoAlias(selector).SqlText, " = ", $"({sqlBuilder.GetCommandTextString()})");
            _setList.Add(exp);
            return AddParameter(sqlBuilder.Params);
        }

        /// <summary>
        /// 设置一个字段值(非空类型) *可选重载
        /// </summary>
        /// <typeparam name="TKey">字段类型</typeparam>
        /// <param name="isSet">是否设置</param>
        /// <param name="selector">字段key selector</param>
        /// <param name="value">value</param>
        /// <returns></returns>
        public TSQL Set<TKey>(bool isSet, Expression<Func<TModel, TKey>> selector, TKey value) => isSet ? Set(selector, value) : This;

        /// <summary>
        /// 设置一个字段值(可空类型) *可选重载
        /// </summary>
        /// <typeparam name="TKey">字段类型</typeparam>
        /// <param name="isSet">是否设置</param>
        /// <param name="selector">字段key selector</param>
        /// <param name="value">value</param>
        /// <returns></returns>
        public TSQL Set<TKey>(bool isSet, Expression<Func<TModel, TKey?>> selector, TKey? value) where TKey : struct => isSet ? Set(selector, value) : This;

        /// <summary>
        /// 设置一个字段值(非空类型)
        /// </summary>
        /// <typeparam name="TKey">字段类型</typeparam>
        /// <param name="selector">字段key selector</param>
        /// <param name="value">value</param>
        /// <returns></returns>
        public TSQL Set<TKey>(Expression<Func<TModel, TKey>> selector, TKey value)
        {
            var field = SqlExpressionVisitor.Instance.VisitSingleForNoAlias(selector).SqlText;
            if (value == null)
            {
                _setList.Add(string.Format("{0} = null", field));
                return This;
            }
            var valueIndex = EntityHelper.ParamsIndex;
            _setList.Add(string.Format("{0} = @{1}", field, valueIndex));
            return AddParameter(new NpgsqlParameter<TKey>(valueIndex, value));
        }
        /// <summary>
        /// 设置一个字段值(可空类型)
        /// </summary>
        /// <typeparam name="TKey">字段类型</typeparam>
        /// <param name="selector">字段key selector</param>
        /// <param name="value">value</param>
        /// <returns></returns>
        public TSQL Set<TKey>(Expression<Func<TModel, TKey?>> selector, TKey? value) where TKey : struct
        {

            var field = SqlExpressionVisitor.Instance.VisitSingleForNoAlias(selector).SqlText;
            if (value == null)
            {
                _setList.Add(string.Format("{0} = null", field));
                return This;
            }
            var valueIndex = EntityHelper.ParamsIndex;
            _setList.Add(string.Format("{0} = @{1}", field, valueIndex));
            return AddParameter(new NpgsqlParameter<TKey>(valueIndex, value.Value));
        }
        /// <summary>
        /// 数组连接一个数组
        /// </summary>
        /// <typeparam name="TKey">数组类型</typeparam>
        /// <param name="selector">key selector</param>
        /// <param name="value">数组</param>
        /// <returns></returns>
        public TSQL SetAppend<TKey>(Expression<Func<TModel, IEnumerable<TKey>>> selector, params TKey[] value)
        {
            return SetJoin(SqlExpressionVisitor.Instance.VisitSingleForNoAlias(selector).SqlText, value);
        }
        /// <summary>
        /// 数组移除某元素
        /// </summary>
        /// <typeparam name="TKey">数组的类型</typeparam>
        /// <param name="selector">key selector</param>
        /// <param name="value">元素</param>
        /// <returns></returns>
        public TSQL SetRemove<TKey>(Expression<Func<TModel, IEnumerable<TKey>>> selector, TKey value)
        {
            return SetRemove(SqlExpressionVisitor.Instance.VisitSingleForNoAlias(selector).SqlText, value);
        }

        /// <summary>
        /// 自增, 可空类型留默认值
        /// </summary>
        /// <typeparam name="TKey">COALESCE默认值类型</typeparam>
        /// <typeparam name="TTarget">增加值的类型</typeparam>
        /// <param name="selector">key selector</param>
        /// <param name="value">增量</param>
        /// <param name="defaultValue">COALESCE默认值, 如果null, 则取default(TKey)</param>
        /// <exception cref="ArgumentNullException">增量为空</exception>
        /// <returns></returns>
        public TSQL SetIncrement<TKey, TTarget>(Expression<Func<TModel, TKey?>> selector, TTarget value, TKey? defaultValue) where TKey : struct
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            var valueIndex = EntityHelper.ParamsIndex;
            var defaultValueIndex = EntityHelper.ParamsIndex;
            var field = SqlExpressionVisitor.Instance.VisitSingleForNoAlias(selector).SqlText;

            _setList.Add(string.Format("{0} = COALESCE({0}, @{1}) + @{2}", field, defaultValueIndex, valueIndex));
            AddParameter(new NpgsqlParameter<TTarget>(valueIndex, value));
            if (defaultValue == null)
                return AddParameter(new NpgsqlParameter<TKey>(defaultValueIndex, value: default));
            return AddParameter(new NpgsqlParameter<TKey>(defaultValueIndex, defaultValue.Value));
        }
        /// <summary>
        /// 自增, 不可空类型不留默认值
        /// </summary>
        /// <typeparam name="TTarget">增加值的类型</typeparam>
        /// <param name="selector">key selector</param>
        /// <param name="value">增量</param>
        /// <exception cref="ArgumentNullException">增量为空</exception>
        /// <returns></returns>

        public TSQL SetIncrement<TKey, TTarget>(Expression<Func<TModel, TKey>> selector, TTarget value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            var valueIndex = EntityHelper.ParamsIndex;
            var field = SqlExpressionVisitor.Instance.VisitSingleForNoAlias(selector).SqlText;
            _setList.Add(string.Format("{0} = {0} + @{1}", field, valueIndex));
            return AddParameter(new NpgsqlParameter<TTarget>(valueIndex, value));
        }

        /// <summary>
        /// 返回修改行数
        /// </summary>
        /// <returns></returns>
        public new int ToRows() => base.ToRows();

        /// <summary>
        /// 返回修改行数, 并且ref实体类(一行)
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <returns></returns>
        public int ToRows(ref TModel refInfo)
        {
            _isReturn = true;
            var info = base.ToOne<TModel>();
            if (info == null) return 0;
            refInfo = info;
            return 1;
        }

        /// <summary>
        /// 返回修改行数, 并且ref列表(多行)
        /// </summary>
        /// <typeparam name="TModel></typeparam>
        /// <param name="refInfo"></param>
        /// <returns></returns>
        public int ToRows(ref List<TModel> refInfo)
        {
            _isReturn = true;
            var info = base.ToList<TModel>();
            refInfo = info;
            return info.Count;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public TSQL ToRowsPipe() => base.ToPipe<int>(PipeReturnType.Rows);

        #region Override
        public override string ToString() => base.ToString();

        public override string GetCommandTextString()
        {
            if (WhereList.Count < 1)
                throw new ArgumentNullException(nameof(WhereList));
            if (_setList.Count == 0)
                throw new ArgumentNullException(nameof(_setList));
            var ret = string.Empty;
            if (_isReturn)
            {
                Fields = EntityHelper.GetModelTypeFieldsString<TModel>(MainAlias);
                ret = $"RETURNING {Fields}";
            }
            return $"UPDATE {MainTable} {MainAlias} SET {string.Join(",", _setList)} WHERE {string.Join("\nAND", WhereList)} {ret}";
        }
        #endregion
    }
}
