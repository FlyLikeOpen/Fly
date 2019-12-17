using System;
using System.Configuration;
using System.Data;
using System.Text;
using System.Data.SqlClient;
using System.Data.Common;
using System.Collections.Generic;
using System.Transactions;
using System.Text.RegularExpressions;
using System.Linq;
using System.Reflection;
using Fly.Framework.Common;
using System.Linq.Expressions;

namespace Fly.Framework.SqlDbAccess
{
    /// <summary>
    /// The SqlHelper class is intended to encapsulate high performance, 
    /// scalable best practices for common uses of SqlClient.
    /// </summary>
    public static partial class SqlHelper
    {
        #region Private Member

        private const int TIME_OUT = 300;

        private static string ProcessDbName(this string sql)
        {
            return sql;
        }

        private class ConnectionWrapper : IDisposable
        {
            private DbConnection m_Connection;
            private bool m_DisposeConnection;

            /// <summary>
            ///		Create a new "lifetime" container for a <see cref="DbConnection"/> instance.
            /// </summary>
            /// <param name="connection">The connection</param>
            /// <param name="disposeConnection">
            ///		Whether or not to dispose of the connection when this class is disposed.
            ///	</param>
            public ConnectionWrapper(DbConnection connection, bool disposeConnection)
            {
                this.m_Connection = connection;
                this.m_DisposeConnection = disposeConnection;
            }

            /// <summary>
            ///		Gets the actual connection.
            /// </summary>
            public DbConnection Connection
            {
                get { return m_Connection; }
            }

            #region IDisposable Members

            /// <summary>
            ///		Dispose the wrapped connection, if appropriate.
            /// </summary>
            public void Dispose()
            {
                if (m_DisposeConnection)
                {
                    try
                    {
                        m_Connection.Close();
                        m_Connection.Dispose();
                    }
                    catch { }
                }
            }

            #endregion
        }

        private static IDbFactory GetDbFactory(string connStrKey)
        {
            return DbFactories.GetFactory(ConnectionStringManager.GetProviderType(connStrKey));
        }

        private static ConnectionWrapper GetOpenConnection(string connString, IDbFactory factory)
        {
            return GetOpenConnection(connString, factory, true);
        }

        private static ConnectionWrapper GetOpenConnection(string connString, IDbFactory factory, bool disposeInnerConnection)
        {
            DbConnection connection = TransactionScopeConnections.GetConnection(connString, factory);
            if (connection != null)
            {
                return new ConnectionWrapper(connection, false);
            }
            else
            {
                try
                {
                    connection = factory.CreateConnection(connString);
                    connection.Open();
                }
                catch
                {
                    if (connection != null)
                    {
                        connection.Close();
                    }
                    throw;
                }
                return new ConnectionWrapper(connection, disposeInnerConnection);
            }
        }

        private static void PrepareCommand(DbCommand cmd, DbConnection conn, DbTransaction trans, CommandType cmdType, string cmdText, DbParameter[] cmdParms)
        {
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }

            cmd.Connection = conn;
            cmd.CommandText = cmdText;
            cmd.CommandTimeout = TIME_OUT;

            if (trans != null)
            {
                cmd.Transaction = trans;
            }

            cmd.CommandType = cmdType;

            if (cmdParms != null)
            {
                foreach (DbParameter parm in cmdParms)
                {
                    cmd.Parameters.Add(parm);
                }
            }
        }

        #endregion

        public static string GetConnStrKey(DbInstance instance)
        {
            if (instance == DbInstance.CanWrite)
            {
                return ConnectionStringManager.DEFAULT_CONN_KEY;
            }
            return ConnectionStringManager.DEFAULT_CONN_KEY;
        }

        public static string GetConnectionString(string connStrKey)
        {
            return ConnectionStringManager.GetConnectionString(connStrKey);
        }

        public static string BuildDbValue(string t)
        {
            if (t == null)
            {
                return "NULL";
            }
            return "'" + t.ToSafeSql() + "'";
        }

