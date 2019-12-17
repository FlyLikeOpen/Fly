using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.Common;
using System.Collections.Generic;
using System.Transactions;

namespace Fly.Framework.SqlDbAccess
{
    public static partial class SqlHelper
    {
        public static int ExecuteNonQuery(string connStrKey, CommandType cmdType, string cmdText, object parameter = null)
        {
            cmdText = ProcessDbName(cmdText);
            IDbFactory dbFactory = GetDbFactory(connStrKey);
            string connectionString = GetConnectionString(connStrKey);
            DbParameter[] commandParameters = DataMapper.BuildDbParameters(cmdText, parameter, dbFactory.CreateParameter, dbFactory.BuildParameterName);
            DbCommand cmd = dbFactory.CreateCommand();
            ConnectionWrapper wrapper = null;
            try
            {
                wrapper = GetOpenConnection(connectionString, dbFactory);
                PrepareCommand(cmd, wrapper.Connection, null, cmdType, cmdText, commandParameters);
                int val = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
                return val;
            }
            catch (Exception ex)
            {
                throw new DaoSqlException(ex, connectionString, cmdText, commandParameters);
            }
            finally
            {
                if (wrapper != null)
                {
                    wrapper.Dispose();
                }
            }
        }

        public static int ExecuteNonQuery(string connStrKey, string cmdText, object parameter = null)
        {
            return ExecuteNonQuery(connStrKey, CommandType.Text, cmdText, parameter);
        }

        public static DbDataReader ExecuteReader(string connStrKey, CommandType cmdType, string cmdText, object parameter = null)
        {
            cmdText = ProcessDbName(cmdText);
            IDbFactory dbFactory = GetDbFactory(connStrKey);
            string connectionString = GetConnectionString(connStrKey);
            DbParameter[] commandParameters = DataMapper.BuildDbParameters(cmdText, parameter, dbFactory.CreateParameter, dbFactory.BuildParameterName);
            DbCommand cmd = dbFactory.CreateCommand();
            DbConnection conn = GetOpenConnection(connectionString, dbFactory).Connection;

            CommandBehavior cmdBehavior;
            if (Transaction.Current != null)
            {
                cmdBehavior = CommandBehavior.Default;
            }
            else
            {
                cmdBehavior = CommandBehavior.CloseConnection;
            }
            try
            {
                PrepareCommand(cmd, conn, null, cmdType, cmdText, commandParameters);
                DbDataReader rdr = cmd.ExecuteReader(cmdBehavior);
                //cmd.Parameters.Clear();
                return rdr;
            }
            catch (Exception ex)
            {
                if (conn != null)
                {
                    try
                    {
                        conn.Close();
                        conn.Dispose();
                    }
                    catch { }
                }
                throw new DaoSqlException(ex, connectionString, cmdText, commandParameters); ;
            }
        }

        public static void ExecuteReaderAll(Action<DbDataReader> action, string connStrKey, CommandType cmdType, string cmdText, object parameter = null)
        {
            using (var reader = ExecuteReader(connStrKey, cmdType, cmdText, parameter))
            {
                while (reader.Read())
                {
                    action(reader);
                }
            }
        }

        public static void ExecuteReaderFirst(Action<DbDataReader> action, string connStrKey, CommandType cmdType, string cmdText, object parameter = null)
        {
            using (var reader = ExecuteReader(connStrKey, cmdType, cmdText, parameter))
            {
                if (reader.Read())
                {
                    action(reader);
                }
            }
        }

        public static DbDataReader ExecuteReader(string connStrKey, string cmdText, object parameter = null)
        {
            return ExecuteReader(connStrKey, CommandType.Text, cmdText, parameter);
        }

        public static void ExecuteReaderAll(Action<DbDataReader> action, string connStrKey, string cmdText, object parameter = null)
        {
            ExecuteReaderAll(action, connStrKey, CommandType.Text, cmdText, parameter);
        }

        public static void ExecuteReaderFirst(Action<DbDataReader> action, string connStrKey, string cmdText, object parameter = null)
        {
            ExecuteReaderFirst(action, connStrKey, CommandType.Text, cmdText, parameter);
        }

