using System;
using System.Data;
using System.Data.Common;
using System.Collections.Generic;

namespace Fly.Framework.SqlDbAccess
{
    public static partial class SqlHelper
    {
        public static int ExecuteNonQuery(DbInstance instance, CommandType cmdType, string cmdText, object parameter = null)
        {
            string connStrKey = GetConnStrKey(instance);
            return ExecuteNonQuery(connStrKey, cmdType, cmdText, parameter);
        }

        public static int ExecuteNonQuery(DbInstance instance, string cmdText, object parameter = null)
        {
            return ExecuteNonQuery(instance, CommandType.Text, cmdText, parameter);
        }

        public static DbDataReader ExecuteReader(DbInstance instance, CommandType cmdType, string cmdText, object parameter = null)
        {
            string connStrKey = GetConnStrKey(instance);
            return ExecuteReader(connStrKey, cmdType, cmdText, parameter);
        }

        public static void ExecuteReaderAll(Action<DbDataReader> action, DbInstance instance, CommandType cmdType, string cmdText, object parameter = null)
        {
            using (var reader = ExecuteReader(instance, cmdType, cmdText, parameter))
            {
                while (reader.Read())
                {
                    action(reader);
                }
            }
        }

        public static void ExecuteReaderFirst(Action<DbDataReader> action, DbInstance instance, CommandType cmdType, string cmdText, object parameter = null)
        {
            using (var reader = ExecuteReader(instance, cmdType, cmdText, parameter))
            {
                if (reader.Read())
                {
                    action(reader);
                }
            }
        }

        public static DbDataReader ExecuteReader(DbInstance instance, string cmdText, object parameter = null)
        {
            return ExecuteReader(instance, CommandType.Text, cmdText, parameter);
        }

        public static void ExecuteReaderAll(Action<DbDataReader> action, DbInstance instance, string cmdText, object parameter = null)
        {
            ExecuteReaderAll(action, instance, CommandType.Text, cmdText, parameter);
        }

        public static void ExecuteReaderFirst(Action<DbDataReader> action, DbInstance instance, string cmdText, object parameter = null)
        {
            ExecuteReaderFirst(action, instance, CommandType.Text, cmdText, parameter);
        }

        public static object ExecuteScalar(DbInstance instance, CommandType cmdType, string cmdText, object parameter = null)
        {
            string connStrKey = GetConnStrKey(instance);
            return ExecuteScalar(connStrKey, cmdType, cmdText, parameter);
        }

        public static TResult ExecuteScalar<TResult>(DbInstance instance, string cmdText, object parameter = null)
        {
            object executedResult = ExecuteScalar(instance, cmdText, parameter);
            if (executedResult == null || executedResult == DBNull.Value)
            {
                return default(TResult);
            }
            return Fly.Framework.Common.DataConvertor.GetValue<TResult>(executedResult, null, null);
        }

        public static object ExecuteScalar(DbInstance instance, string cmdText, object parameter = null)
        {
            return ExecuteScalar(instance, CommandType.Text, cmdText, parameter);
        }

        public static DataSet ExecuteDataSet(DbInstance instance, CommandType cmdType, string cmdText, object parameter = null)
        {
            string connStrKey = GetConnStrKey(instance);
            return ExecuteDataSet(connStrKey, cmdType, cmdText, parameter);
        }

        public static DataSet ExecuteDataSet(DbInstance instance, string cmdText, object parameter = null)
        {
            return ExecuteDataSet(instance, CommandType.Text, cmdText, parameter);
        }

        public static DataTable ExecuteDataTable(DbInstance instance, CommandType cmdType, string cmdText, object parameter = null)
        {
            string connStrKey = GetConnStrKey(instance);
            return ExecuteDataTable(connStrKey, cmdType, cmdText, parameter);
        }

        public static DataTable ExecuteDataTable(DbInstance instance, string cmdText, object parameter = null)
        {
            return ExecuteDataTable(instance, CommandType.Text, cmdText, parameter);
        }

        public static DataRow ExecuteDataRow(DbInstance instance, CommandType cmdType, string cmdText, object parameter = null)
        {
            DataTable table = ExecuteDataTable(instance, cmdType, cmdText, parameter);
            if (table.Rows.Count == 0)
            {
                return null;
            }
            return table.Rows[0];
        }

        public static DataRow ExecuteDataRow(DbInstance instance, string cmdText, object parameter = null)
        {
            return ExecuteDataRow(instance, CommandType.Text, cmdText, parameter);
        }

        public static List<T> ExecuteList<T>(Func<DbDataReader, T> converter, DbInstance instance, CommandType cmdType, string cmdText, object parameter = null)
        {
            List<T> list = new List<T>();
            using (DbDataReader reader = SqlHelper.ExecuteReader(instance, CommandType.Text, cmdText, parameter))
            {
                while (reader.Read())
                {
                    list.Add(converter(reader));
                }
                reader.Close();
            }
            return list;
        }

        public static List<T> ExecuteList<T>(Func<DbDataReader, T> converter, DbInstance instance, string cmdText, object parameter = null)
        {
            return ExecuteList<T>(converter, instance, CommandType.Text, cmdText, parameter);
        }

        public static T ExecuteSingle<T>(Func<DbDataReader, T> converter, DbInstance instance, CommandType cmdType, string cmdText, object parameter = null)
        {
            T rst = default(T);
            using (DbDataReader reader = SqlHelper.ExecuteReader(instance, CommandType.Text, cmdText, parameter))
            {
                if (reader.Read())
                {
                    rst = converter(reader);
                }
                reader.Close();
            }
            return rst;
        }

        public static T ExecuteSingle<T>(Func<DbDataReader, T> converter, DbInstance instance, string cmdText, object parameter = null)
        {
            return ExecuteSingle<T>(converter, instance, CommandType.Text, cmdText, parameter);
        }

        public static List<T> ExecuteEntityList<T>(DbInstance instance, CommandType cmdType, string cmdText, object parameter = null) where T : class, new()
        {
            return ExecuteList<T>(ConvertReaderToEntity<T>, instance, cmdType, cmdText, parameter);
        }

        public static List<T> ExecuteEntityList<T>(DbInstance instance, string cmdText, object parameter = null) where T : class, new()
        {
            return ExecuteEntityList<T>(instance, CommandType.Text, cmdText, parameter);
        }

        public static T ExecuteEntity<T>(DbInstance instance, CommandType cmdType, string cmdText, object parameter = null) where T : class, new()
        {
            return ExecuteSingle<T>(ConvertReaderToEntity<T>, instance, cmdType, cmdText, parameter);
        }

        public static T ExecuteEntity<T>(DbInstance instance, string cmdText, object parameter = null) where T : class, new()
        {
            return ExecuteEntity<T>(instance, CommandType.Text, cmdText, parameter);
        }

        public static void BulkInsert(string tableName, DataTable table, int bulkCopyTimeoutSeconds = 60)
        {
            string connStrKey = GetConnStrKey(DbInstance.CanWrite);
            BulkInsert(connStrKey, tableName, table, bulkCopyTimeoutSeconds);
        }
    }
}