        public static string BuildDbValue<T>(T? t) where T : struct
        {
            if (t == null)
            {
                return "NULL";
            }
            if (typeof(T) == typeof(Guid) || typeof(T) == typeof(char))
            {
                return "'" + t.ToString().ToSafeSql() + "'";
            }
            if (typeof(T) == typeof(bool))
            {
                return (bool)(object)(t.Value) ? "1" : "0";
            }
            if (typeof(T) == typeof(DateTime))
            {
                return "'" + ((DateTime)((object)t.Value)).ToString("yyyy-MM-dd HH:mm:ss") + "'";
            }
            if (typeof(T).IsEnum)
            {
                return ((int)(object)t).ToString();
            }
            return t.ToString().ToSafeSql();
        }

        public static string ToUnionAllSelectSql(this IEnumerable<string> valueList, string columnName = null)
        {
            if (valueList == null || valueList.Count() <= 0)
            {
                return null;
            }
            StringBuilder sql = new StringBuilder();
            foreach (var v in valueList)
            {
                if (sql.Length > 0)
                {
                    sql.Append(@"
    UNION ALL");
                }
                sql.AppendFormat(@"
    SELECT {0}", BuildDbValue(v));
                if (string.IsNullOrWhiteSpace(columnName) == false)
                {
                    columnName = columnName.Trim();
                    if (columnName.StartsWith("[") == false || columnName.EndsWith("]") == false)
                    {
                        columnName = "[" + columnName + "]";
                    }
                    sql.AppendFormat(" AS {0}", columnName);
                }
            }
            return sql.ToString();
        }

        public static string ToUnionAllSelectSql<T>(this IEnumerable<T> valueList, string columnName = null)
            where T : struct
        {
            return ToUnionAllSelectSql(valueList.Select(x => (T?)x), columnName);
        }

        public static string ToUnionAllSelectSql<T>(this IEnumerable<T?> valueList, string columnName = null)
            where T : struct
        {
            if (valueList == null || valueList.Count() <= 0)
            {
                return null;
            }
            StringBuilder sql = new StringBuilder();
            foreach (var v in valueList)
            {
                if (sql.Length > 0)
                {
                    sql.Append(@"
    UNION ALL");
                }
                sql.AppendFormat(@"
    SELECT {0}", BuildDbValue(v));
                if (!string.IsNullOrWhiteSpace(columnName))
                {
                    columnName = columnName.Trim();
                    if (!columnName.StartsWith("[")|| !columnName.EndsWith("]"))
                    {
                        columnName = "[" + columnName + "]";
                    }
                    sql.AppendFormat(" AS {0}", columnName);
                }
            }
            return sql.ToString();
        }

        public static string ToUnionAllSelectSql<T>(this IEnumerable<T> objList, bool onlyUsePropertySpecifiedInMapper, PropertyToSelectField<T> mapper = null)
            where T : class
        {
            if (typeof(T) == typeof(string))
            {
                throw new ApplicationException("泛型T的类型不支持string类型");
            }
            Dictionary<string, string> pToFMapper;
            if (mapper != null)
            {
                pToFMapper = new Dictionary<string, string>(mapper.Mapper);
            }
            else
            {
                pToFMapper = new Dictionary<string, string>();
            }
            if (pToFMapper.Count <= 0 && onlyUsePropertySpecifiedInMapper)
            {
                throw new ApplicationException("因为指定了onlyUsePropertySpecifiedInMapper为true，那么就必须传入参数mapper并且mapper里必须有SELECT字段和对象属性名称的映射关系");
            }
            if (onlyUsePropertySpecifiedInMapper == false)
            {
                var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (properties != null && properties.Length > 0)
                {
                    foreach (var property in properties)
                    {
                        string pname = property.Name;
                        if (pToFMapper.ContainsKey(pname) == false && pToFMapper.ContainsValue(pname) == false)
                        {
                            pToFMapper.Add(pname, pname);
                        }
                    }
                }
            }

            StringBuilder sql = new StringBuilder();
            foreach (var obj in objList)
            {
                if (sql.Length > 0)
                {
                    sql.Append(@"
    UNION ALL");
                }
                sql.Append(@"
    SELECT");
                int idx = 0;
                foreach (var map in pToFMapper)
                {
                    string fieldName = map.Key.Trim();
                    if (fieldName.StartsWith("[") == false || fieldName.EndsWith("]") == false)
                    {
                        fieldName = "[" + fieldName + "]";
                    }
                    if (idx > 0)
                    {
                        sql.Append(",");
                    }
                    object val = obj == null ? null : Invoker.PropertyGet(obj, map.Value);
                    if (val == null)
                    {
                        sql.AppendFormat(@" NULL AS {0}", fieldName);
                    }
                    else
                    {
                        Type type = val.GetType();
                        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)
                            && type.GetGenericArguments() != null
                            && type.GetGenericArguments().Length == 1)
                        {
                            var tmpArgs = type.GetGenericArguments();
                            if (tmpArgs != null && tmpArgs.Length == 1)
                            {
                                type = tmpArgs[0];
                            }
                        }
                        string dbVal;
                        if (type == typeof(Guid) || type == typeof(string) || type == typeof(char))
                        {
                            dbVal = "'" + val.ToString().ToSafeSql() + "'";
                        }
                        else if (type == typeof(bool))
                        {
                            dbVal = (bool)val ? "1" : "0";
                        }
                        else if (type == typeof(DateTime))
                        {
                            dbVal = "'" + ((DateTime)val).ToString("yyyy-MM-dd HH:mm:ss") + "'";
                        }
                        else if (type.IsEnum)
                        {
                            dbVal = ((int)val).ToString();
                        }
                        else
                        {
                            dbVal = val.ToString().ToSafeSql();
                        }
                        sql.AppendFormat(@" {0} AS {1}", dbVal, fieldName);
                    }
                    idx++;
                }
            }

            return sql.ToString();
        }
        
        public static string ToPagerSql(this string inputSql, int pageIndex, int pageSize)
        {
            return ToPagerSql(inputSql, "[RowNo]", pageIndex, pageSize);
        }

        public static string ToPagerSql(this string inputSql, string rowNoFieldName, int pageIndex, int pageSize)
        {
            if (inputSql == null)
            {
                return null;
            }
            return string.Format(@"
SELECT TOP {0}
	*
FROM
(
{1}
) AS ibbx
WHERE
	ibbx.{4}>{2}
	AND ibbx.{4}<={3}
ORDER BY
	ibbx.{4}ASC", pageSize, inputSql, pageSize * pageIndex, (pageIndex + 1) * pageSize, rowNoFieldName);
        }

        public static string ToSafeSql(this string inputSql, bool isWhereLikeClause = false)
        {
            if(inputSql == null)
            {
                return null;
            }
            string s = inputSql;
            s = inputSql.Replace("'", "''");
            if (isWhereLikeClause)
            {
                s = s.Replace("[", "[[]");
                s = s.Replace("%", "[%]");
                s = s.Replace("_", "[_]");
            }
            return s;
        }

        public static string ToSqlWithTransaction(this string sql)
        {
            if (sql == null)
            {
                return null;
            }
            return string.Format(@"
BEGIN TRY
	BEGIN TRAN
{0}
	COMMIT TRAN
END TRY
BEGIN CATCH
	ROLLBACK TRAN
	DECLARE @ErrorMessage NVARCHAR(MAX)
	DECLARE @ErrorSeverity INT
	DECLARE @ErrorState INT
	SELECT
		@ErrorMessage = ERROR_MESSAGE()
		,@ErrorSeverity = ERROR_SEVERITY()
		,@ErrorState = ERROR_STATE()
	RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState)
END CATCH", sql);
        }

		public static T Field<T>(this DbDataReader reader, string fieldName)
		{
			object fieldValue = reader[fieldName];
            if (fieldValue == null || fieldValue == DBNull.Value)
            {
                return default(T);
            }

			//return (T)fieldValue;
            return Fly.Framework.Common.DataConvertor.GetValue<T>(fieldValue, null, null);
		}

        public static T Field<T>(this DbDataReader reader, int ordinal)
        {
            object fieldValue = reader[ordinal];
            if (fieldValue == null || fieldValue == DBNull.Value)
            {
                return default(T);
            }
            //return (T)fieldValue;
            return Fly.Framework.Common.DataConvertor.GetValue<T>(fieldValue, null, null);
        }

		/// <summary>
		/// 在用户输入的查询关键词中可能存在空格, 会分隔为2个关键词返回SQL查询条件, 
		/// 返回值示例: {FieldName} LIKE '%{Keyword1}%' AND {FieldName} LIKE '%{Keyword2}%' 
		/// </summary>
		/// <param name="fieldName"></param>
		/// <param name="userInputKeyword"></param>
		/// <returns></returns>
		public static string BuildKeywordQuery(string fieldName, string userInputKeyword)
		{
            if (string.IsNullOrWhiteSpace(userInputKeyword))
            {
                return string.Empty;
            }
            //userInputKeyword = string.Join(" ", Fly.Framework.Common.HelperUtility.SegmentToWord(userInputKeyword.Trim(), true));

            var keywords = Fly.Framework.Common.HelperUtility.SegmentToWord(userInputKeyword.Trim(), true); // userInputKeyword.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            if (keywords.Count <= 0)
            {
                return string.Empty;
            }

			StringBuilder criteriaBuilder = new StringBuilder();
            foreach (string keyword in keywords)
            {
                criteriaBuilder.AppendFormat("AND {0} LIKE '%{1}%' ", fieldName, keyword.ToSafeSql(true));
            }
			return criteriaBuilder.Remove(0, 4).ToString();
		}

        public static T ConvertReaderToEntity<T>(IDataReader reader) where T : class, new()
        {
            T entity = new T();
            DataMapper.NoReadAutoMap(entity, reader, true, true, '.');
            return entity;
        }
    }

    public enum DbInstance
    {
        OnlyRead = 0,
        CanWrite = 1
    }
    
    public class PropertyToSelectField<T> where T : class
    {
        internal Dictionary<string, string> Mapper { get; private set; }

        private PropertyToSelectField() { Mapper = new Dictionary<string, string>(); }

        public static PropertyToSelectField<T> New()
        {
            return new PropertyToSelectField<T>();
        }

        public PropertyToSelectField<T> Map(Expression<Func<T, object>> e, string selectFieldName = null)
        {
            Expression exp = e.Body;
            while (exp.NodeType == ExpressionType.Convert
                || exp.NodeType == ExpressionType.ConvertChecked
                || exp.NodeType == ExpressionType.Quote
                || exp.NodeType == ExpressionType.TypeAs)
            {
                exp = ((UnaryExpression)exp).Operand;
            }
            MemberExpression mexp = exp as MemberExpression;
            if (mexp == null || mexp.Member == null || !(mexp.Member is PropertyInfo))
            {
                throw new ArgumentException("exp", "不是合法的属性表达式");
            }
            var pro = (PropertyInfo)mexp.Member;
            string propertyName = pro.Name;
            if (mexp.Expression != null && mexp.Expression.NodeType != ExpressionType.Parameter)
            {
                throw new ArgumentException("exp", "仅支持一层属性，对于属性\"" + propertyName + "\"不能再指定下级成员");
            }
            MethodInfo getMethod = pro.GetGetMethod(false);
            if (getMethod == null)
            {
                throw new ApplicationException("属性" + propertyName + "不支持public的get读取数据");
            }
            return InnerMap(propertyName, selectFieldName, false);
        }

        public PropertyToSelectField<T> Map(string propertyName, string selectFieldName = null)
        {
            return InnerMap(propertyName, selectFieldName, true);
        }

        private PropertyToSelectField<T> InnerMap(string propertyName, string selectFieldName = null, bool checkPropertyName = true)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new ArgumentNullException("propertyName");
            }
            if (string.IsNullOrWhiteSpace(selectFieldName))
            {
                selectFieldName = propertyName;
            }
            if (Mapper.ContainsKey(selectFieldName))
            {
                throw new ArgumentException("selectFieldName", "出现了重复的SELECT字段名：" + selectFieldName);
            }
            if (checkPropertyName)
            {
                var pro = typeof(T).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
                if (pro == null)
                {
                    throw new ApplicationException("不存在public的属性：" + propertyName);
                }
                MethodInfo getMethod = pro.GetGetMethod(false);
                if (getMethod == null)
                {
                    throw new ApplicationException("属性" + propertyName + "不支持public的get读取数据");
                }
            }
            Mapper.Add(selectFieldName, propertyName);
            return this;
        }
    }
}