        public static object ExecuteScalar(string connStrKey, CommandType cmdType, string cmdText, object parameter = null)
        {
            cmdText = ProcessDbName(cmdText);
            IDbFactory dbFactory = GetDbFactory(connStrKey);
            string connectionString = GetConnectionString(connStrKey);
            DbParameter[] commandParameters = DataMapper.BuildDbParameters(cmdText, parameter, dbFactory.CreateParameter, dbFactory.BuildParameterName);

            DbCommand cmd = dbFactory.CreateCommand();

            ConnectionWrapper wrapper = null;
            try
            {
                wrapper = GetOpenConnection(connectionString, dbFactory);
                PrepareCommand(cmd, wrapper.Connection, null, cmdType, cmdText, commandParameters);
                object val = cmd.ExecuteScalar();
                cmd.Parameters.Clear();
                return val;
            }
            catch (Exception ex)
            {
                throw new DaoSqlException(ex, connectionString, cmdText, commandParameters);
            }
            finally
            {
                if (wrapper != null)
                {
                    wrapper.Dispose();
                }
            }
        }

        public static TResult ExecuteScalar<TResult>(string connStrKey, string cmdText, object parameter = null)
        {
            object executedResult = ExecuteScalar(connStrKey, cmdText, parameter);
            if (executedResult == null || executedResult == DBNull.Value)
            {
                return default(TResult);
            }
            return Fly.Framework.Common.DataConvertor.GetValue<TResult>(executedResult, null, null);
        }

        public static object ExecuteScalar(string connStrKey, string cmdText, object parameter = null)
        {
            return ExecuteScalar(connStrKey, CommandType.Text, cmdText, parameter);
        }

        public static DataSet ExecuteDataSet(string connStrKey, CommandType cmdType, string cmdText, object parameter = null)
        {
            cmdText = ProcessDbName(cmdText);
            IDbFactory dbFactory = GetDbFactory(connStrKey);
            string connectionString = GetConnectionString(connStrKey);
            DbParameter[] commandParameters = DataMapper.BuildDbParameters(cmdText, parameter, dbFactory.CreateParameter, dbFactory.BuildParameterName);

            DbCommand cmd = dbFactory.CreateCommand();

            ConnectionWrapper wrapper = null;
            DataSet ds = new DataSet();
            try
            {
                wrapper = GetOpenConnection(connectionString, dbFactory);
                PrepareCommand(cmd, wrapper.Connection, null, cmdType, cmdText, commandParameters);
                DbDataAdapter sda = dbFactory.CreateDataAdapter();
                sda.SelectCommand = cmd;
                sda.Fill(ds);
                cmd.Parameters.Clear();
            }
            catch (Exception ex)
            {
                throw new DaoSqlException(ex, connectionString, cmdText, commandParameters);
            }
            finally
            {
                if (wrapper != null)
                {
                    wrapper.Dispose();
                }
            }
            return ds;
        }

        public static DataSet ExecuteDataSet(string connStrKey, string cmdText, object parameter = null)
        {
            return ExecuteDataSet(connStrKey, CommandType.Text, cmdText, parameter);
        }

        public static DataTable ExecuteDataTable(string connStrKey, CommandType cmdType, string cmdText, object parameter = null)
        {
            cmdText = ProcessDbName(cmdText);
            IDbFactory dbFactory = GetDbFactory(connStrKey);
            string connectionString = GetConnectionString(connStrKey);
            DbParameter[] commandParameters = DataMapper.BuildDbParameters(cmdText, parameter, dbFactory.CreateParameter, dbFactory.BuildParameterName);

            DbCommand cmd = dbFactory.CreateCommand();

            ConnectionWrapper wrapper = null;
            DataTable table = new DataTable();
            try
            {
                wrapper = GetOpenConnection(connectionString, dbFactory);
                PrepareCommand(cmd, wrapper.Connection, null, cmdType, cmdText, commandParameters);
                DbDataAdapter sda = dbFactory.CreateDataAdapter();
                sda.SelectCommand = cmd;
                sda.Fill(table);
                cmd.Parameters.Clear();
            }
            catch (Exception ex)
            {
                throw new DaoSqlException(ex, connectionString, cmdText, commandParameters);
            }
            finally
            {
                if (wrapper != null)
                {
                    wrapper.Dispose();
                }
            }
            return table;
        }

