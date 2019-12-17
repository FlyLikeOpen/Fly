using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using System.Transactions;
using System.Configuration;
using System.Threading;

namespace Fly.Framework.SqlDbAccess
{
    internal static class TransactionScopeConnections
    {
        //private static Dictionary<Transaction, Dictionary<string, DbConnection>> transactionConnections = new Dictionary<Transaction, Dictionary<string, DbConnection>>();
        [ThreadStatic]
        private static Dictionary<Transaction, Dictionary<string, DbConnection>> s_Dictionary = null;
        private static Dictionary<Transaction, Dictionary<string, DbConnection>> GetTransactionConnectionDictionary()
        {
            string name = ConfigurationManager.AppSettings["Transaction_Scope_Name"];
            if (name == null || name.Trim().Length <= 0)
            {
                name = "Transaction_Scope";
            }
            LocalDataStoreSlot slot = Thread.GetNamedDataSlot(name);
            if (slot == null)
            {
                slot = Thread.AllocateNamedDataSlot(name);
            }
            var transactionConnections = Thread.GetData(slot) as Dictionary<Transaction, Dictionary<string, DbConnection>>;
            if (transactionConnections == null)
            {
                transactionConnections = new Dictionary<Transaction, Dictionary<string, DbConnection>>();
                Thread.SetData(slot, transactionConnections);
            }
            return transactionConnections;
        }

        /// <summary>
        ///		Returns a connection for the current transaction. This will be an existing <see cref="DbConnection"/>
        ///		instance or a new one if there is a <see cref="TransactionScope"/> active. Otherwise this method
        ///		returns null.
        /// </summary>
        /// <param name="db"></param>
        /// <returns>Either a <see cref="DbConnection"/> instance or null.</returns>
        public static DbConnection GetConnection(string connStr, IDbFactory dbFactory)
        {
            Transaction currentTransaction = Transaction.Current;

            if (currentTransaction == null)
            {
                return null;
            }

            if (s_Dictionary == null)
            {
                s_Dictionary = GetTransactionConnectionDictionary();
            }
            Dictionary<string, DbConnection> connectionList;
            s_Dictionary.TryGetValue(currentTransaction, out connectionList);

            DbConnection connection;
            if (connectionList != null)
            {
                connectionList.TryGetValue(connStr, out connection);
                if (connection != null)
                {
                    return connection;
                }
            }
            else
            {		
                // We don't have a list for this transaction, so create a new one
                connectionList = new Dictionary<string, DbConnection>();
                s_Dictionary.Add(currentTransaction, connectionList);
            }

            //
            // Next we'll see if there is already a connection. If not, we'll create a new connection and add it
            // to the transaction's list of connections.
            //
            if (connectionList.ContainsKey(connStr))
            {
                connection = connectionList[connStr];
            }
            else
            {
                connection = dbFactory.CreateConnection(connStr);
                connection.Open();
                currentTransaction.TransactionCompleted += new TransactionCompletedEventHandler(OnTransactionCompleted);
                connectionList.Add(connStr, connection);
            }

            return connection;
        }

        /// <summary>
        ///		This event handler is called whenever a transaction is about to be disposed, which allows
        ///		us to remove the transaction from our list and dispose the connection instance we created.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnTransactionCompleted(object sender, TransactionEventArgs e)
        {
            Dictionary<string, DbConnection> connectionList; // = connections[e.Transaction];
            s_Dictionary.TryGetValue(e.Transaction, out connectionList);
            if (connectionList == null)
            {
                return;
            }
            s_Dictionary.Remove(e.Transaction);
            foreach (DbConnection conneciton in connectionList.Values)
            {
                try
                {
                    conneciton.Dispose();
                }
                catch { }
            }
        }
    }
}