        public static DataTable ExecuteDataTable(string connStrKey, string cmdText, object parameter = null)
        {
            return ExecuteDataTable(connStrKey, CommandType.Text, cmdText, parameter);
        }

        public static DataRow ExecuteDataRow(string connStrKey, CommandType cmdType, string cmdText, object parameter = null)
        {
            DataTable table = ExecuteDataTable(connStrKey, cmdType, cmdText, parameter);
            if (table.Rows.Count == 0)
            {
                return null;
            }
            return table.Rows[0];
        }

        public static DataRow ExecuteDataRow(string connStrKey, string cmdText, object parameter = null)
        {
            return ExecuteDataRow(connStrKey, CommandType.Text, cmdText, parameter);
        }

        public static List<T> ExecuteList<T>(Func<DbDataReader, T> converter, string connStrKey, CommandType cmdType, string cmdText, object parameter = null)
        {
            List<T> list = new List<T>();
            using (DbDataReader reader = SqlHelper.ExecuteReader(connStrKey, CommandType.Text, cmdText, parameter))
            {
                while (reader.Read())
                {
                    list.Add(converter(reader));
                }
                reader.Close();
            }
            return list;
        }

        public static List<T> ExecuteList<T>(Func<DbDataReader, T> converter, string connStrKey, string cmdText, object parameter = null)
        {
            return ExecuteList<T>(converter, connStrKey, CommandType.Text, cmdText, parameter);
        }

        public static T ExecuteSingle<T>(Func<DbDataReader, T> converter, string connStrKey, CommandType cmdType, string cmdText, object parameter = null)
        {
            T rst = default(T);
            using (DbDataReader reader = SqlHelper.ExecuteReader(connStrKey, CommandType.Text, cmdText, parameter))
            {
                if (reader.Read())
                {
                    rst = converter(reader);
                }
                reader.Close();
            }
            return rst;
        }

        public static T ExecuteSingle<T>(Func<DbDataReader, T> converter, string connStrKey, string cmdText, object parameter = null)
        {
            return ExecuteSingle<T>(converter, connStrKey, CommandType.Text, cmdText, parameter);
        }

        public static List<T> ExecuteEntityList<T>(string connStrKey, CommandType cmdType, string cmdText, object parameter = null) where T : class, new()
        {
            return ExecuteList<T>(ConvertReaderToEntity<T>, connStrKey, cmdType, cmdText, parameter);
        }

        public static List<T> ExecuteEntityList<T>(string connStrKey, string cmdText, object parameter = null) where T : class, new()
        {
            return ExecuteEntityList<T>(connStrKey, CommandType.Text, cmdText, parameter);
        }

        public static T ExecuteEntity<T>(string connStrKey, CommandType cmdType, string cmdText, object parameter = null) where T : class, new()
        {
            return ExecuteSingle<T>(ConvertReaderToEntity<T>, connStrKey, cmdType, cmdText, parameter);
        }

        public static T ExecuteEntity<T>(string connStrKey, string cmdText, object parameter = null) where T : class, new()
        {
            return ExecuteEntity<T>(connStrKey, CommandType.Text, cmdText, parameter);
        }

        public static void BulkInsert(string connStrKey, string tableName, DataTable table, int bulkCopyTimeoutSeconds = 60)
        {
            if (table == null || table.Columns == null || table.Columns.Count <= 0 || table.Rows == null || table.Rows.Count <= 0)
            {
                return;
            }
            //batchSize = batchSize < table.Rows.Count ? batchSize : table.Rows.Count;
            IDbFactory dbFactory = GetDbFactory(connStrKey);
            string connectionString = GetConnectionString(connStrKey);
            using (var wrapper = GetOpenConnection(connectionString, dbFactory))
            {
                SqlConnection conn = (SqlConnection)wrapper.Connection;
                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                }
                using (SqlBulkCopy sqlbulk = new SqlBulkCopy(conn))
                {
                    foreach (DataColumn col in table.Columns)
                    {
                        sqlbulk.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                    }
                    sqlbulk.BatchSize = table.Rows.Count;
                    sqlbulk.BulkCopyTimeout = bulkCopyTimeoutSeconds;
                    sqlbulk.DestinationTableName = tableName;
                    sqlbulk.WriteToServer(table);
                    sqlbulk.Close();
                }
                conn.Close();
            }
        }
    }
}
